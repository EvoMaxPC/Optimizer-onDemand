using EVO.Core.Contracts;
using EVO.Core.Correlation;
using EVO.Core.Findings;

namespace EVO.Core.Tests.Correlation;

/// <summary>Recolector falso (sin CIM) para los tests del motor M14.</summary>
public sealed class StubCollector : ICollector
{
    private readonly string _moduleId;
    private readonly Dictionary<string, double> _metrics;
    private readonly bool _succeeds;

    public StubCollector(string moduleId, IEnumerable<KeyValuePair<string, double>> metrics, bool succeeds = true)
    {
        _moduleId = moduleId;
        _metrics = new Dictionary<string, double>(metrics);
        _succeeds = succeeds;
    }

    public string ModuleId => _moduleId;

    public Task<CollectResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        if (!_succeeds)
            throw new InvalidOperationException("Falla simulada por el test");

        var metrics = _metrics.Select(kv => new Metric(kv.Key, "", kv.Value, DateTimeOffset.UtcNow)).ToArray();
        return Task.FromResult(CollectResult.Create(_moduleId, metrics, Array.Empty<DiagnosticFinding>()));
    }
}

public class BottleneckEngineCollectorTests
{
    [Fact]
    public async Task CollectAsync_ReturnsModuleIdM14()
    {
        var engine = new BottleneckEngineCollector(new ICollector[] { new StubCollector("M02", new Dictionary<string, double>()) });
        var result = await engine.CollectAsync();

        Assert.Equal("M14", engine.ModuleId);
        Assert.Equal("M14", result.ModuleId);
        Assert.NotNull(result.Metrics);
        Assert.NotNull(result.Findings);
    }

    [Fact]
    public async Task CollectAsync_WithoutEvidence_ProducesNoFindings()
    {
        var engine = new BottleneckEngineCollector(new ICollector[]
        {
            new StubCollector("M02", new Dictionary<string, double>()),
            new StubCollector("M03", new Dictionary<string, double>()),
            new StubCollector("M12", new Dictionary<string, double>()),
        });

        var result = await engine.CollectAsync();

        Assert.Empty(result.Findings);
        Assert.Contains(result.Metrics, m => m.Name == "bottleneck.suspected" && m.Value == 0);
        Assert.Contains(result.Metrics, m => m.Name == "bottleneck.corroborated" && m.Value == 0);
    }

    [Fact]
    public async Task CollectAsync_CpuLowUsage_ProducesNoFinding()
    {
        var engine = new BottleneckEngineCollector(new ICollector[]
        {
            new StubCollector("M02", new Dictionary<string, double> { ["cpu.usage_percent"] = 50.0 }),
        });

        var result = await engine.CollectAsync();

        Assert.Empty(result.Findings);
    }

    [Fact]
    public async Task CollectAsync_CpuHighWithoutProcessCorroboration_ProducesWarningFinding()
    {
        var engine = new BottleneckEngineCollector(new ICollector[]
        {
            new StubCollector("M02", new Dictionary<string, double> { ["cpu.usage_percent"] = 92.0 }),
        });

        var result = await engine.CollectAsync();

        var finding = Assert.Single(result.Findings);
        Assert.Equal("CPU", finding.Category);
        Assert.Equal(FindingConfidence.Warning, finding.Confidence);
        Assert.Equal(Severity.Medium, finding.Severity);
        Assert.False(string.IsNullOrWhiteSpace(finding.Recommendation));
        Assert.False(string.IsNullOrWhiteSpace(finding.VerificationMethod));
    }

    [Fact]
    public async Task CollectAsync_CpuHighWithProcessCorroboration_ProducesProbableFinding()
    {
        var engine = new BottleneckEngineCollector(new ICollector[]
        {
            new StubCollector("M02", new Dictionary<string, double> { ["cpu.usage_percent"] = 92.0 }),
            new StubCollector("M06", new Dictionary<string, double> { ["process.top_cpu_percent"] = 88.0 }),
        });

        var result = await engine.CollectAsync();

        var finding = Assert.Single(result.Findings);
        Assert.Equal("CPU", finding.Category);
        Assert.Equal(FindingConfidence.Probable, finding.Confidence);
        Assert.Contains(finding.Evidence, e => e.Source == "CPU" || e.Source == "Process");
        Assert.Contains(result.Metrics, m => m.Name == "bottleneck.corroborated" && m.Value == 1);
    }

    [Fact]
    public async Task CollectAsync_LowMemoryWithPaging_ProducesProbableFinding()
    {
        var engine = new BottleneckEngineCollector(new ICollector[]
        {
            new StubCollector("M03", new Dictionary<string, double>
            {
                ["ram.available_percent"] = 5.0,
                ["ram.page_faults_per_sec"] = 250.0,
            }),
        });

        var result = await engine.CollectAsync();

        Assert.Single(result.Findings, f => f.Category == "RAM");
        Assert.Contains(result.Findings, f => f.Category == "RAM" && f.Confidence == FindingConfidence.Probable);
    }

    [Fact]
    public async Task CollectAsync_LowMemoryWithoutPaging_ProducesWarningFinding()
    {
        var engine = new BottleneckEngineCollector(new ICollector[]
        {
            new StubCollector("M03", new Dictionary<string, double> { ["ram.available_percent"] = 5.0 }),
        });

        var result = await engine.CollectAsync();

        Assert.Contains(result.Findings, f => f.Category == "RAM" && f.Confidence == FindingConfidence.Warning);
    }

    [Fact]
    public async Task CollectAsync_HealthHighEvents_ProducesProbableFinding()
    {
        var engine = new BottleneckEngineCollector(new ICollector[]
        {
            new StubCollector("M12", new Dictionary<string, double>
            {
                ["health.events_24h"] = 80.0,
                ["health.system.errors"] = 25.0,
            }),
        });

        var result = await engine.CollectAsync();

        Assert.Contains(result.Findings, f => f.Category == "Health" && f.Confidence == FindingConfidence.Probable);
    }

    [Fact]
    public async Task CollectAsync_HealthLowEvents_ProducesNoFinding()
    {
        var engine = new BottleneckEngineCollector(new ICollector[]
        {
            new StubCollector("M12", new Dictionary<string, double> { ["health.events_24h"] = 5.0 }),
        });

        var result = await engine.CollectAsync();

        Assert.Empty(result.Findings);
    }

    [Fact]
    public async Task CollectAsync_CollectorFailure_BestEffortContinue()
    {
        var engine = new BottleneckEngineCollector(new ICollector[]
        {
            new StubCollector("M02", new Dictionary<string, double> { ["cpu.usage_percent"] = 92.0 }, succeeds: false),
            new StubCollector("M03", new Dictionary<string, double> { ["ram.available_percent"] = 5.0 }),
        });

        var result = await engine.CollectAsync();

        // El módulo fallido se omite; el resto sigue funcionando.
        Assert.Contains(result.Findings, f => f.Category == "RAM");
    }

    [Fact]
    public async Task CollectAsync_MultipleSourcesSameArea_LatestWins()
    {
        var engine = new BottleneckEngineCollector(new ICollector[]
        {
            new StubCollector("M02", new Dictionary<string, double> { ["cpu.usage_percent"] = 50.0 }),
            new StubCollector("M02", new Dictionary<string, double> { ["cpu.usage_percent"] = 95.0 }),
        });

        var result = await engine.CollectAsync();

        // Último gana: 95 → hallazgo.
        Assert.Contains(result.Findings, f => f.Category == "CPU");
    }

    [Fact]
    public async Task CollectAsync_NoConfirmedConfidenceEver()
    {
        // Garantía dura: ningún hallazgo M14 jamás declara "Confirmed".
        var engine = new BottleneckEngineCollector(new ICollector[]
        {
            new StubCollector("M02", new Dictionary<string, double> { ["cpu.usage_percent"] = 95.0 }),
            new StubCollector("M06", new Dictionary<string, double> { ["process.top_cpu_percent"] = 95.0 }),
            new StubCollector("M03", new Dictionary<string, double> { ["ram.available_percent"] = 5.0, ["ram.page_faults_per_sec"] = 500.0 }),
            new StubCollector("M12", new Dictionary<string, double> { ["health.events_24h"] = 100.0, ["health.system.errors"] = 20.0 }),
        });

        var result = await engine.CollectAsync();

        foreach (var finding in result.Findings)
        {
            Assert.NotEqual(FindingConfidence.Confirmed, finding.Confidence);
            Assert.NotEqual(FindingConfidence.Normal, finding.Confidence);
        }
        Assert.Equal(3, result.Findings.Count);
    }

    [Fact]
    public void Constructor_EmptyEvidence_Throws()
    {
        Assert.Throws<ArgumentException>(() => new BottleneckEngineCollector(Array.Empty<ICollector>()));
    }
}
