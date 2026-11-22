namespace EVO.Core.Contracts;

/// <summary>
/// Métrica: dato medido por un recolector durante una observación.
/// No es una conclusión; es el insumo de la correlación.
/// </summary>
/// <remarks>
/// El dato es numérico (<see cref="Value"/>) o string (<see cref="StringValue"/>),
/// uno de los dos es no-null. Los inventarios (M01) producen hechos string;
/// los análisis (M02+) producirán magnitudes numéricas normalizables.
/// </remarks>
public sealed record Metric(
    string Name,
    string Unit,
    double Value,
    DateTimeOffset Timestamp,
    string? StringValue = null);
