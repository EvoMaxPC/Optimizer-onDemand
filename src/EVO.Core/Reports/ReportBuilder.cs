using System.Text.Json;
using EVO.Core.Contracts;

namespace EVO.Core.Reports;

/// <summary>Hallazgo resumido en el informe (sin recortar: los campos se exponen tal cual).</summary>
public sealed record ReportFinding(
    string Category,
    string Title,
    string Severity,
    string Confidence,
    string Description);

/// <summary>
/// Documento de informe: resumen honesto del resultado del motor M14
/// (o de cualquier módulo) — no duplica, resume. Sin promesas: el JSON
/// solo contiene datos reales del resultado.
/// </summary>
public sealed record ReportDocument(
    string Id,
    DateTimeOffset GeneratedAt,
    string ModuleId,
    int FindingsTotal,
    int Corroborated,
    int Warning,
    int MetricsTotal,
    IReadOnlyList<ReportFinding> Findings)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Serialización en JSON (indentado).</summary>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);
}

/// <summary>
/// M15 — Informe: construir un documento de resumen JSON a partir de un
/// <see cref="CollectResult"/>. La construcción es lógica pura (sin I/O ni de '.json' a disco).
/// La escritura del archivo queda en el caller (responsabilidad del dashboard / la app).
/// </summary>
public static class ReportBuilder
{
    /// <summary>
    /// Construye el documento de informe a partir del resultado del motor.
    /// </summary>
    /// <param name="result">Resultado del motor M14 (o de cualquier módulo).</param>
    /// <exception cref="ArgumentNullException">Si result es null.</exception>
    public static ReportDocument Build(CollectResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var findings = result.Findings
            .Select(f => new ReportFinding(
                f.Category,
                f.Title,
                f.Severity.ToString(),
                f.Confidence.ToString(),
                f.Description))
            .ToList();

        var corroborated = findings.Count(f => f.Confidence == "Probable");
        var warning = findings.Count(f => f.Confidence == "Warning");

        return new ReportDocument(
            Id: Guid.NewGuid().ToString("N"),
            GeneratedAt: result.CollectedAt,
            ModuleId: result.ModuleId,
            FindingsTotal: findings.Count,
            Corroborated: corroborated,
            Warning: warning,
            MetricsTotal: result.Metrics.Count,
            Findings: findings);
    }
}
