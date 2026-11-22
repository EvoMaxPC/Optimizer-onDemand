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
        var defs = schemaDoc.RootElement.GetProperty("$defs");

        var pairs = new (string Req, string DefName)[]
        {
            ("""{"jsonrpc":"2.0","id":1,"method":"engine.hello","params":{"clientName":"ci","protocolVersion":1}}""", "methodEngineHello"),
            ("""{"jsonrpc":"2.0","id":2,"method":"engine.version"}""", "methodEngineVersion"),
            ("""{"jsonrpc":"2.0","id":3,"method":"system.osInfo"}""", "methodSystemOsInfo")
        };

        foreach (var (req, defName) in pairs)
        {
            string? res = _processor.Process(req);
            Assert.NotNull(res);
            using var resDoc = JsonDocument.Parse(res!);
            var result = resDoc.RootElement.GetProperty("result");
            var required = defs.GetProperty(defName).GetProperty("result").GetProperty("required");
            foreach (var r in required.EnumerateArray())
            {
                string key = r.GetString()!;
                Assert.True(result.TryGetProperty(key, out _), $"Missing required field {key} in {defName}");
            }
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
