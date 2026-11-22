using EVO.Collectors.Storage;

namespace EVO.Collectors.Tests;

public class StorageAnalyzerCollectorTests
{
    [Fact]
    public async Task CollectAsync_ReturnsResultWithModuleId()
    {
        var collector = new StorageAnalyzerCollector();
        var result = await collector.CollectAsync();

        Assert.Equal("M04", collector.ModuleId);
        Assert.Equal("M04", result.ModuleId);
        Assert.NotNull(result.Metrics);
        Assert.NotNull(result.Findings);
        Assert.True(result.CollectedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task CollectAsync_EmitsNoFindings()
    {
        // M04 produce solo métricas de almacenamiento; los hallazgos pertenecen a M14.
        var collector = new StorageAnalyzerCollector();
        var result = await collector.CollectAsync();

        Assert.Empty(result.Findings);
    }

    [Fact]
    public async Task CollectAsync_MetricsAreStorageShaped()
    {
        var collector = new StorageAnalyzerCollector();
        var result = await collector.CollectAsync();

        // Si hay métricas, todas siguen el patrón storage.<disco>.<métrica>.
        foreach (var m in result.Metrics)
        {
            Assert.StartsWith("storage.", m.Name);
        }
    }

    [Fact]
    public async Task CollectAsync_PercentDiskTimeIsWithinValidRange()
    {
        var collector = new StorageAnalyzerCollector();
        var result = await collector.CollectAsync();

        foreach (var m in result.Metrics.Where(m => m.Name.Contains(".percent_disk_time", StringComparison.OrdinalIgnoreCase)))
        {
            Assert.InRange(m.Value, 0.0, 100.0);
            Assert.Equal("%", m.Unit);
        }
    }

    [Fact]
    public async Task CollectAsync_LatencyMetricsAreNonNegative()
    {
        var collector = new StorageAnalyzerCollector();
        var result = await collector.CollectAsync();

        foreach (var m in result.Metrics.Where(m => m.Name.Contains(".avg_sec_", StringComparison.OrdinalIgnoreCase)))
        {
            Assert.True(m.Value >= 0.0);
            Assert.Equal("ms", m.Unit);
        }
    }

    [Fact]
    public async Task CollectAsync_IsDeterministicModuleId()
    {
        var collector = new StorageAnalyzerCollector();

        Assert.Equal("M04", collector.ModuleId);
        Assert.Equal("M04", collector.ModuleId);
    }
}
