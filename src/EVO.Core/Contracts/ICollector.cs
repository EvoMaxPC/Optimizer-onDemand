using EVO.Core.Findings;

namespace EVO.Core.Contracts;

/// <summary>
/// Contrato de entrada y salida de un módulo recolector (M01–M16).
/// Toda recolección produce métricas e, opcionalmente, hallazgos.
/// </summary>
public interface ICollector
{
    /// <summary>Identificador del módulo (ej. "M02").</summary>
    string ModuleId { get; }

    /// <summary>
    /// Recolecta métricas y hallazgos.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelación cooperativa.</param>
    /// <returns>Resultado de la recolección.</returns>
    Task<CollectResult> CollectAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado de la recolección: métricas y hallazgos, con timestamp de recolección.
/// </summary>
public sealed class CollectResult
{
    /// <summary>Identificador del módulo que produjo el resultado.</summary>
    public required string ModuleId { get; init; }

    /// <summary>Momento UTC de la recolección.</summary>
    public required DateTimeOffset CollectedAt { get; init; }

    /// <summary>Métricas recolectadas.</summary>
    public required IReadOnlyList<Metric> Metrics { get; init; }

    /// <summary>Hallazgos producidos en esta recolección.</summary>
    public required IReadOnlyList<DiagnosticFinding> Findings { get; init; }

    /// <summary>Resultado vacío con timestamp actual.</summary>
    public static CollectResult CreateEmpty(string moduleId)
        => new()
        {
            ModuleId = moduleId,
            CollectedAt = DateTimeOffset.UtcNow,
            Metrics = Array.Empty<Metric>(),
            Findings = Array.Empty<DiagnosticFinding>(),
        };

    /// <summary>Resultado con métricas y hallazgos.</summary>
    public static CollectResult Create(string moduleId, IEnumerable<Metric> metrics, IEnumerable<DiagnosticFinding> findings)
        => new()
        {
            ModuleId = moduleId,
            CollectedAt = DateTimeOffset.UtcNow,
            Metrics = metrics.ToList(),
            Findings = findings.ToList(),
        };
}
