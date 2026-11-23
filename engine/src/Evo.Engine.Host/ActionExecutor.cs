using System.Diagnostics;
using System.Text;
using System.Text.Json;

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

    public static object Apply(string actionId)
    {
        var action = LoadCatalog().FirstOrDefault(a => a.Id == actionId)
            ?? throw new KeyNotFoundException($"Unknown action: {actionId}");

        string snapshotDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EvoOptimizer",
            "snapshots");
        Directory.CreateDirectory(snapshotDir);
        string snapshotPath = Path.Combine(snapshotDir, $"{action.Id}.json");

        if (!File.Exists(snapshotPath))
        {
            var snap = new
            {
                actionId = action.Id,
                target = action.Target,
                primitive = action.Primitive,
                previousValue = action.DefaultValue,
                capturedAtUtc = DateTime.UtcNow.ToString("O")
            };
            File.WriteAllText(snapshotPath, JsonSerializer.Serialize(snap), Encoding.UTF8);
        }

        if (OperatingSystem.IsWindows())
        {
            ExecuteWindowsPrimitive(action.Primitive, action.Target, action.ValueType, action.ApplyValue);
        }

        return new
        {
            actionId = action.Id,
            status = "applied",
            snapshotPath
        };
    }

    public static object Rollback(string actionId)
    {
        var action = LoadCatalog().FirstOrDefault(a => a.Id == actionId)
            ?? throw new KeyNotFoundException($"Unknown action: {actionId}");

        if (OperatingSystem.IsWindows())
        {
            ExecuteWindowsPrimitive(action.Primitive, action.Target, action.ValueType, action.DefaultValue);
        }

        return new
        {
            actionId = action.Id,
            status = "rolled_back"
        };
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

    private static void RunProcess(string fileName, string arguments)
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

        proc.WaitForExit();
        if (proc.ExitCode != 0)
        {
            string err = proc.StandardError.ReadToEnd();
            throw new InvalidOperationException($"{fileName} exited with {proc.ExitCode}: {err}");
        }
    }
}
