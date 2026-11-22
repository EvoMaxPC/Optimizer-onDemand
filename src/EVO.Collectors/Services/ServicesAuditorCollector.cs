using System.Management;
using EVO.Core.Contracts;
using EVO.Core.Findings;

namespace EVO.Collectors.Services;

/// <summary>
/// M07 — Services Auditor: auditoría de servicios de Windows.
/// Mide el inventario de servicios por estado (running/stopped) y por modo
/// de inicio (auto/manual/disabled) para detectar servicios sospechosos o
/// mal configurados. NO declara causas ni cuellos de botella: es una auditoría
/// de inventario. Las decisiones de alerta pertenecen a M14 — Bottleneck Engine.
/// </summary>
/// <remarks>
/// Recolección best-effort: si la consulta falla (driver raro, servicio
/// aislado), la sección se omite sin abortar. Los servicios individuales se
/// agregan en conteos — un detalle por servicio es trabajo del dashboard (M15).
/// </remarks>
public sealed class ServicesAuditorCollector : ICollector
{
    /// <summary>Identificador del módulo.</summary>
    public string ModuleId => "M07";

    public Task<CollectResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var collectedAt = DateTimeOffset.UtcNow;
        var metrics = new List<Metric>();

        int total = 0;
        int running = 0;
        int stopped = 0;
        int autoStart = 0;
        int manual = 0;
        int disabled = 0;

        try
        {
            CollectServices(
                ref total, ref running, ref stopped, ref autoStart, ref manual, ref disabled);
        }
        catch { /* mejor esfuerzo: omitir la sección */ }

        metrics.Add(new Metric("services.count", "count", total, collectedAt));
        metrics.Add(new Metric("services.running", "count", running, collectedAt));
        metrics.Add(new Metric("services.stopped", "count", stopped, collectedAt));
        metrics.Add(new Metric("services.auto_start", "count", autoStart, collectedAt));
        metrics.Add(new Metric("services.manual", "count", manual, collectedAt));
        metrics.Add(new Metric("services.disabled", "count", disabled, collectedAt));

        // M07 es auditoría: produce solo métricas de inventarios, sin hallazgos.
        return Task.FromResult(CollectResult.Create(ModuleId, metrics, Array.Empty<DiagnosticFinding>()));
    }

    private static void CollectServices(
        ref int total,
        ref int running,
        ref int stopped,
        ref int autoStart,
        ref int manual,
        ref int disabled)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT State, StartMode FROM Win32_Service");

        foreach (ManagementObject row in searcher.Get())
        {
            total++;

            var stateRaw = row["State"];
            if (stateRaw is not null)
            {
                var state = stateRaw.ToString()?.ToLowerInvariant();
                if (state == "running" || state == "startpending" || state == "continuepending")
                    running++;
                else
                    stopped++;
            }

            var modeRaw = row["StartMode"];
            if (modeRaw is not null)
            {
                var mode = modeRaw.ToString()?.ToLowerInvariant();
                switch (mode)
                {
                    case "auto": case "autostart": case "automatic":
                        autoStart++;
                        break;
                    case "demand": case "manual":
                        manual++;
                        break;
                    case "disabled":
                        disabled++;
                        break;
                }
            }
        }
    }
}
