namespace EVO.Core.Contracts;

/// <summary>
/// Anomalía: comportamiento que merece investigación.
/// No afirma una causa ni un diagnóstico confirmado; solo indica dónde mirar.
/// </summary>
public sealed class Anomaly
{
    public required string ModuleId { get; init; }
    public required string MetricName { get; init; }
    public required string Description { get; init; }
    public double? Threshold { get; init; }
    public required DateTimeOffset DetectedAt { get; init; }

    /// <summary>
    /// Crea una anomalía con timestamp UTC actual.
    /// </summary>
    /// <param name="moduleId">Identificador del módulo (ej. "M02").</param>
    /// <param name="metricName">Nombre de la métrica observada.</param>
    /// <param name="description">Descripción del comportamiento.</param>
    /// <param name="threshold">Umbral esperado, opcional.</param>
    /// <returns>La anomalía creada.</returns>
    public static Anomaly Create(string moduleId, string metricName, string description, double? threshold = null)
        => new()
        {
            ModuleId = moduleId,
            MetricName = metricName,
            Description = description,
            Threshold = threshold,
            DetectedAt = DateTimeOffset.UtcNow,
        };
}
