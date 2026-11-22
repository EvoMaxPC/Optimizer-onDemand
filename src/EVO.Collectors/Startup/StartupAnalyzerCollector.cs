using System.Management;
using EVO.Core.Contracts;
using EVO.Core.Findings;

namespace EVO.Collectors.Startup;

/// <summary>
/// M08 — Startup Analyzer: auditoría de elementos de arranque.
/// Mide el inventario de elementos de arranque de Windows (vía <c>Win32_StartupCommand</c>):
/// total, fuentes (registro vs. carpetas de inicio) y elementos sin descripción.
/// NO declara causas ni cuellos de botella: es una auditoría de inventario.
/// </summary>
/// <remarks>
/// Recolección best-effort: si la consulta falla (entorno sin WMI de arranque,
/// propiedades restringidas), la sección se omite sin abortar.
/// Los detalles por elemento (nombre, comando, usuario) quedan fuera de alcance —
/// el dashboard (M15) y la correlación (M14) los consumen.
/// </remarks>
public sealed class StartupAnalyzerCollector : ICollector
{
    /// <summary>Identificador del módulo.</summary>
    public string ModuleId => "M08";

    public Task<CollectResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var collectedAt = DateTimeOffset.UtcNow;
        var metrics = new List<Metric>();

        int total = 0;
        int registry = 0;
        int folder = 0;
        int noDescription = 0;

        try
        {
            CollectStartup(ref total, ref registry, ref folder, ref noDescription);
        }
        catch { /* mejor esfuerzo: omitir la sección */ }

        metrics.Add(new Metric("startup.count", "count", total, collectedAt));
        metrics.Add(new Metric("startup.registry", "count", registry, collectedAt));
        metrics.Add(new Metric("startup.folder", "count", folder, collectedAt));
        metrics.Add(new Metric("startup.no_description", "count", noDescription, collectedAt));

        // M08 es auditoría: produce solo métricas, sin hallazgos.
        return Task.FromResult(CollectResult.Create(ModuleId, metrics, Array.Empty<DiagnosticFinding>()));
    }

    private static void CollectStartup(
        ref int total,
        ref int registry,
        ref int folder,
        ref int noDescription)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT Name, Location, Description FROM Win32_StartupCommand");

        foreach (ManagementObject row in searcher.Get())
        {
            total++;

            var location = row["Location"]?.ToString()?.Trim() ?? "";
            var description = row["Description"]?.ToString()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(description))
                noDescription++;

            if (location.Contains("Startup", StringComparison.OrdinalIgnoreCase)
                || location.Contains("Startup Folder", StringComparison.OrdinalIgnoreCase))
            {
                folder++;
            }
            else
            {
                registry++;
            }
        }
    }
}
