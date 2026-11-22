using EVO.Collectors.CPU;
using EVO.Core.Findings;

namespace EVO.Collectors.Tests;

public class CpuAnalyzerCollectorTests
{
    [Fact]
    public async Task CollectAsync_ReturnsResultWithModuleId()
    {
        var collector = new CpuAnalyzerCollector();
        var result = await collector.CollectAsync();

        Assert.Equal("M02", collector.ModuleId);
        Assert.Equal("M02", result.ModuleId);
        Assert.NotNull(result.Metrics);
        Assert.NotNull(result.Findings);
        Assert.True(result.CollectedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task CollectAsync_UsageMetricIsWithinValidRange()
    {
        var collector = new CpuAnalyzerCollector();
        var result = await collector.CollectAsync();

        var usage = result.Metrics.FirstOrDefault(m => m.Name == "cpu.usage_percent");
        if (usage is null)
        {
            // CIM de rendimiento no disponible en este entorno: la sección se omite.
            return;
        }

        Assert.InRange(usage.Value, 0.0, 100.0);
        Assert.Equal("%", usage.Unit);
    }

    [Fact]
    public async Task CollectAsync_FindingIsWarningNotConfirmed()
    {
        var collector = new CpuAnalyzerCollector();
        var result = await collector.CollectAsync();

        // Si hay hallazgo (uso elevado en el instante), NUNCA puede ser "confirmado".
        foreach (var finding in result.Findings)
        {
            Assert.Equal("CPU", finding.Category);
            Assert.Equal(FindingConfidence.Warning, finding.Confidence);
            Assert.NotEmpty(finding.Evidence);
            Assert.NotEmpty(finding.SuspectedSources);
            Assert.False(string.IsNullOrWhiteSpace(finding.Recommendation));
            Assert.False(string.IsNullOrWhiteSpace(finding.VerificationMethod));
        }
    }

    [Fact]
    public async Task CollectAsync_QueueLengthIsNonNegative()
    {
        var collector = new CpuAnalyzerCollector();
        var result = await collector.CollectAsync();

        var queue = result.Metrics.FirstOrDefault(m => m.Name == "cpu.queue_length");
        if (queue is null)
        {
            // Columna de cola puede no existir en este entorno: la métrica se omite.
            return;
        }

        Assert.True(queue.Value >= 0.0);
        Assert.Equal("jobs", queue.Unit);
    }

    [Fact]
    public async Task CollectAsync_IsDeterministicModuleId()
    {
        var collector = new CpuAnalyzerCollector();

        Assert.Equal("M02", collector.ModuleId);
        Assert.Equal("M02", collector.ModuleId);
    }
}
