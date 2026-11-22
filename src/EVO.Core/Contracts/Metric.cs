namespace EVO.Core.Contracts;

/// <summary>
/// Métrica: dato medido por un recolector durante una observación.
/// No es una conclusión; es el insumo de la correlación.
/// </summary>
public sealed record Metric(string Name, string Unit, double Value, DateTimeOffset Timestamp);
