using EVO.Collectors.Startup;

namespace EVO.Collectors.Tests;

public class StartupAnalyzerCollectorTests
{
    [Fact]
    public async Task CollectAsync_ReturnsResultWithModuleId()
    {
        var collector = new StartupAnalyzerCollector();
        var result = await collector.CollectAsync();

        Assert.Equal("M08", collector.ModuleId);
        Assert.Equal("M08", result.ModuleId);
        Assert.NotNull(result.Metrics);
        Assert.NotNull(result.Findings);
        Assert.True(result.CollectedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task CollectAsync_CountIsAtLeastZero()
    {
        var collector = new StartupAnalyzerCollector();
        var result = await collector.CollectAsync();

        var count = result.Metrics.FirstOrDefault(m => m.Name == "startup.count");
        Assert.NotNull(count);
        Assert.True(count!.Value >= 0.0);
        Assert.Equal("count", count.Unit);
    }

    [Fact]
    public async Task CollectAsync_EmitsNoFindings()
    {
        // M08 es auditoría: produce solo métricas, sin hallazgos.
        var collector = new StartupAnalyzerCollector();
        var result = await collector.CollectAsync();

        Assert.Empty(result.Findings);
    }

    [Fact]
    public async Task CollectAsync_CountsAreConsistent()
    {
        var collector = new StartupAnalyzerCollector();
        var result = await collector.CollectAsync();

        var count = result.Metrics.FirstOrDefault(m => m.Name == "startup.count")?.Value ?? 0.0;
        var registry = result.Metrics.FirstOrDefault(m => m.Name == "startup.registry")?.Value ?? 0.0;
        var folder = result.Metrics.FirstOrDefault(m => m.Name == "startup.folder")?.Value ?? 0.0;

        if (count > 0.0)
        {
            // Cada elemento se clasifica como registry o folder → sumas == count.
            Assert.Equal(count, registry + folder, 3);
        }
    }

    [Fact]
    public async Task CollectAsync_IsDeterministicModuleId()
    {
        var collector = new StartupAnalyzerCollector();

        Assert.Equal("M08", collector.ModuleId);
        Assert.Equal("M08", collector.ModuleId);
    }
}
