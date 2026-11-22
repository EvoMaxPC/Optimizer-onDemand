namespace EVO.Core.Findings;

/// <summary>
/// Hallazgo estructurado producido por un motor de diagnóstico.
/// Modelo fiel a la especificación (PARTE I.4): nunca presentamos una
/// causa como confirmada cuando solamente exista una correlación.
/// </summary>
public sealed class DiagnosticFinding
{
    /// <summary>Identificador único del hallazgo (GUID normalizado).</summary>
    public required string Id { get; init; }

    /// <summary>Categoría del hallazgo (ej. "CPU", "RAM", "Storage").</summary>
    public required string Category { get; init; }

    /// <summary>Título corto del hallazgo.</summary>
    public required string Title { get; init; }

    /// <summary>Descripción detallada del hallazgo.</summary>
    public required string Description { get; init; }

    /// <summary>Severidad/impacto declarado.</summary>
    public required Severity Severity { get; init; }

    /// <summary>Confianza/estatus (Confirmed / Probable / Warning / Normal / InsufficientData).</summary>
    public required FindingConfidence Confidence { get; init; }

    /// <summary>Evidencias que respaldan el hallazgo.</summary>
    public required IReadOnlyList<Evidence> Evidence { get; init; }

    /// <summary>Fuentes sospechosas (nunca presentadas como confirmadas).</summary>
    public required IReadOnlyList<string> SuspectedSources { get; init; }

    /// <summary>Impacto sobre el equipo.</summary>
    public required string Impact { get; init; }

    /// <summary>Recomendación técnica justificada.</summary>
    public required string Recommendation { get; init; }

    /// <summary>Método de verificación recomendado.</summary>
    public required string VerificationMethod { get; init; }

    /// <summary>Timestamp UTC del hallazgo.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Crea un hallazgo con timestamp UTC actual.
    /// </summary>
    public static DiagnosticFinding Create(
        string category,
        string title,
        string description,
        Severity severity,
        FindingConfidence confidence,
        IReadOnlyList<Evidence> evidence,
        IReadOnlyList<string> suspectedSources,
        string impact,
        string recommendation,
        string verificationMethod,
        string? id = null)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(suspectedSources);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new DiagnosticFinding
        {
            Id = id ?? Guid.NewGuid().ToString("N"),
            Category = category,
            Title = title,
            Description = description,
            Severity = severity,
            Confidence = confidence,
            Evidence = evidence,
            SuspectedSources = suspectedSources,
            Impact = impact,
            Recommendation = recommendation,
            VerificationMethod = verificationMethod,
            Timestamp = DateTimeOffset.UtcNow,
        };
    }
}
