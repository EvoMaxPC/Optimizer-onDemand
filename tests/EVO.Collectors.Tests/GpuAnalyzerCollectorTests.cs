using EVO.Collectors.GPU;

namespace EVO.Collectors.Tests;

public class GpuAnalyzerCollectorTests
{
    [Fact]
    public async Task CollectAsync_ReturnsResultWithModuleId()
    {
        var collector = new GpuAnalyzerCollector();
        var result = await collector.CollectAsync();

        Assert.Equal("M05", collector.ModuleId);
        Assert.Equal("M05", result.ModuleId);
        Assert.NotNull(result.Metrics);
        Assert.NotNull(result.Findings);
    }

    [Fact]
    public async Task CollectAsync_CountIsAtLeastZero()
    {
        var collector = new GpuAnalyzerCollector();
        var result = await collector.CollectAsync();

        var count = result.Metrics.FirstOrDefault(m => m.Name == "gpu.adapters");
        Assert.NotNull(count);
        Assert.True(count!.Value >= 0.0);
    }

    [Fact]
    public async Task CollectAsync_EmitsNoFindings()
    {
        var collector = new GpuAnalyzerCollector();
        var result = await collector.CollectAsync();

        Assert.Empty(result.Findings);
    }

    [Fact]
    public async Task CollectAsync_TotalRamIsNonNegative()
    {
        var collector = new GpuAnalyzerCollector();
        var result = await collector.CollectAsync();

        var total = result.Metrics.FirstOrDefault(m => m.Name == "gpu.total_adapter_ram_mb");
        Assert.NotNull(total);
        Assert.True(total!.Value >= 0.0);
    }
}
