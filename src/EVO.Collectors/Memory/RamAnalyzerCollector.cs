using System.Management;
using EVO.Core.Contracts;
using EVO.Core.Findings;

namespace EVO.Collectors.Memory;

/// <summary>
/// M03 — RAM Analyzer: presión y paginación.
/// Mide memoria disponible, memoria comprometida (committed) y actividad de paginación
/// (page faults y pages por segundo) para detectar presión de memoria en el instante.
/// NO declara cuello de botella ni causa (eso pertenece a M14).
/// </summary>
/// <remarks>
/// Recolección best-effort: si la consulta de rendimiento falla (driver raro,
/// contador deshabilitado, entorno sin PerfFormattedData), la sección se omite.
/// Métricas de tasa (<c>Page Faults/sec</c>, <c>Pages/sec</c>) son instantáneas:
/// un único snapshot del contador no equivale a una serie temporal (eso es M16).
/// </remarks>
public sealed class RamAnalyzerCollector : ICollector
{
    /// <summary>Identificador del módulo.</summary>
    public string ModuleId => "M03";

    /// <summary>
    /// Umbral de presión: memoria disponible por debajo de este porcentaje
    /// del total dispara el hallazgo de advertencia.
    /// No constituye diagnóstico: solo indica que hay que mirar más.
    /// </summary>
    public const double PressureThresholdPercent = 10.0;

    public Task<CollectResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var collectedAt = DateTimeOffset.UtcNow;
        var metrics = new List<Metric>();
        var findings = new List<DiagnosticFinding>();

        double? availableMb = null;
        double? committedMb = null;
        double? pageFaultsPerSec = null;
        double? pagesPerSec = null;
        double? totalMb = null;

        try { CollectMemoryUsage(metrics, collectedAt, ref availableMb, ref committedMb, ref pageFaultsPerSec, ref pagesPerSec); }
        catch { /* mejor esfuerzo: omitir la sección */ }

        try { CollectTotalMemory(ref totalMb); }
        catch { /* mejor esfuerzo: la métrica relativa se omite sin abortar */ }

        if (availableMb.HasValue && totalMb.HasValue && totalMb.Value > 0)
        {
            var availablePercent = availableMb.Value / totalMb.Value * 100.0;
            metrics.Add(new Metric("ram.available_percent", "%", availablePercent, collectedAt));

            if (availablePercent < PressureThresholdPercent)
            {
                findings.Add(CreatePressureFinding(availablePercent, collectedAt));
            }
        }

        return Task.FromResult(CollectResult.Create(ModuleId, metrics, findings));
    }

    private static void CollectMemoryUsage(
        List<Metric> metrics,
        DateTimeOffset at,
        ref double? availableMb,
        ref double? committedMb,
        ref double? pageFaultsPerSec,
        ref double? pagesPerSec)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT 'Available MBytes', 'Committed Bytes', 'Page Faults/sec', 'Pages/sec' FROM Win32_PerfFormattedData_PerfOS_Memory");

        foreach (ManagementObject row in searcher.Get())
        {
            var availableRaw = row["Available MBytes"];
            if (availableRaw is not null && double.TryParse(availableRaw.ToString(), out double available))
            {
                availableMb = available;
                metrics.Add(new Metric("ram.available_mb", "MB", available, at));
            }

            var committedRaw = row["Committed Bytes"];
            if (committedRaw is not null && long.TryParse(committedRaw.ToString(), out long committedBytes))
            {
                var committedMbLocal = committedBytes / (1024.0 * 1024);
                committedMb = committedMbLocal;
                metrics.Add(new Metric("ram.committed_mb", "MB", committedMbLocal, at));
            }

            var faultsRaw = row["Page Faults/sec"];
            if (faultsRaw is not null && double.TryParse(faultsRaw.ToString(), out double faults))
            {
                pageFaultsPerSec = faults;
                metrics.Add(new Metric("ram.page_faults_per_sec", "/sec", faults, at));
            }

            var pagesRaw = row["Pages/sec"];
            if (pagesRaw is not null && double.TryParse(pagesRaw.ToString(), out double pages))
            {
                pagesPerSec = pages;
                metrics.Add(new Metric("ram.pages_per_sec", "/sec", pages, at));
            }
        }
    }

    private static void CollectTotalMemory(ref double? totalMb)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");

        foreach (ManagementObject os in searcher.Get())
        {
            var totalRaw = os["TotalVisibleMemorySize"];
            if (totalRaw is not null && long.TryParse(totalRaw.ToString(), out long totalKb))
            {
                totalMb = totalKb / 1024.0;
            }
        }
    }

    private static DiagnosticFinding CreatePressureFinding(double availablePercent, DateTimeOffset at)
    {
        return DiagnosticFinding.Create(
            category: "RAM",
            title: "Presión de memoria detectada (memoria disponible baja)",
            description: $"La memoria disponible en el instante de la medición quedó en {availablePercent:F1} % del total (umbral {PressureThresholdPercent:F0} %). Es una instantánea puntual, no una serie temporal; no constituye un cuello de botella confirmado.",
            severity: Severity.Medium,
            confidence: FindingConfidence.Warning,
            evidence: new[]
            {
                new Evidence("RAM", "Memoria disponible relativa al total", $"{availablePercent:F1} %"),
            },
            suspectedSources: new[]
            {
                "procesos de alto consumo",
                "servicios en background",
            },
            impact: "El uso elevado de memoria puede degradar la respuesta general del equipo en ese instante. Requiere corroboración y no debe interpretarse como cuello de botella confirmado.",
            recommendation: "Verificar el consumo de memoria por proceso (M06) y la correlación de evidencias (M14) antes de concluir un cuello de botella.",
            verificationMethod: "Monitorear por intervalo (M16 — EVO Live) o revisar el ranking de procesos por memoria (M06) para confirmar o descartar.");
    }
}
