using EVO.Core.Findings;

namespace EVO.Core.Tests.Findings;

public class DiagnosticFindingTests
{
    private static DiagnosticFinding CreateFinding(string? id = null)
        => DiagnosticFinding.Create(
            category: "RAM",
            title: "Presión de memoria",
            description: "Memoria disponible insuficiente con paginación activa.",
            severity: Severity.High,
            confidence: FindingConfidence.Probable,
            evidence: new[]
            {
                new Evidence("RAM", "Memoria disponible insuficiente", "92% utilizada"),
                new Evidence("RAM", "Paginación activa", "4.2 GB"),
            },
            suspectedSources: new[] { "Proceso X", "Proceso Y" },
            impact: "Tiempos de respuesta aumentados",
            recommendation: "Investigar los procesos responsables",
            verificationMethod: "Medir presión de memoria por 5 minutos",
            id: id);

    [Fact]
    public void Create_GeneratesIdAndTimestamp()
    {
        var before = DateTimeOffset.UtcNow;
        var finding = CreateFinding();
        var after = DateTimeOffset.UtcNow;

        Assert.False(string.IsNullOrWhiteSpace(finding.Id));
        Assert.True(finding.Timestamp >= before && finding.Timestamp <= after,
            $"Timestamp {finding.Timestamp:O} fuera del rango esperado");
        Assert.Equal(2, finding.Evidence.Count);
    }

    [Fact]
    public void Create_WithCustomId_KeepsId()
    {
        var finding = CreateFinding(id: "custom-id-123");
        Assert.Equal("custom-id-123", finding.Id);
    }

    [Fact]
    public void Create_PreservesEvidenceAndSuspectedSources()
    {
        var finding = CreateFinding();

        Assert.Equal(2, finding.Evidence.Count);
        Assert.Equal("RAM", finding.Evidence[0].Source);
        Assert.Equal("RAM", finding.Evidence[1].Source);
        Assert.Equal(2, finding.SuspectedSources.Count);
        Assert.Contains("Proceso X", finding.SuspectedSources);
    }

    [Fact]
    public void Create_EmptySuspectedSources_IsAllowed()
    {
        var finding = DiagnosticFinding.Create(
            category: "CPU",
            title: "Carga normal",
            description: "Sin anomalías.",
            severity: Severity.Low,
            confidence: FindingConfidence.Normal,
            evidence: new Evidence[] { new Evidence("CPU", "Uso medio", "42%") },
            suspectedSources: new string[] { },
            impact: "Ninguno",
            recommendation: "Ninguna",
            verificationMethod: "Repetir medición en 1 hora");

        Assert.Empty(finding.SuspectedSources);
        Assert.Equal(FindingConfidence.Normal, finding.Confidence);
    }

    [Fact]
    public void Create_StatesApprovedConfidenceStates()
    {
        // Los cinco estados del producto deben existir y estar ordenados.
        var values = Enum.GetValues<FindingConfidence>();

        Assert.Contains(FindingConfidence.Confirmed, values);
        Assert.Contains(FindingConfidence.Probable, values);
        Assert.Contains(FindingConfidence.Warning, values);
        Assert.Contains(FindingConfidence.Normal, values);
        Assert.Contains(FindingConfidence.InsufficientData, values);
    }
}
