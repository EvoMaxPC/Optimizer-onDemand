using EVO.Collectors.Hardware;

namespace EVO.Collectors.Tests;

public class SystemIntelligenceCollectorTests
{
    [Fact]
    public async Task CollectAsync_ReturnsResultWithModuleIdAndMetrics()
    {
        var collector = new SystemIntelligenceCollector();
        var result = await collector.CollectAsync();

        Assert.Equal("M01", collector.ModuleId);
        Assert.Equal("M01", result.ModuleId);
        Assert.NotNull(result.Metrics);
        Assert.NotEmpty(result.Metrics);
        Assert.Empty(result.Findings);
        Assert.True(result.CollectedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task CollectAsync_RecordsOsCaptionMetric()
    {
        var collector = new SystemIntelligenceCollector();
        var result = await collector.CollectAsync();

        // En cualquier Windows, el SO es presente
        Assert.Contains(result.Metrics, m => m.Name == "os.caption");
    }

    [Fact]
    public async Task CollectAsync_RecordsCpuNameMetric()
    {
        var collector = new SystemIntelligenceCollector();
        var result = await collector.CollectAsync();

        // Cualquier Windows moderno tiene al menos 1 procesador
        Assert.Contains(result.Metrics, m => m.Name == "cpu.name");
    }

    [Fact]
    public async Task CollectAsync_RecordsDiskMetrics()
    {
        var collector = new SystemIntelligenceCollector();
        var result = await collector.CollectAsync();

        // Cualquier Windows con discos locales tiene metricas disk.<id>.total_gb
        Assert.Contains(result.Metrics, m => m.Name.StartsWith("disk.") && m.Name.EndsWith(".total_gb"));
    }
}
