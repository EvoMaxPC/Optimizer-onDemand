using EVO.Collectors.Memory;
using EVO.Core.Findings;

namespace EVO.Collectors.Tests;

public class RamAnalyzerCollectorTests
{
    [Fact]
    public async Task CollectAsync_ReturnsResultWithModuleId()
    {
        var collector = new RamAnalyzerCollector();
        var result = await collector.CollectAsync();

        Assert.Equal("M03", collector.ModuleId);
        Assert.Equal("M03", result.ModuleId);
        Assert.NotNull(result.Metrics);
        Assert.NotNull(result.Findings);
        Assert.True(result.CollectedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task CollectAsync_AvailablePercentIsWithinValidRange()
    {
        var collector = new RamAnalyzerCollector();
        var result = await collector.CollectAsync();

        var pct = result.Metrics.FirstOrDefault(m => m.Name == "ram.available_percent");
        if (pct is null)
        {
            // Rendimiento de memoria no disponible en este entorno: la métrica se omite.
            return;
        }

        Assert.InRange(pct.Value, 0.0, 100.0);
        Assert.Equal("%", pct.Unit);
    }

    [Fact]
    public async Task CollectAsync_FindingIsWarningNotConfirmed()
    {
        var collector = new RamAnalyzerCollector();
        var result = await collector.CollectAsync();

        // Si hay hallazgo (memoria baja en el instante), NUNCA puede ser "confirmado".
        foreach (var finding in result.Findings)
        {
            Assert.Equal("RAM", finding.Category);
            Assert.Equal(FindingConfidence.Warning, finding.Confidence);
            Assert.NotEmpty(finding.Evidence);
            Assert.NotEmpty(finding.SuspectedSources);
            Assert.False(string.IsNullOrWhiteSpace(finding.Recommendation));
            Assert.False(string.IsNullOrWhiteSpace(finding.VerificationMethod));
        }
    }

    [Fact]
    public async Task CollectAsync_AvailableMbIsNonNegative()
    {
        var collector = new RamAnalyzerCollector();
        var result = await collector.CollectAsync();

        var avail = result.Metrics.FirstOrDefault(m => m.Name == "ram.available_mb");
        if (avail is null)
        {
            return;
        }

        Assert.True(avail.Value >= 0.0);
        Assert.Equal("MB", avail.Unit);
    }

    [Fact]
    public async Task CollectAsync_CommittedMbIsNonNegative()
    {
        var collector = new RamAnalyzerCollector();
        var result = await collector.CollectAsync();

        var committed = result.Metrics.FirstOrDefault(m => m.Name == "ram.committed_mb");
        if (committed is null)
        {
            return;
        }

        Assert.True(committed.Value >= 0.0);
        Assert.Equal("MB", committed.Unit);
    }

    [Fact]
    public async Task CollectAsync_IsDeterministicModuleId()
    {
        var collector = new RamAnalyzerCollector();

        Assert.Equal("M03", collector.ModuleId);
        Assert.Equal("M03", collector.ModuleId);
    }
}
