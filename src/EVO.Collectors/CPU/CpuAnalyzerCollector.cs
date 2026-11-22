using System.Management;
using EVO.Core.Contracts;
using EVO.Core.Findings;

namespace EVO.Collectors.CPU;

/// <summary>
/// M02 — CPU Analyzer: saturación y limitaciones.
/// Mide el estado instantáneo del procesador (uso %, longitud de cola) e
/// emite un hallazgo de advertencia si el uso supera el umbral.
/// NO declara cuello de botella ni causa (eso es M14). Produce solo datos:
/// la correlación y causa probable pertenecen a M14 — Bottleneck Engine.
/// </summary>
/// <remarks>
/// Las métricas actuales son una instantánea puntual, no una serie temporal
/// (eso es M16 — EVO Live). Recolección best-effort: si la consulta de
/// rendimiento falla (driver raro, habilitado/deshabilitado), la sección se omite.
/// </remarks>
public sealed class CpuAnalyzerCollector : ICollector
{
    /// <summary>Identificador del módulo.</summary>
    public string ModuleId => "M02";

    /// <summary>
    /// Umbral de saturación (% de uso en la instantánea) que dispara el hallazgo de advertencia.
    /// No constituye diagnóstico: solo indica que hay que mirar más.
    /// </summary>
    public const double SaturationThresholdPercent = 90.0;

    public Task<CollectResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var collectedAt = DateTimeOffset.UtcNow;
        var metrics = new List<Metric>();
        var findings = new List<DiagnosticFinding>();

        double? usagePercent = null;
        double? queueLength = null;

        try { CollectUsage(metrics, collectedAt, ref usagePercent, ref queueLength); }
        catch { /* mejor esfuerzo: omitir la sección */ }

        if (usagePercent.HasValue && usagePercent.Value >= SaturationThresholdPercent)
        {
            findings.Add(CreateSaturationFinding(usagePercent.Value, collectedAt));
        }

        return Task.FromResult(CollectResult.Create(ModuleId, metrics, findings));
    }

    private static void CollectUsage(List<Metric> metrics, DateTimeOffset at, ref double? usagePercent, ref double? queueLength)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT '% Processor Time', 'Queue Length' FROM Win32_PerfFormattedData_PerfOS_Processor");

        foreach (ManagementObject row in searcher.Get())
        {
            var usageRaw = row["% Processor Time"];
            if (usageRaw is not null && double.TryParse(usageRaw.ToString(), out double usage))
            {
                usagePercent = usage;
                metrics.Add(new Metric("cpu.usage_percent", "%", usage, at));
            }

            var queueRaw = row["Queue Length"];
            if (queueRaw is not null && double.TryParse(queueRaw.ToString(), out double queue))
            {
                queueLength = queue;
                metrics.Add(new Metric("cpu.queue_length", "jobs", queue, at));
            }
        }
    }

    private static DiagnosticFinding CreateSaturationFinding(double usagePercent, DateTimeOffset at)
    {
        return DiagnosticFinding.Create(
            category: "CPU",
            title: "CPU saturada en el instante de la medición",
            description: $"El uso del procesador en el instante de la medición alcanzó {usagePercent:F1} % (umbral {SaturationThresholdPercent:F0} %). Es una instantánea puntual, no una serie temporal; no constituye un cuello de botella confirmado.",
            severity: Severity.Medium,
            confidence: FindingConfidence.Warning,
            evidence: new[]
            {
                new Evidence("CPU", "Uso de CPU en el instante de la medición", $"{usagePercent:F1} %"),
            },
            suspectedSources: new[]
            {
                "proceso de alto consumo",
                "servicio o proceso en background",
            },
            impact: "El uso elevado de CPU puede degradar la respuesta general del equipo en ese instante. Requiere corroboración y no debe interpretarse como cuello de botella confirmado.",
            recommendation: "Verificar el consumo por proceso (M06) y la correlación de evidencias (M14) antes de concluir un cuello de botella.",
            verificationMethod: "Monitorear por intervalo (M16 — EVO Live) o revisar el ranking de procesos (M06) para confirmar o descartar.");
    }
}
