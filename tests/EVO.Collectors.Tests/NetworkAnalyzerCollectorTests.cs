using EVO.Collectors.Network;

namespace EVO.Collectors.Tests;

public class NetworkAnalyzerCollectorTests
{
    [Fact]
    public async Task CollectAsync_ReturnsResultWithModuleId()
    {
        var collector = new NetworkAnalyzerCollector();
        var result = await collector.CollectAsync();

        Assert.Equal("M11", collector.ModuleId);
        Assert.Equal("M11", result.ModuleId);
        Assert.NotNull(result.Metrics);
        Assert.NotNull(result.Findings);
    }

    [Fact]
    public async Task CollectAsync_CountIsAtLeastZero()
    {
        var collector = new NetworkAnalyzerCollector();
        var result = await collector.CollectAsync();

        var count = result.Metrics.FirstOrDefault(m => m.Name == "network.adapters.count");
        Assert.NotNull(count);
        Assert.True(count!.Value >= 0.0);
    }

    [Fact]
    public async Task CollectAsync_EmitsNoFindings()
    {
        var collector = new NetworkAnalyzerCollector();
        var result = await collector.CollectAsync();

        Assert.Empty(result.Findings);
    }

    [Fact]
    public async Task CollectAsync_EnabledDoesNotExceedTotal()
    {
        var collector = new NetworkAnalyzerCollector();
        var result = await collector.CollectAsync();

        var total = result.Metrics.FirstOrDefault(m => m.Name == "network.adapters.count")?.Value ?? 0.0;
        var enabled = result.Metrics.FirstOrDefault(m => m.Name == "network.adapters.enabled")?.Value ?? 0.0;

        if (total > 0.0)
        {
            Assert.InRange(enabled, 0.0, total);
        }
    }
}
