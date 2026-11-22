using EVO.Collectors.Drivers;

namespace EVO.Collectors.Tests;

public class DriverAnalyzerCollectorTests
{
    [Fact]
    public async Task CollectAsync_ReturnsResultWithModuleId()
    {
        var collector = new DriverAnalyzerCollector();
        var result = await collector.CollectAsync();

        Assert.Equal("M09", collector.ModuleId);
        Assert.Equal("M09", result.ModuleId);
        Assert.NotNull(result.Metrics);
        Assert.NotNull(result.Findings);
        Assert.True(result.CollectedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task CollectAsync_CountIsAtLeastZero()
    {
        var collector = new DriverAnalyzerCollector();
        var result = await collector.CollectAsync();

        var count = result.Metrics.FirstOrDefault(m => m.Name == "drivers.count");
        Assert.NotNull(count);
        Assert.True(count!.Value >= 0.0);
        Assert.Equal("count", count.Unit);
    }

    [Fact]
    public async Task CollectAsync_EmitsNoFindings()
    {
        // M09 es auditoría: produce solo métricas, sin hallazgos.
        var collector = new DriverAnalyzerCollector();
        var result = await collector.CollectAsync();

        Assert.Empty(result.Findings);
    }

    [Fact]
    public async Task CollectAsync_RunningDoesNotExceedCount()
    {
        var collector = new DriverAnalyzerCollector();
        var result = await collector.CollectAsync();

        var count = result.Metrics.FirstOrDefault(m => m.Name == "drivers.count")?.Value ?? 0.0;
        var running = result.Metrics.FirstOrDefault(m => m.Name == "drivers.running")?.Value ?? 0.0;

        if (count > 0.0)
        {
            Assert.InRange(running, 0.0, count);
        }
    }

    [Fact]
    public async Task CollectAsync_IsDeterministicModuleId()
    {
        var collector = new DriverAnalyzerCollector();

        Assert.Equal("M09", collector.ModuleId);
        Assert.Equal("M09", collector.ModuleId);
    }
}
