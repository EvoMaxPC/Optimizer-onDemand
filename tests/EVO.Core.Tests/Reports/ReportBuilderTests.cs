using EVO.Core.Contracts;
using EVO.Core.Findings;
using EVO.Core.Reports;

namespace EVO.Core.Tests.Reports;

public class ReportBuilderTests
{
    private static DiagnosticFinding MakeFinding(FindingConfidence confidence, Severity severity = Severity.Medium, string category = "CPU")
        => new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Category = category,
            Title = $"Hallazgo de prueba ({confidence})",
            Description = "Descripción honesta del hallazgo.",
            Severity = severity,
            Confidence = confidence,
            Evidence = new[] { new Evidence("CPU", "Evidencia", "92 %") },
            SuspectedSources = new[] { "proceso" },
            Impact = "Impacto",
            Recommendation = "Recomendación",
            VerificationMethod = "Verificación",
            Timestamp = DateTimeOffset.UtcNow,
        };

    [Fact]
    public void Build_AggregatesCounts()
    {
        var result = CollectResult.Create(
            "M14",
            new[] { new Metric("bottleneck.suspected", "count", 2, DateTimeOffset.UtcNow) },
            new[]
            {
                MakeFinding(FindingConfidence.Probable),
                MakeFinding(FindingConfidence.Warning),
            });

        var doc = ReportBuilder.Build(result);

        Assert.Equal("M14", doc.ModuleId);
        Assert.Equal(2, doc.FindingsTotal);
        Assert.Equal(1, doc.Corroborated);
        Assert.Equal(1, doc.Warning);
        Assert.Equal(1, doc.MetricsTotal);
        Assert.Equal(2, doc.Findings.Count);
        Assert.Equal("Probable", doc.Findings[0].Confidence);
        Assert.Equal("Warning", doc.Findings[1].Confidence);
    }

    [Fact]
    public void Build_EmptyResult_HasZeroCounts()
    {
        var result = CollectResult.CreateEmpty("M14");

        var doc = ReportBuilder.Build(result);

        Assert.Equal(0, doc.FindingsTotal);
        Assert.Equal(0, doc.MetricsTotal);
        Assert.Empty(doc.Findings);
    }

    [Fact]
    public void Build_FindingsDoNotDropInformation()
    {
        var result = CollectResult.Create(
            "M14",
            Array.Empty<Metric>(),
            new[] { MakeFinding(FindingConfidence.Probable, Severity.High, "RAM") });

        var doc = ReportBuilder.Build(result);

        var finding = doc.Findings[0];
        Assert.Equal("RAM", finding.Category);
        Assert.Equal("High", finding.Severity);
        Assert.Equal("Probable", finding.Confidence);
        Assert.False(string.IsNullOrWhiteSpace(finding.Title));
        Assert.False(string.IsNullOrWhiteSpace(finding.Description));
    }

    [Fact]
    public void ToJson_ContainsExpectedKeys()
    {
        var result = CollectResult.Create(
            "M14",
            new[] { new Metric("bottleneck.areas", "count", 3, DateTimeOffset.UtcNow) },
            new[] { MakeFinding(FindingConfidence.Probable) });

        var doc = ReportBuilder.Build(result);
        var json = doc.ToJson();

        Assert.Contains("\"moduleId\": \"M14\"", json);
        Assert.Contains("\"findingsTotal\": 1", json);
        Assert.Contains("\"corroborated\": 1", json);
        Assert.Contains("\"warning\": 0", json);
    }

    [Fact]
    public void Build_NullResult_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ReportBuilder.Build(null!));
    }
}
