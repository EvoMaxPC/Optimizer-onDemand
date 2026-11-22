using EVO.Collectors.Windows;

namespace EVO.Collectors.Tests;

public class WindowsHealthCollectorTests
{
    [Fact]
    public async Task CollectAsync_ReturnsResultWithModuleId()
    {
        var collector = new WindowsHealthCollector();
        var result = await collector.CollectAsync();

        Assert.Equal("M12", collector.ModuleId);
        Assert.Equal("M12", result.ModuleId);
        Assert.NotNull(result.Metrics);
        Assert.NotNull(result.Findings);
        Assert.True(result.CollectedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task CollectAsync_CountsAreNonNegative()
    {
        var collector = new WindowsHealthCollector();
        var result = await collector.CollectAsync();

        foreach (var m in result.Metrics)
        {
            Assert.True(m.Value >= 0.0, $"metric {m.Name} must be >= 0");
            Assert.Equal("count", m.Unit);
        }
    }

    [Fact]
    public async Task CollectAsync_EmitsNoFindings()
    {
        // M12 es auditoría: produce solo métricas, sin hallazgos.
        var collector = new WindowsHealthCollector();
        var result = await collector.CollectAsync();

        Assert.Empty(result.Findings);
    }

    [Fact]
    public async Task CollectAsync_TotalIsConsistent()
    {
        var collector = new WindowsHealthCollector();
        var result = await collector.CollectAsync();

        double total = result.Metrics.FirstOrDefault(m => m.Name == "health.events_24h")?.Value ?? 0.0;
        double systemErr = result.Metrics.FirstOrDefault(m => m.Name == "health.system.errors")?.Value ?? 0.0;
        double systemWarn = result.Metrics.FirstOrDefault(m => m.Name == "health.system.warnings")?.Value ?? 0.0;
        double appErr = result.Metrics.FirstOrDefault(m => m.Name == "health.application.errors")?.Value ?? 0.0;
        double appWarn = result.Metrics.FirstOrDefault(m => m.Name == "health.application.warnings")?.Value ?? 0.0;

        Assert.Equal(total, systemErr + systemWarn + appErr + appWarn, 2);
    }

    [Fact]
    public async Task CollectAsync_IsDeterministicModuleId()
    {
        var collector = new WindowsHealthCollector();

        Assert.Equal("M12", collector.ModuleId);
        Assert.Equal("M12", collector.ModuleId);
    }
}
