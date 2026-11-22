using EVO.Core.Contracts;
using EVO.Core.Findings;

namespace EVO.Core.Tests.Contracts;

public class CollectResultTests
{
    [Fact]
    public void CreateEmpty_ReturnsZeroMetricsAndZeroFindings()
    {
        var result = CollectResult.CreateEmpty("M02");

        Assert.Equal("M02", result.ModuleId);
        Assert.Empty(result.Metrics);
        Assert.Empty(result.Findings);
        Assert.True(result.CollectedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Create_PreservesMetricsAndFindings()
    {
        var metrics = new[]
        {
            new Metric("cpu_usage_percent", "%", 42.5, DateTimeOffset.UtcNow),
            new Metric("disk_latency_ms", "ms", 12.0, DateTimeOffset.UtcNow),
        };

        var findings = new[]
        {
            DiagnosticFinding.Create(
                category: "CPU",
                title: "Elevado uso de CPU",
                description: "CPU elevada durante la observación.",
                severity: Severity.Medium,
                confidence: FindingConfidence.Probable,
                evidence: Array.Empty<Evidence>(),
                suspectedSources: Array.Empty<string>(),
                impact: "Rendimiento degradado",
                recommendation: "Investigar proceso dominante",
                verificationMethod: "Medir uso de CPU por 5 minutos"),
        };

        var result = CollectResult.Create("M02", metrics, findings);

        Assert.Equal(2, result.Metrics.Count);
        Assert.Equal("cpu_usage_percent", result.Metrics[0].Name);
        Assert.Equal(1, result.Findings.Count);
        Assert.Equal("M02", result.ModuleId);
    }

    [Fact]
    public void Metric_RecordsValues()
    {
        var metric = new Metric("ram_usage_percent", "%", 92.0, DateTimeOffset.Parse("2026-11-22T10:00:00Z"));

        Assert.Equal("ram_usage_percent", metric.Name);
        Assert.Equal("%", metric.Unit);
        Assert.Equal(92.0, metric.Value);
        Assert.Equal(DateTimeOffset.Parse("2026-11-22T10:00:00Z"), metric.Timestamp);
    }

    [Fact]
    public void Anomaly_Create_SetsTimestampAndThreshold()
    {
        var detected = DateTimeOffset.UtcNow;
        var anomaly = Anomaly.Create("M04", "disk_latency_ms", "Latencia elevada", threshold: 20.0);

        Assert.Equal("M04", anomaly.ModuleId);
        Assert.Equal("disk_latency_ms", anomaly.MetricName);
        Assert.Equal(20.0, anomaly.Threshold);
        Assert.True(anomaly.DetectedAt >= detected);
    }
}
