using System.Management;
using EVO.Core.Contracts;
using EVO.Core.Findings;

namespace EVO.Collectors.GPU;

/// <summary>
/// M05 — GPU Analyzer: auditoría de adaptadores gráficos.
/// Mide el total y el máximo de memoria de video de los adaptadores.
/// NO mide porcentaje de uso/utilización: los contadores de GPU por vendor no
/// son confiables/disponibles vía CIM — queda pendiente un diseño explícito.
/// </summary>
/// <remarks>
/// Recolección best-effort: si la consulta falla, la sección se omite sin abortar.
/// Detalles por adaptador (nombre, modelo) quedan en M01 (inventario) — M05
/// agrega métricas resumen, sin string metrics (D-016 sigue propuesta).
/// </remarks>
public sealed class GpuAnalyzerCollector : ICollector
{
    /// <summary>Identificador del módulo.</summary>
    public string ModuleId => "M05";

    public Task<CollectResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var collectedAt = DateTimeOffset.UtcNow;
        var metrics = new List<Metric>();

        int adapters = 0;
        double totalRamMb = 0.0;
        double maxRamMb = 0.0;

        try
        {
            CollectGpu(ref adapters, ref totalRamMb, ref maxRamMb);
        }
        catch { /* mejor esfuerzo: omitir la sección */ }

        metrics.Add(new Metric("gpu.adapters", "count", adapters, collectedAt));
        metrics.Add(new Metric("gpu.total_adapter_ram_mb", "MB", totalRamMb, collectedAt));
        metrics.Add(new Metric("gpu.max_adapter_ram_mb", "MB", maxRamMb, collectedAt));

        // M05 es auditoría: produce solo métricas, sin hallazgos.
        return Task.FromResult(CollectResult.Create(ModuleId, metrics, Array.Empty<DiagnosticFinding>()));
    }

    private static void CollectGpu(ref int adapters, ref double totalRamMb, ref double maxRamMb)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT AdapterRAM FROM Win32_VideoController");

        foreach (ManagementObject row in searcher.Get())
        {
            adapters++;

            var ramRaw = row["AdapterRAM"];
            if (ramRaw is not null && long.TryParse(ramRaw.ToString(), out long ramBytes))
            {
                var ramMb = ramBytes / (1024.0 * 1024);
                totalRamMb += ramMb;
                if (ramMb > maxRamMb)
                    maxRamMb = ramMb;
            }
        }
    }
}
