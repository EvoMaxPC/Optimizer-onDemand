using System.Management;
using EVO.Core.Contracts;
using EVO.Core.Findings;

namespace EVO.Collectors.Storage;

/// <summary>
/// M04 — Storage Analyzer: latencia e I/O.
/// Mide, por disco lógico, la latencia media (sec/lectura y sec/escritura),
/// la longitud de cola, el tiempo de disco (%) y las tasas de lectura/escritura.
/// NO declara causas ni cuellos de botella: solo produce datos. Las decisiones
/// de alerta pertenecen a M14 — Bottleneck Engine, que correlaciona evidencia.
/// </summary>
/// <remarks>
/// Recolección best-effort: ante cualquier falla de consulta (driver raro,
/// contador deshabilitado), la sección o el disco afectado se omite sin abortar.
/// Las medidas son instantáneas, no series temporales (eso es M16 — EVO Live).
/// No se emiten hallazgos: los umbrales de latencia dependem del disco, del
/// workload y del equipo — inventar uno y llamarlo verificado sería falso.
/// </remarks>
public sealed class StorageAnalyzerCollector : ICollector
{
    /// <summary>Identificador del módulo.</summary>
    public string ModuleId => "M04";

    public Task<CollectResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var collectedAt = DateTimeOffset.UtcNow;
        var metrics = new List<Metric>();

        try { CollectUsage(metrics, collectedAt); }
        catch { /* mejor esfuerzo: omitir la sección */ }

        // M04 es agnóstico a hallazgos: produce solo métricas de almacenamiento.
        return Task.FromResult(CollectResult.Create(ModuleId, metrics, Array.Empty<DiagnosticFinding>()));
    }

    private static void CollectUsage(List<Metric> metrics, DateTimeOffset at)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT * FROM Win32_PerfFormattedData_PerfDisk_LogicalDisk");

        foreach (ManagementObject row in searcher.Get())
        {
            var nameRaw = row["Name"];
            var diskId = nameRaw is not null ? nameRaw.ToString()?.Trim() ?? "" : "";
            if (string.IsNullOrWhiteSpace(diskId))
                diskId = "unknown";

            CollectDisk(metrics, at, diskId, row);
        }
    }

    private static void CollectDisk(List<Metric> metrics, DateTimeOffset at, string diskId, ManagementObject row)
    {
        CollectNumeric(metrics, at, $"storage.{diskId}.percent_disk_time", "%", row, "% Disk Time", 0.0, 100.0);
        CollectNumeric(metrics, at, $"storage.{diskId}.avg_sec_read_ms", "ms", row, "Average Disk sec/Read", 0.0, null, value => value * 1000.0);
        CollectNumeric(metrics, at, $"storage.{diskId}.avg_sec_write_ms", "ms", row, "Average Disk sec/Write", 0.0, null, value => value * 1000.0);
        CollectNumeric(metrics, at, $"storage.{diskId}.avg_queue_length", "jobs", row, "Average Disk Queue Length", 0.0, null);
        CollectNumeric(metrics, at, $"storage.{diskId}.read_mb_sec", "MB/s", row, "Disk Read Bytes/sec", 0.0, null, value => value / (1024.0 * 1024));
        CollectNumeric(metrics, at, $"storage.{diskId}.write_mb_sec", "MB/s", row, "Disk Write Bytes/sec", 0.0, null, value => value / (1024.0 * 1024));
    }

    private static void CollectNumeric(
        List<Metric> metrics,
        DateTimeOffset at,
        string metricName,
        string unit,
        ManagementObject row,
        string propertyName,
        double min,
        double? max,
        Func<double, double>? transform = null)
    {
        var raw = row[propertyName];
        if (raw is null)
            return;

        if (!double.TryParse(raw.ToString(), out double value))
            return;

        value = transform?.Invoke(value) ?? value;

        if (value < min)
            return;
        if (max.HasValue && value > max.Value)
            return;

        metrics.Add(new Metric(metricName, unit, value, at));
    }
}
