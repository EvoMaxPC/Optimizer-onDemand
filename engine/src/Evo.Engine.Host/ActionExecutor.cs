using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Evo.Engine.Host;

public sealed record CatalogAction(
    string Id,
    string ModuleId,
    int Category,
    string Title,
    string Description,
    string RiskLevel,
    string Reversibility,
    string Primitive,
    string Target,
    string ValueType,
    JsonElement ApplyValue,
    JsonElement DefaultValue);

public static class ActionExecutor
{
    private const int ProcessTimeoutMs = 15_000;

    public static string FindCatalogPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "catalog", "actions.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }
            current = current.Parent;
        }
        throw new FileNotFoundException("Could not locate catalog/actions.json.");
    }

    public static List<CatalogAction> LoadCatalog()
    {
        string path = FindCatalogPath();
        using var doc = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
        var actions = new List<CatalogAction>();
        foreach (var el in doc.RootElement.GetProperty("actions").EnumerateArray())
        {
            actions.Add(new CatalogAction(
                el.GetProperty("id").GetString()!,
                el.GetProperty("moduleId").GetString()!,
                el.GetProperty("category").GetInt32(),
                el.GetProperty("title").GetString()!,
                el.GetProperty("description").GetString()!,
                el.GetProperty("riskLevel").GetString()!,
                el.GetProperty("reversibility").GetString()!,
                el.GetProperty("primitive").GetString()!,
                el.GetProperty("target").GetString()!,
                el.GetProperty("valueType").GetString()!,
                el.GetProperty("applyValue").Clone(),
                el.GetProperty("defaultValue").Clone()));
        }
        return actions;
    }

    private static string GetSnapshotPath(string actionId)
    {
        string snapshotDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EvoOptimizer",
            "snapshots");
        Directory.CreateDirectory(snapshotDir);
        return Path.Combine(snapshotDir, $"{actionId}.json");
    }

    public static object Apply(string actionId)
    {
        var action = LoadCatalog().FirstOrDefault(a => a.Id == actionId)
            ?? throw new KeyNotFoundException($"Unknown action: {actionId}");

        string snapshotPath = GetSnapshotPath(action.Id);

        if (!File.Exists(snapshotPath))
        {
            object previousValue = OperatingSystem.IsWindows()
                ? ReadCurrentWindowsValue(action.Primitive, action.Target, action.ValueType, action.DefaultValue)
                : action.DefaultValue;

            var snap = new
            {
                actionId = action.Id,
                target = action.Target,
                primitive = action.Primitive,
                previousValue,
                capturedAtUtc = DateTime.UtcNow.ToString("O")
            };
            File.WriteAllText(snapshotPath, JsonSerializer.Serialize(snap), Encoding.UTF8);
        }

        if (OperatingSystem.IsWindows())
        {
            ExecuteWindowsPrimitive(action.Primitive, action.Target, action.ValueType, action.ApplyValue);
        }

        var result = new
        {
            actionId = action.Id,
            status = "applied",
            snapshotPath
        };
        EvidenceLedger.Append("action.apply", "applied", new { actionId = action.Id, target = action.Target });
        return result;
    }

    public static object Rollback(string actionId)
    {
        var action = LoadCatalog().FirstOrDefault(a => a.Id == actionId)
            ?? throw new KeyNotFoundException($"Unknown action: {actionId}");

        string snapshotPath = GetSnapshotPath(action.Id);
        JsonElement restoreValue = action.DefaultValue;

        if (File.Exists(snapshotPath))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(snapshotPath, Encoding.UTF8));
            if (doc.RootElement.TryGetProperty("previousValue", out var prev))
            {
                restoreValue = prev.Clone();
            }
        }

        if (OperatingSystem.IsWindows())
        {
            ExecuteWindowsPrimitive(action.Primitive, action.Target, action.ValueType, restoreValue);
        }

        var result = new
        {
            actionId = action.Id,
            status = "rolled_back"
        };
        EvidenceLedger.Append("action.rollback", "rolled_back", new { actionId = action.Id });
        return result;
    }

    public static List<JsonElement> ListSnapshots()
    {
        string snapshotDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EvoOptimizer",
            "snapshots");
        if (!Directory.Exists(snapshotDir))
        {
            return new List<JsonElement>();
        }

        var list = new List<JsonElement>();
        foreach (string file in Directory.GetFiles(snapshotDir, "*.json"))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file, Encoding.UTF8));
                list.Add(doc.RootElement.Clone());
            }
            catch
            {
                // Ignore malformed snapshot files
            }
        }
        return list;
    }

    private static object ReadCurrentWindowsValue(string primitive, string target, string valueType, JsonElement fallback)
    {
        try
        {
            if (primitive == "registry.setValue")
            {
                int lastSlash = target.LastIndexOf('\\');
                if (lastSlash <= 0) return fallback;
                string key = target[..lastSlash];
                string valueName = target[(lastSlash + 1)..];
                var (exitCode, stdout, _) = RunProcess("reg.exe", $"query \"{key}\" /v \"{valueName}\"", throwOnError: false);
                if (exitCode != 0) return fallback;

                var match = Regex.Match(stdout, $@"{Regex.Escape(valueName)}\s+REG_\w+\s+(\S+)");
                if (!match.Success) return fallback;
                string raw = match.Groups[1].Value;
                if (valueType == "DWORD")
                {
                    if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(raw[2..], System.Globalization.NumberStyles.HexNumber, null, out int hexVal))
                    {
                        return hexVal;
                    }
                    if (int.TryParse(raw, out int decVal))
                    {
                        return decVal;
                    }
                }
                return raw;
            }

            if (primitive == "service.setStartType")
            {
                var (exitCode, stdout, _) = RunProcess("sc.exe", $"qc \"{target}\"", throwOnError: false);
                if (exitCode != 0) return fallback;
                if (stdout.Contains("DISABLED", StringComparison.OrdinalIgnoreCase)) return "Disabled";
                if (stdout.Contains("DEMAND_START", StringComparison.OrdinalIgnoreCase)) return "Manual";
                if (stdout.Contains("AUTO_START", StringComparison.OrdinalIgnoreCase)) return "Automatic";
            }
        }
        catch
        {
            // Fallback to catalog default if query fails
        }
        return fallback;
    }

    private static void ExecuteWindowsPrimitive(string primitive, string target, string valueType, JsonElement value)
    {
        if (primitive == "registry.setValue")
        {
            int lastSlash = target.LastIndexOf('\\');
            if (lastSlash <= 0)
            {
                throw new InvalidOperationException($"Invalid registry target: {target}");
            }
            string key = target[..lastSlash];
            string valueName = target[(lastSlash + 1)..];
            string regType = valueType == "DWORD" ? "REG_DWORD" : "REG_SZ";
            string valStr = value.ValueKind == JsonValueKind.Number ? value.GetInt32().ToString() : value.ToString();

            RunProcess("reg.exe", $"add \"{key}\" /v \"{valueName}\" /t {regType} /d \"{valStr}\" /f");
        }
        else if (primitive == "service.setStartType")
        {
            string startMode = value.GetString()?.ToLowerInvariant() switch
            {
                "disabled" => "disabled",
                "manual" => "demand",
                _ => "auto"
            };
            RunProcess("sc.exe", $"config \"{target}\" start= {startMode}");
        }
        else
        {
            throw new NotSupportedException($"Unsupported primitive: {primitive}");
        }
    }

    private static (int ExitCode, string Stdout, string Stderr) RunProcess(string fileName, string arguments, bool throwOnError = true)
    {
        using var proc = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }) ?? throw new InvalidOperationException($"Failed to start {fileName}");

        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();

        if (!proc.WaitForExit(ProcessTimeoutMs))
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException($"{fileName} timed out after {ProcessTimeoutMs}ms");
        }

        string stdout = stdoutTask.GetAwaiter().GetResult();
        string stderr = stderrTask.GetAwaiter().GetResult();

        if (throwOnError && proc.ExitCode != 0)
        {
            throw new InvalidOperationException($"{fileName} exited with {proc.ExitCode}: {stderr}");
        }

        return (proc.ExitCode, stdout, stderr);
    }
}
