using System.Text;
using System.Text.Json;
using Evo.Engine.Host;
using Xunit;

namespace Evo.Engine.Host.Tests;

public sealed class RequestProcessorTests
{
    private readonly RequestProcessor _processor = new();

    [Fact]
    public void Hello_Valid_ReturnsExpectedResult()
    {
        string? res = _processor.Process("""{"jsonrpc":"2.0","id":1,"method":"engine.hello","params":{"clientName":"test","protocolVersion":1}}""");
        Assert.NotNull(res);
        Assert.DoesNotContain("\n", res);
        using var doc = JsonDocument.Parse(res!);
        var result = doc.RootElement.GetProperty("result");
        Assert.Equal(1, result.GetProperty("protocolVersion").GetInt32());
        Assert.Equal("Evo.Engine.Host", result.GetProperty("engineName").GetString());
        Assert.True(result.GetProperty("readOnly").GetBoolean());
    }

    [Fact]
    public void Hello_ProtocolMismatch_ReturnsMinus32001()
    {
        string? res = _processor.Process("""{"jsonrpc":"2.0","id":2,"method":"engine.hello","params":{"clientName":"test","protocolVersion":2}}""");
        Assert.NotNull(res);
        using var doc = JsonDocument.Parse(res!);
        Assert.Equal(-32001, doc.RootElement.GetProperty("error").GetProperty("code").GetInt32());
    }

    [Fact]
    public void UnknownMethod_ReturnsMinus32601()
    {
        string? res = _processor.Process("""{"jsonrpc":"2.0","id":3,"method":"nope"}""");
        Assert.NotNull(res);
        using var doc = JsonDocument.Parse(res!);
        Assert.Equal(-32601, doc.RootElement.GetProperty("error").GetProperty("code").GetInt32());
    }

    [Fact]
    public void InvalidParams_ReturnsMinus32602()
    {
        string? res = _processor.Process("""{"jsonrpc":"2.0","id":4,"method":"engine.hello","params":{"clientName":""}}""");
        Assert.NotNull(res);
        using var doc = JsonDocument.Parse(res!);
        Assert.Equal(-32602, doc.RootElement.GetProperty("error").GetProperty("code").GetInt32());
    }

    [Fact]
    public void InvalidJson_ReturnsMinus32700WithNullId()
    {
        string? res = _processor.Process("{broken");
        Assert.NotNull(res);
        using var doc = JsonDocument.Parse(res!);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("id").ValueKind);
        Assert.Equal(-32700, doc.RootElement.GetProperty("error").GetProperty("code").GetInt32());
    }

    [Fact]
    public void BatchRequest_ReturnsMinus32600()
    {
        string? res = _processor.Process("""[{"jsonrpc":"2.0","id":1,"method":"engine.version"}]""");
        Assert.NotNull(res);
        using var doc = JsonDocument.Parse(res!);
        Assert.Equal(-32600, doc.RootElement.GetProperty("error").GetProperty("code").GetInt32());
    }

    [Fact]
    public void Notification_ReturnsNull()
    {
        string? res = _processor.Process("""{"jsonrpc":"2.0","method":"engine.version"}""");
        Assert.Null(res);
    }

    [Fact]
    public void ResultFields_MatchIpcHostSchemaRequired()
    {
        string repoRoot = RepoRoot.Find();
        string schemaPath = Path.Combine(repoRoot, "schemas", "ipc-host.schema.json");
        using var schemaDoc = JsonDocument.Parse(File.ReadAllText(schemaPath, Encoding.UTF8));

        if (!schemaDoc.RootElement.TryGetProperty("$defs", out var defs))
        {
            Assert.Fail("Missing schema path: $defs");
            return;
        }

        var pairs = new (string Req, string DefName)[]
        {
            ("""{"jsonrpc":"2.0","id":1,"method":"engine.hello","params":{"clientName":"ci","protocolVersion":1}}""", "methodEngineHello"),
            ("""{"jsonrpc":"2.0","id":2,"method":"engine.version"}""", "methodEngineVersion"),
            ("""{"jsonrpc":"2.0","id":3,"method":"system.osInfo"}""", "methodSystemOsInfo")
        };

        foreach (var (req, defName) in pairs)
        {
            if (!defs.TryGetProperty(defName, out var defNode))
            {
                Assert.Fail($"Missing schema path: $defs.{defName}");
                return;
            }
            if (!defNode.TryGetProperty("properties", out var defProps))
            {
                Assert.Fail($"Missing schema path: $defs.{defName}.properties");
                return;
            }
            if (!defProps.TryGetProperty("result", out var resultSchema))
            {
                Assert.Fail($"Missing schema path: $defs.{defName}.properties.result");
                return;
            }
            if (!resultSchema.TryGetProperty("required", out var required) || required.ValueKind != JsonValueKind.Array)
            {
                Assert.Fail($"Missing schema path: $defs.{defName}.properties.result.required");
                return;
            }

            var expectedKeys = required.EnumerateArray()
                .Select(e => e.GetString())
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s!)
                .ToHashSet(StringComparer.Ordinal);
            Assert.NotEmpty(expectedKeys);

            string? res = _processor.Process(req);
            Assert.NotNull(res);
            using var resDoc = JsonDocument.Parse(res!);
            if (!resDoc.RootElement.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Object)
            {
                Assert.Fail($"Response for {defName} did not contain a 'result' object.");
                return;
            }

            var actualKeys = result.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
            Assert.True(expectedKeys.SetEquals(actualKeys),
                $"Result keys mismatch for {defName}. Expected: [{string.Join(", ", expectedKeys)}], Actual: [{string.Join(", ", actualKeys)}]");
        }
    }

    [Fact]
    public void HostSource_IsStrictlyReadOnly()
    {
        string repoRoot = RepoRoot.Find();
        string hostDir = Path.Combine(repoRoot, "engine", "src", "Evo.Engine.Host");
        string[] forbidden = ["Microsoft.Win32", "System.ServiceProcess", "Process.Start", "File.Write", "File.Delete", "Registry", "DllImport"];

        string[] csFiles = Directory.GetFiles(hostDir, "*.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(csFiles);
        string[] fileNames = csFiles.Select(Path.GetFileName).Where(n => n is not null).Select(n => n!).ToArray();
        Assert.Contains("Program.cs", fileNames);
        Assert.Contains("RequestProcessor.cs", fileNames);

        foreach (string csFile in csFiles)
        {
            string content = File.ReadAllText(csFile, Encoding.UTF8);
            foreach (string term in forbidden)
            {
                Assert.DoesNotContain(term, content);
            }
        }
    }
}
