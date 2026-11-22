using EVO.Core.Contracts;
using EVO.Core.Findings;

namespace EVO.Core.Correlation;

/// <summary>
/// M14 — Bottleneck Engine: correlación de evidencias.
/// Consume el conjunto de métricas emitidas por otros módulos (M01–M12), las
/// agrega y produce hallazgos finales con confianza honesta (Warning / Probable).
/// </summary>
/// <remarks>
/// Principios de diseño (honestidad obligatoria):
/// <list type="bullet">
/// <item><description>NUNCA declara "confirmado": toda la cadena es posible / advertencia.</description></item>
/// <item><description>NUNCA declara cuello de botella por uso elevado sin corroboración.</description></item>
/// <item><description>Cada hallazgo declara su evidencia con nombre de módulo fuente.</description></item>
/// <item><description>Best-effort: si un módulo productor falla, se omite sin abortar.</description></item>
/// </list>
/// Las reglas son triage (umbrales documentados), no diagnósticos calibrados.
/// </remarks>
public sealed class BottleneckEngineCollector : ICollector
{
    /// <summary>Identificador del módulo.</summary>
    public string ModuleId => "M14";

    /// <summary>Umbral de sospecha de CPU (% de uso, M02). Triage, no diagnóstico.</summary>
    public const double CpuSuspectedPercent = 80.0;

    /// <summary>Umbral de corroboración de CPU (% de uso del proceso, M06).</summary>
    public const double CpuCorroboratedPercent = 80.0;

    /// <summary>Umbral de sospecha de MEMORY (% de memoria disponible baja, M03).</summary>
    public const double MemorySuspectedAvailablePercent = 10.0;

    /// <summary>Umbral de corroboración de MEMORY (page faults/sec, M03).</summary>
    public const double MemoryCorroboratedPageFaults = 100.0;

    /// <summary>Umbral de sospecha de HEALTH (eventos error+warning en 24 h, M12).</summary>
    public const double HealthSuspectedEvents = 50.0;

    /// <summary>Umbral de corroboración de HEALTH (errores de sistema en 24 h, M12).</summary>
    public const double HealthCorroboratedSystemErrors = 10.0;

    private readonly List<ICollector> _sources;

    /// <summary>
    /// Crea el motor con los módulos fuente cuyas métricas se correlacionan.
    /// </summary>
    /// <param name="sources">Módulos productores (por ejemplo M02, M03, M06, M12).</param>
    public BottleneckEngineCollector(IEnumerable<ICollector> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        _sources = sources.ToList();
        if (_sources.Count == 0)
            throw new ArgumentException("El motor requiere al menos un módulo fuente.", nameof(sources));
    }

    public async Task<CollectResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var collectedAt = DateTimeOffset.UtcNow;
        var metrics = new Dictionary<string, double>();

        // 1. Recolectar desde cada fuente, best-effort por fuente (no abortar).
        foreach (var source in _sources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ICollector collector = source;
            CollectResult? result = null;
            try { result = await collector.CollectAsync(cancellationToken).ConfigureAwait(false); }
            catch { continue; } // mejor esfuerzo: omitir la fuente

            if (result is null)
                continue;

            foreach (var metric in result.Metrics)
            {
                // Últimos gana: si dos módulos emiten el mismo nombre, el más reciente prevalece.
                metrics[metric.Name] = metric.Value;
            }
        }

        // 2. Evaluar reglas por área.
        var findings = new List<DiagnosticFinding>();
        var corroboratedCount = 0;

        EvaluateCpu(metrics, collectedAt, findings);
        EvaluateMemory(metrics, collectedAt, findings);
        EvaluateHealth(metrics, collectedAt, findings);

        foreach (var areaFinding in findings)
        {
            if (areaFinding.Confidence == FindingConfidence.Probable)
                corroboratedCount++;
        }

        // 3. Emitir métricas de correlación.
        var outMetrics = new List<Metric>
        {
            new("bottleneck.areas", "count", 3, collectedAt),
            new("bottleneck.suspected", "count", findings.Count, collectedAt),
            new("bottleneck.corroborated", "count", corroboratedCount, collectedAt),
        };

        return CollectResult.Create(ModuleId, outMetrics, findings);
    }

    // --- Reglas por área (triage, no diagnóstico) ---

    private static void EvaluateCpu(Dictionary<string, double> metrics, DateTimeOffset at, List<DiagnosticFinding> findings)
    {
        if (!metrics.TryGetValue("cpu.usage_percent", out var usage))
            return; // sin evidencia de CPU: no se emite nada

        if (usage < CpuSuspectedPercent)
            return; // por debajo del umbral: no sospecha

        bool corroborated = metrics.TryGetValue("process.top_cpu_percent", out var topProcCpu) && topProcCpu >= CpuCorroboratedPercent;
        findings.Add(new DiagnosticFinding
        {
            Id = Guid.NewGuid().ToString("N"),
            Category = "CPU",
            Title = corroborated
                ? "CPU presión detectada (evidencia corroborada)"
                : "CPU uso elevado (evidencia única)",
            Description = $"Uso de CPU {usage:F1} % (umbral {CpuSuspectedPercent:F0} %). Corroboración: {(corroborated ? "proceso de alto consumo (M06)" : "no disponible")}. Es una instantánea puntual; no es un cuello de botella confirmado.",
            Severity = Severity.Medium,
            Confidence = corroborated ? FindingConfidence.Probable : FindingConfidence.Warning,
            Evidence = new[]
            {
                new Evidence("CPU", "Uso de CPU instantáneo (M02)", $"{usage:F1} %"),
                new Evidence("Process", "CPU máximo por proceso (M06)", metrics.TryGetValue("process.top_cpu_percent", out var top) ? $"{top:F1} %" : "no disponible"),
            },
            SuspectedSources = corroborated
                ? new[] { "proceso de alto consumo", "servicio o proceso en background" }
                : new[] { "proceso de alto consumo" },
            Impact = "El uso elevado de CPU puede degradar la respuesta general del equipo en ese instante. Requiere corroboración y no constituye cuello de botella confirmado.",
            Recommendation = "Revisar el consumo por proceso (M06) y corroborar con monitoreo por intervalo (M16) antes de concluir un cuello de botella.",
            VerificationMethod = "Monitorear por intervalo (M16 — EVO Live) y verificar el ranking completo (M15) para confirmar o descartar.",
            Timestamp = at,
        });
    }

    private static void EvaluateMemory(Dictionary<string, double> metrics, DateTimeOffset at, List<DiagnosticFinding> findings)
    {
        if (!metrics.TryGetValue("ram.available_percent", out var availablePercent))
            return;

        if (availablePercent >= MemorySuspectedAvailablePercent)
            return;

        bool corroborated = metrics.TryGetValue("ram.page_faults_per_sec", out var faults) && faults >= MemoryCorroboratedPageFaults;
        findings.Add(new DiagnosticFinding
        {
            Id = Guid.NewGuid().ToString("N"),
            Category = "RAM",
            Title = corroborated
                ? "Presión de memoria detectada (paginación activa)"
                : "Presión de memoria (memoria disponible baja)",
            Description = $"Memoria disponible {availablePercent:F1} % del total (umbral {MemorySuspectedAvailablePercent:F0} %). Corroboración: {(corroborated ? "paginación activa (M03)" : "no disponible")}. Es una instantánea puntual; no es un cuello de botella confirmado.",
            Severity = Severity.Medium,
            Confidence = corroborated ? FindingConfidence.Probable : FindingConfidence.Warning,
            Evidence = new[]
            {
                new Evidence("RAM", "Memoria disponible (M03)", $"{availablePercent:F1} %"),
                new Evidence("RAM", "Page faults/sec (M03)", metrics.TryGetValue("ram.page_faults_per_sec", out var pf) ? $"{pf:F1}/sec" : "no disponible"),
            },
            SuspectedSources = new[] { "procesos de alto consumo", "servicios en background" },
            Impact = "La memoria baja puede degradar la respuesta general del equipo en ese instante. Requiere corroboración y no constituye cuello de botella confirmado.",
            Recommendation = "Verificar el consumo de memoria por proceso (M06) y corroborar con monitoreo por intervalo (M16).",
            VerificationMethod = "Monitorear por intervalo (M16 — EVO Live) y verificar el ranking completo (M15) para confirmar o descartar.",
            Timestamp = at,
        });
    }

    private static void EvaluateHealth(Dictionary<string, double> metrics, DateTimeOffset at, List<DiagnosticFinding> findings)
    {
        if (!metrics.TryGetValue("health.events_24h", out var events))
            return;

        if (events < HealthSuspectedEvents)
            return;

        bool corroborated = metrics.TryGetValue("health.system.errors", out var systemErrors) && systemErrors >= HealthCorroboratedSystemErrors;
        findings.Add(new DiagnosticFinding
        {
            Id = Guid.NewGuid().ToString("N"),
            Category = "Health",
            Title = corroborated
                ? "Eventos del sistema elevados (errores de sistema correlacionados)"
                : "Eventos del sistema elevados (evidencia única)",
            Description = $"Eventos error+warning en 24 h: {events:F0} (umbral {HealthSuspectedEvents:F0}). Corroboración: {(corroborated ? "errores de sistema (M12)" : "no disponible")}. Es una instantánea puntual; no es un diagnóstico confirmado.",
            Severity = Severity.Medium,
            Confidence = corroborated ? FindingConfidence.Probable : FindingConfidence.Warning,
            Evidence = new[]
            {
                new Evidence("Health", "Eventos (error+warning) en 24 h (M12)", $"{events:F0}"),
                new Evidence("Health", "Errores de sistema en 24 h (M12)", metrics.TryGetValue("health.system.errors", out var se) ? $"{se:F0}" : "no disponible"),
            },
            SuspectedSources = corroborated
                ? new[] { "driver o servicio problemático", "actualización o proceso en background" }
                : new[] { "driver o servicio problemático" },
            Impact = "La acumulación de eventos puede indicar instabilidad del sistema en esa ventana. Requiere corroboración (M14/M12) y no constituye diagnóstico confirmado.",
            Recommendation = "Verificar el detalle de eventos (M12) y corroborar con la correlación de evidencias (M14) para concluir la causa probable.",
            VerificationMethod = "Revisar el detalle de eventos en el dashboard (M15) y corroborar con monitoreo por intervalo (M16).",
            Timestamp = at,
        });
    }
}
