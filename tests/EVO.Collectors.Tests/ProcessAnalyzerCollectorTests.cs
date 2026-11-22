using EVO.Collectors.Processes;
using EVO.Core.Findings;

namespace EVO.Collectors.Tests;

public class ProcessAnalyzerCollectorTests
{
    [Fact]
    public async Task CollectAsync_ReturnsResultWithModuleId()
    {
        var collector = new ProcessAnalyzerCollector();
        var result = await collector.CollectAsync();

        Assert.Equal("M06", collector.ModuleId);
        Assert.Equal("M06", result.ModuleId);
        Assert.NotNull(result.Metrics);
        Assert.NotNull(result.Findings);
        Assert.True(result.CollectedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task CollectAsync_CountsProcesses()
    {
        var collector = new ProcessAnalyzerCollector();
        var result = await collector.CollectAsync();

        var count = result.Metrics.FirstOrDefault(m => m.Name == "process.count");
        Assert.NotNull(count);
        Assert.True(count!.Value > 0.0);
    }

    [Fact]
    public async Task CollectAsync_TopCpuIsWithinValidRange()
    {
        var collector = new ProcessAnalyzerCollector();
        var result = await collector.CollectAsync();

        var topCpu = result.Metrics.FirstOrDefault(m => m.Name == "process.top_cpu_percent");
        Assert.NotNull(topCpu);
        Assert.InRange(topCpu!.Value, 0.0, 100.0);
        Assert.Equal("%", topCpu.Unit);
    }

    [Fact]
    public async Task CollectAsync_TopMemoryIsNonNegative()
    {
        var collector = new ProcessAnalyzerCollector();
        var result = await collector.CollectAsync();

        var topMem = result.Metrics.FirstOrDefault(m => m.Name == "process.top_memory_mb");
        Assert.NotNull(topMem);
        Assert.True(topMem!.Value >= 0.0);
        Assert.Equal("MB", topMem.Unit);
    }

    [Fact]
    public async Task CollectAsync_FindingIsWarningNotConfirmed()
    {
        var collector = new ProcessAnalyzerCollector();
        var result = await collector.CollectAsync();

        // Si hay hallazgo (proceso de alto consumo en el instante), NUNCA puede ser "confirmado".
        foreach (var finding in result.Findings)
        {
            Assert.Equal("Process", finding.Category);
            Assert.Equal(FindingConfidence.Warning, finding.Confidence);
            Assert.NotEmpty(finding.Evidence);
            Assert.False(string.IsNullOrWhiteSpace(finding.Recommendation));
            Assert.False(string.IsNullOrWhiteSpace(finding.VerificationMethod));
        }
    }

    [Fact]
    public async Task CollectAsync_IsDeterministicModuleId()
    {
        var collector = new ProcessAnalyzerCollector();

        Assert.Equal("M06", collector.ModuleId);
        Assert.Equal("M06", collector.ModuleId);
    }
}
