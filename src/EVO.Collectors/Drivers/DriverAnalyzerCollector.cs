using System.Management;
using EVO.Core.Contracts;
using EVO.Core.Findings;

namespace EVO.Collectors.Drivers;

/// <summary>
/// M09 — Driver Intelligence: auditoría de controladores de Windows.
/// Mide el inventario de controladores (vía Win32_SystemDriver): total,
/// estado (running/stopped), modo de inicio (auto/manual/disabled) y
/// estado de integridad (otros que OK). NO declara causas ni cuellos de botella:
/// es una auditoría de integridad.
/// </summary>
/// <remarks>
/// Recolección best-effort: cualquier falla de consulta (driver raro, permisos)
/// omite la sección sin abortar. Los detalles por controlador (nombre, ruta)
/// quedan fuera de alcance — el dashboard (M15) y la correlación (M14) los consumen.
/// </remarks>
public sealed class DriverAnalyzerCollector : ICollector
{
    /// <summary>Identificador del módulo.</summary>
    public string ModuleId => "M09";

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
        int degraded = 0;

        try
        {
            CollectDrivers(ref total, ref running, ref stopped, ref autoStart, ref manual, ref disabled, ref degraded);
        }
        catch { /* mejor esfuerzo: omitir la sección */ }

        metrics.Add(new Metric("drivers.count", "count", total, collectedAt));
        metrics.Add(new Metric("drivers.running", "count", running, collectedAt));
        metrics.Add(new Metric("drivers.stopped", "count", stopped, collectedAt));
        metrics.Add(new Metric("drivers.auto_start", "count", autoStart, collectedAt));
        metrics.Add(new Metric("drivers.manual", "count", manual, collectedAt));
        metrics.Add(new Metric("drivers.disabled", "count", disabled, collectedAt));
        metrics.Add(new Metric("drivers.degraded", "count", degraded, collectedAt));

        // M09 es auditoría: produce solo métricas, sin hallazgos.
        return Task.FromResult(CollectResult.Create(ModuleId, metrics, Array.Empty<DiagnosticFinding>()));
    }

    private static void CollectDrivers(
        ref int total,
        ref int running,
        ref int stopped,
        ref int autoStart,
        ref int manual,
        ref int disabled,
        ref int degraded)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT State, StartMode, Status FROM Win32_SystemDriver");

        foreach (ManagementObject row in searcher.Get())
        {
            total++;

            var stateRaw = row["State"];
            if (stateRaw is not null)
            {
                var state = stateRaw.ToString()?.ToLowerInvariant();
                if (state == "running" || state == "start pending" || state == "continue pending")
                    running++;
                else
                    stopped++;
            }

            var modeRaw = row["StartMode"];
            if (modeRaw is not null)
            {
                var mode = modeRaw.ToString()?.ToLowerInvariant();
                if (mode == "auto")
                    autoStart++;
                else if (mode == "manual")
                    manual++;
                else if (mode == "disabled")
                    disabled++;
            }

            var statusRaw = row["Status"];
            if (statusRaw is not null)
            {
                var status = statusRaw.ToString()?.ToLowerInvariant();
                if (status is "error" or "degraded" or "pred fail" or "stressed" or "nonrecover" or "no contact" or "lost comm")
                    degraded++;
            }
        }
    }
}
