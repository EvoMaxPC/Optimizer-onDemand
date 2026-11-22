namespace EVO.Core.Findings;

/// <summary>
/// Estatus/confianza del hallazgo. Los cinco estados del producto:
/// nunca presentamos una causa como confirmada cuando solamente exista una correlación.
/// </summary>
public enum FindingConfidence
{
    /// <summary>Problema confirmado (evidencia directa).</summary>
    Confirmed = 0,

    /// <summary>Problema probable (correlación con múltiples evidencias).</summary>
    Probable = 1,

    /// <summary>Advertencia (comportamiento fuera de lo esperado, no decisivo).</summary>
    Warning = 2,

    /// <summary>Comportamiento normal (dentro de lo esperado).</summary>
    Normal = 3,

    /// <summary>Datos insuficientes para confirmar o descartar.</summary>
    InsufficientData = 4,
}
