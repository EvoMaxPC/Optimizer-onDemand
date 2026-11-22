namespace EVO.Core.Findings;

/// <summary>
/// Evidencia: dato que respalda una conclusión.
/// </summary>
/// <param name="Source">Origen de la evidencia (ej. "Storage", "ETW").</param>
/// <param name="Summary">Descripción corta del dato.</param>
/// <param name="Value">Valor opcional (ej. "95.7 GB/s").</param>
public sealed record Evidence(string Source, string Summary, string? Value = null);
