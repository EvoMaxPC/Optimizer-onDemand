using System.Management;
using EVO.Core.Contracts;
using EVO.Core.Findings;

namespace EVO.Collectors.Windows;

/// <summary>
/// M12 — Windows Health: integridad y eventos.
/// Mide el inventario de eventos de error y advertencia en los logs System y
/// Application (ventana de 24 h). M12 es una auditoría: no correlaciona ni
/// declara causas ni cuellos de botella (eso pertenece a M14).
/// </summary>
/// <remarks>
/// Recolección best-effort: si la consulta falla (permisos, log inaccesible),
/// la sección se omite sin abortar. Los eventos individuales quedan fuera de
/// alcance — el dashboard (M15) y la correlación (M14) los consumen.
/// Los eventos son instantáneos: no son una serie temporal (eso es M16).
/// </remarks>
public sealed class WindowsHealthCollector : ICollector
{
    /// <summary>Identificador del módulo.</summary>
    public string ModuleId => "M12";

    /// <summary>Hora de ventana en horas (24 h).</summary>
    public const int WindowHours = 24;

    public Task<CollectResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var collectedAt = DateTimeOffset.UtcNow;
        var metrics = new List<Metric>();

        int systemErrors = 0;
        int systemWarnings = 0;
        int appErrors = 0;
        int appWarnings = 0;

        try
        {
            CollectEvents(
                "System",
                collectedAt.AddHours(-WindowHours),
                ref systemErrors, ref systemWarnings);

            CollectEvents(
                "Application",
                collectedAt.AddHours(-WindowHours),
                ref appErrors, ref appWarnings);
        }
        catch { /* mejor esfuerzo: omitir la sección */ }

        metrics.Add(new Metric("health.system.errors", "count", systemErrors, collectedAt));
        metrics.Add(new Metric("health.system.warnings", "count", systemWarnings, collectedAt));
        metrics.Add(new Metric("health.application.errors", "count", appErrors, collectedAt));
        metrics.Add(new Metric("health.application.warnings", "count", appWarnings, collectedAt));
        metrics.Add(new Metric("health.events_24h", "count", systemErrors + systemWarnings + appErrors + appWarnings, collectedAt));

        // M12 es auditoría: produce solo métricas, sin hallazgos.
        return Task.FromResult(CollectResult.Create(ModuleId, metrics, Array.Empty<DiagnosticFinding>()));
    }

    private static void CollectEvents(string logName, DateTimeOffset since, ref int errors, ref int warnings)
    {
        var sinceIso = since.ToString("yyyy-MM-ddTHH:mm:ssZ");
        using var searcher = new ManagementObjectSearcher(
            $"SELECT EventType, Logfile FROM Win32_NTLogEvent WHERE Logfile='{logName}' AND TimeWritten >= '{sinceIso}'");

        foreach (ManagementObject row in searcher.Get())
        {
            var typeRaw = row["EventType"];
            if (typeRaw is null)
                continue;

            var typeText = typeRaw.ToString();
            if (int.TryParse(typeText, out int type))
            {
                // EventType 1 = Error, 2 = Warning (0 = Critical, 3 = Info, 4 = Success Audit, 5 = Failure Audit)
                if (type == 1) errors++;
                else if (type == 2) warnings++;
            }
        }
    }
}
