using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Evo.Engine.Host;

public sealed class RequestProcessor
{
    public const int MaxLineBytes = 1048576; // 1 MiB
    private static readonly string[] SupportedMethods = ["engine.hello", "engine.version", "system.osInfo"];

    public string? Process(string line)
    {
        if (Encoding.UTF8.GetByteCount(line) > MaxLineBytes)
        {
            return SerializeError(null, -32600, "Invalid Request: line exceeds 1 MiB");
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(line);
        }
        catch (JsonException)
        {
            return SerializeError(null, -32700, "Parse error");
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return SerializeError(null, -32600, "Invalid Request");
            }

            bool hasId = root.TryGetProperty("id", out var idElement);
            object? idValue = null;
            if (hasId)
            {
                if (idElement.ValueKind == JsonValueKind.String)
                    idValue = idElement.GetString();
                else if (idElement.ValueKind == JsonValueKind.Number && idElement.TryGetInt64(out long l))
                    idValue = l;
                else if (idElement.ValueKind == JsonValueKind.Null)
                    idValue = null;
                else
                    return SerializeError(null, -32600, "Invalid Request: invalid id");
            }

            if (!root.TryGetProperty("jsonrpc", out var rpc) || rpc.GetString() != "2.0" ||
                !root.TryGetProperty("method", out var methodEl) || methodEl.ValueKind != JsonValueKind.String)
            {
                return hasId ? SerializeError(idValue, -32600, "Invalid Request") : null;
            }

            string method = methodEl.GetString() ?? string.Empty;
            bool hasParams = root.TryGetProperty("params", out var paramsEl);

            if (method == "engine.hello")
            {
                if (!hasParams || paramsEl.ValueKind != JsonValueKind.Object)
                {
                    return hasId ? SerializeError(idValue, -32602, "Invalid params") : null;
                }
                if (!paramsEl.TryGetProperty("clientName", out var clientEl) || clientEl.ValueKind != JsonValueKind.String ||
                    !paramsEl.TryGetProperty("protocolVersion", out var verEl) || verEl.ValueKind != JsonValueKind.Number || !verEl.TryGetInt32(out int protoVer))
                {
                    return hasId ? SerializeError(idValue, -32602, "Invalid params") : null;
                }
                string clientName = clientEl.GetString() ?? string.Empty;
                if (clientName.Length < 1 || clientName.Length > 64)
                {
                    return hasId ? SerializeError(idValue, -32602, "Invalid params: clientName length") : null;
                }
                if (protoVer != 1)
                {
                    return hasId ? SerializeError(idValue, -32001, "protocol_mismatch") : null;
                }
                if (!hasId) return null;
                return SerializeResult(idValue, new
                {
                    protocolVersion = 1,
                    engineName = "Evo.Engine.Host",
                    readOnly = true,
                    methods = SupportedMethods
                });
            }

            if (method == "engine.version")
            {
                if (hasParams && paramsEl.ValueKind != JsonValueKind.Null)
                {
                    return hasId ? SerializeError(idValue, -32602, "Invalid params") : null;
                }
                if (!hasId) return null;
                return SerializeResult(idValue, new
                {
                    engineVersion = typeof(RequestProcessor).Assembly.GetName().Version?.ToString() ?? "0.1.0",
                    runtime = RuntimeInformation.FrameworkDescription
                });
            }

            if (method == "system.osInfo")
            {
                if (hasParams && paramsEl.ValueKind != JsonValueKind.Null)
                {
                    return hasId ? SerializeError(idValue, -32602, "Invalid params") : null;
                }
                if (!hasId) return null;
                string platform = OperatingSystem.IsWindows() ? "windows"
                    : OperatingSystem.IsLinux() ? "linux"
                    : OperatingSystem.IsMacOS() ? "macos"
                    : "other";
                return SerializeResult(idValue, new
                {
                    platform,
                    osDescription = RuntimeInformation.OSDescription,
                    osVersion = Environment.OSVersion.VersionString,
                    architecture = RuntimeInformation.OSArchitecture.ToString(),
                    source = "runtime"
                });
            }

            if (method == "catalog.list")
            {
                if (!hasId) return null;
                return SerializeResult(idValue, new { actions = ActionExecutor.LoadCatalog() });
            }

            if (method == "action.apply")
            {
                if (!hasParams || paramsEl.ValueKind != JsonValueKind.Object ||
                    !paramsEl.TryGetProperty("actionId", out var actionIdEl) || actionIdEl.ValueKind != JsonValueKind.String)
                {
                    return hasId ? SerializeError(idValue, -32602, "Invalid params: actionId required") : null;
                }
                if (!hasId) return null;
                return SerializeResult(idValue, ActionExecutor.Apply(actionIdEl.GetString()!));
            }

            if (method == "action.rollback")
            {
                if (!hasParams || paramsEl.ValueKind != JsonValueKind.Object ||
                    !paramsEl.TryGetProperty("actionId", out var actionIdEl) || actionIdEl.ValueKind != JsonValueKind.String)
                {
                    return hasId ? SerializeError(idValue, -32602, "Invalid params: actionId required") : null;
                }
                if (!hasId) return null;
                return SerializeResult(idValue, ActionExecutor.Rollback(actionIdEl.GetString()!));
            }

            if (method == "snapshot.list")
            {
                if (!hasId) return null;
                return SerializeResult(idValue, new { snapshots = ActionExecutor.ListSnapshots() });
            }

            if (method == "diagnostics.collect")
            {
                if (!hasId) return null;
                return SerializeResult(idValue, DiagnosticsCollector.Collect());
            }

            if (method == "plan.generate")
            {
                string profile = "balanced";
                if (hasParams && paramsEl.ValueKind == JsonValueKind.Object &&
                    paramsEl.TryGetProperty("profile", out var profEl) && profEl.ValueKind == JsonValueKind.String)
                {
                    profile = profEl.GetString() ?? "balanced";
                }
                if (!hasId) return null;
                return SerializeResult(idValue, AdaptiveCore.GeneratePlan(profile));
            }

            return hasId ? SerializeError(idValue, -32601, "Method not found") : null;
        }
    }

    private static string SerializeResult(object? id, object result)
    {
        return JsonSerializer.Serialize(new { jsonrpc = "2.0", id, result });
    }

    private static string SerializeError(object? id, int code, string message)
    {
        return JsonSerializer.Serialize(new { jsonrpc = "2.0", id, error = new { code, message } });
    }
}
