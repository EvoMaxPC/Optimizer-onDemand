using System.Management;
using EVO.Core.Contracts;
using EVO.Core.Findings;

namespace EVO.Collectors.Processes;

/// <summary>
/// M06 — Process Intelligence: consumo por proceso.
/// Mide el ranking de procesos por uso de CPU y por memoria (working set) para
/// detectar los principales consumidores. NO declara causas ni cuellos de botella:
/// eso pertenece a M14 — Bottleneck Engine (correlación de evidencia).
/// </summary>
/// <remarks>
/// Recolección best-effort: si la consulta de rendimiento de procesos falla,
/// la sección se omite sin abortar. Métricas instantáneas, no series temporales
/// (eso es M16 — EVO Live). Solo se emite el máximo de cada atributo — un ranking
/// completo por proceso es trabajo del dashboard (M15) y de la correlación (M14).
/// </remarks>
public sealed class ProcessAnalyzerCollector : ICollector
{
    /// <summary>Identificador del módulo.</summary>
    public string ModuleId => "M06";

    /// <summary>
    /// Umbral de CPU (%) que dispara el hallazgo de advertencia. No constituye
    /// diagnóstico ni cuello de botella — solo indica que hay que mirar más.
    /// </summary>
    public const double HighCpuThresholdPercent = 80.0;

    public Task<CollectResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var collectedAt = DateTimeOffset.UtcNow;
        var metrics = new List<Metric>();
        var findings = new List<DiagnosticFinding>();

        string? topCpuName = null;
        double topCpu = 0.0;
        double topMemoryMb = 0.0;
        int processCount = 0;

        try
        {
            CollectProcesses(metrics, collectedAt, ref processCount, ref topCpuName, ref topCpu, ref topMemoryMb);
        }
        catch { /* mejor esfuerzo: omitir la sección */ }

        metrics.Add(new Metric("process.count", "count", processCount, collectedAt));
        metrics.Add(new Metric("process.top_cpu_percent", "%", topCpu, collectedAt));
        metrics.Add(new Metric("process.top_memory_mb", "MB", topMemoryMb, collectedAt));

        if (topCpu >= HighCpuThresholdPercent)
        {
            findings.Add(CreateHighCpuFinding(topCpu, topCpuName, collectedAt));
        }

        return Task.FromResult(CollectResult.Create(ModuleId, metrics, findings));
    }

    private static void CollectProcesses(
        List<Metric> metrics,
        DateTimeOffset at,
        ref int processCount,
        ref string? topCpuName,
        ref double topCpu,
        ref double topMemoryMb)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT Name, '% Processor Time', 'Working Set' FROM Win32_PerfFormattedData_PerfProc_Process");

        foreach (ManagementObject row in searcher.Get())
        {
            processCount++;

            var name = row["Name"]?.ToString();

            var cpuRaw = row["% Processor Time"];
            if (cpuRaw is not null && double.TryParse(cpuRaw.ToString(), out double cpu) && cpu > topCpu)
            {
                topCpu = cpu;
                topCpuName = name;
            }

            var memoryRaw = row["Working Set"];
            if (memoryRaw is not null && long.TryParse(memoryRaw.ToString(), out long workingSetBytes))
            {
                var memoryMb = workingSetBytes / (1024.0 * 1024);
                if (memoryMb > topMemoryMb)
                    topMemoryMb = memoryMb;
            }
        }
    }

    private static DiagnosticFinding CreateHighCpuFinding(double topCpu, string? topCpuName, DateTimeOffset at)
    {
        var name = string.IsNullOrWhiteSpace(topCpuName) ? "proceso" : topCpuName!;
        return DiagnosticFinding.Create(
            category: "Process",
            title: "Proceso de alto consumo de CPU detectado",
            description: $"El proceso «{name}» alcanzó {topCpu:F1} % de uso de CPU en el instante de la medición (umbral {HighCpuThresholdPercent:F0} %). Es una instantánea puntual; no constituye un cuello de botella confirmado.",
            severity: Severity.Medium,
            confidence: FindingConfidence.Warning,
            evidence: new[]
            {
                new Evidence("Process", "Proceso de mayor uso de CPU", $"{name} — {topCpu:F1} %"),
            },
            suspectedSources: new[]
            {
                "proceso de alto consumo",
                "servicio en background",
            },
            impact: "Un solo proceso consumiendo mucha CPU puede degradar la respuesta general del equipo en ese instante. Requiere corroboración y no debe interpretarse como cuello de botella confirmado.",
            recommendation: "Verificar el consumo por proceso (M06) y la correlación de evidencias (M14) antes de concluir un cuello de botella.",
            verificationMethod: "Monitorear por intervalo (M16 — EVO Live) o revisar el ranking completo en el dashboard (M15) para confirmar o descartar.");
    }
}
