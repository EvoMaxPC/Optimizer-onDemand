using EVO.Core.Contracts;
using EVO.Core.Findings;
using EVO.Core.Live;

namespace EVO.Core.Tests.Live;

/// <summary>Recolector falso (sin CIM) para los tests del monitor M16.</summary>
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

public class LiveMonitorCollectorTests
{
    [Fact]
    public async Task CollectAsync_ReturnsModuleIdM16()
    {
        var monitor = new LiveMonitorCollector(new ICollector[] { new StubCollector("M02", new Dictionary<string, double>()) }, intervalMs: 0, sampleCount: 2);
        var result = await monitor.CollectAsync();

        Assert.Equal("M16", monitor.ModuleId);
        Assert.Equal("M16", result.ModuleId);
        Assert.NotNull(result.Metrics);
    }

    [Fact]
    public async Task CollectAsync_SamplesCountIsReported()
    {
        var monitor = new LiveMonitorCollector(
            new ICollector[] { new StubCollector("M02", new Dictionary<string, double>()) }, intervalMs: 0, sampleCount: 3);

        var result = await monitor.CollectAsync();

        Assert.Contains(result.Metrics, m => m.Name == "live.samples" && m.Value == 3);
    }

    [Fact]
    public async Task CollectAsync_CpuUsageAggregatesAcrossSamples()
    {
        var monitor = new LiveMonitorCollector(
            new ICollector[] { new StubCollector("M02", new Dictionary<string, double> { ["cpu.usage_percent"] = 50.0 }) },
            intervalMs: 0, sampleCount: 3);

        var result = await monitor.CollectAsync();

        // Las 3 muestras devuelven 50.0 → avg = max = 50.0
        Assert.Contains(result.Metrics, m => m.Name == "live.cpu.usage_avg" && m.Value == 50.0);
        Assert.Contains(result.Metrics, m => m.Name == "live.cpu.usage_max" && m.Value == 50.0);
    }

    [Fact]
    public async Task CollectAsync_MemoryMetricsAggregate()
    {
        var monitor = new LiveMonitorCollector(
            new ICollector[] { new StubCollector("M03", new Dictionary<string, double> { ["ram.available_percent"] = 30.0 }) },
            intervalMs: 0, sampleCount: 2);

        var result = await monitor.CollectAsync();

        Assert.Contains(result.Metrics, m => m.Name == "live.ram.available_avg" && m.Value == 30.0);
        Assert.Contains(result.Metrics, m => m.Name == "live.ram.available_min" && m.Value == 30.0);
    }

    [Fact]
    public async Task CollectAsync_EmitsNoFindings()
    {
        var monitor = new LiveMonitorCollector(
            new ICollector[] { new StubCollector("M02", new Dictionary<string, double> { ["cpu.usage_percent"] = 95.0 }) },
            intervalMs: 0, sampleCount: 2);

        var result = await monitor.CollectAsync();

        Assert.Empty(result.Findings);
    }

    [Fact]
    public async Task CollectAsync_CollectorFailure_BestEffortContinue()
    {
        var monitor = new LiveMonitorCollector(
            new ICollector[]
            {
                new StubCollector("M02", new Dictionary<string, double> { ["cpu.usage_percent"] = 0.0 }, succeeds: false),
                new StubCollector("M03", new Dictionary<string, double> { ["ram.available_percent"] = 40.0 }),
            },
            intervalMs: 0, sampleCount: 2);

        var result = await monitor.CollectAsync();

        // Falla simulada en M02; M03 sigue emitiendo métricas.
        Assert.Contains(result.Metrics, m => m.Name == "live.ram.available_avg" && m.Value == 40.0);
        Assert.DoesNotContain(result.Metrics, m => m.Name == "live.cpu.usage_avg");
    }

    [Fact]
    public void Constructor_EmptyResources_Throws()
    {
        Assert.Throws<ArgumentException>(() => new LiveMonitorCollector(Array.Empty<ICollector>(), intervalMs: 0, sampleCount: 1));
    }

    [Fact]
    public void Constructor_InvalidSampleCount_Throws()
    {
        Assert.Throws<ArgumentException>(() => new LiveMonitorCollector(
            new ICollector[] { new StubCollector("M02", new Dictionary<string, double>()) },
            intervalMs: 0, sampleCount: 0));
    }

    [Fact]
    public void Constructor_InvalidInterval_Throws()
    {
        Assert.Throws<ArgumentException>(() => new LiveMonitorCollector(
            new ICollector[] { new StubCollector("M02", new Dictionary<string, double>()) },
            intervalMs: -1, sampleCount: 1));
    }
}
