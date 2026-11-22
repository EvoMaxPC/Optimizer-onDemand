using EVO.Collectors.Services;

namespace EVO.Collectors.Tests;

public class ServicesAuditorCollectorTests
{
    [Fact]
    public async Task CollectAsync_ReturnsResultWithModuleId()
    {
        var collector = new ServicesAuditorCollector();
        var result = await collector.CollectAsync();

        Assert.Equal("M07", collector.ModuleId);
        Assert.Equal("M07", result.ModuleId);
        Assert.NotNull(result.Metrics);
        Assert.NotNull(result.Findings);
        Assert.True(result.CollectedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task CollectAsync_CountIsAtLeastZero()
    {
        var collector = new ServicesAuditorCollector();
        var result = await collector.CollectAsync();

        var count = result.Metrics.FirstOrDefault(m => m.Name == "services.count");
        Assert.NotNull(count);
        Assert.True(count!.Value >= 0.0);
        Assert.Equal("count", count.Unit);
    }

    [Fact]
    public async Task CollectAsync_EmitsNoFindings()
    {
        // M07 es auditoría: produce solo métricas, sin hallazgos.
        var collector = new ServicesAuditorCollector();
        var result = await collector.CollectAsync();

        Assert.Empty(result.Findings);
    }

    [Fact]
    public async Task CollectAsync_RunningIsAtMostCount()
    {
        var collector = new ServicesAuditorCollector();
        var result = await collector.CollectAsync();

        var count = result.Metrics.FirstOrDefault(m => m.Name == "services.count")?.Value ?? 0.0;
        var running = result.Metrics.FirstOrDefault(m => m.Name == "services.running")?.Value ?? 0.0;

        if (count > 0.0)
        {
            Assert.InRange(running, 0.0, count);
        }
    }

    [Fact]
    public async Task CollectAsync_IsDeterministicModuleId()
    {
        var collector = new ServicesAuditorCollector();

        Assert.Equal("M07", collector.ModuleId);
        Assert.Equal("M07", collector.ModuleId);
    }
}
