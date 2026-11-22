using EVO.Core.Contracts;
using EVO.Core.Findings;

namespace EVO.Core.Live;

/// <summary>
/// M16 — EVO Live: monitorización temporal.
/// Muestrea los módulos fuente en una ventana acotada (N muestras) y emite
/// estadísticas temporales agregadas (promedio/máx/mín) por métrica clave.
/// NO es un stream continuo: es una serie acotada de muestras con resumen.
/// </summary>
/// <remarks>
/// Principios de diseño (honestidad obligatoria):
/// <list type="bullet">
/// <item><description>Ventana acotada y explícita: N muestras con intervalo. Sin espera indefinida.</description></item>
/// <item><description>Sin hallazgos: M16 produce evidencia temporal; los verdicts pertenecen a M14.</description></item>
/// <item><description>Best-effort: si un módulo falla en una muestra, se omite esa muestra.</description></item>
/// </list>
/// La monitorización continua (stream real) es trabajo del dashboard (M16+) — aquí es un muestreo acotado.
/// </remarks>
public sealed class LiveMonitorCollector : ICollector
{
    /// <summary>Identificador del módulo.</summary>
    public string ModuleId => "M16";

    private readonly List<ICollector> _sources;
    private readonly int _intervalMs;
    private readonly int _sampleCount;

    /// <summary>
    /// Crea el monitor con los módulos fuente a muestrear.
    /// </summary>
    /// <param name="sources">Módulos productores (por ejemplo M02, M03, M12).</param>
    /// <param name="intervalMs">Intervalo entre muestras (ms). Por defecto 500.</param>
    /// <param name="sampleCount">Cantidad de muestras (por defecto 3).</param>
    public LiveMonitorCollector(IEnumerable<ICollector> sources, int intervalMs = 500, int sampleCount = 3)
    {
        ArgumentNullException.ThrowIfNull(sources);
        _sources = sources.ToList();
        if (_sources.Count == 0)
            throw new ArgumentException("El monitor requiere al menos un módulo fuente.", nameof(sources));
        if (sampleCount < 1)
            throw new ArgumentException("sampleCount debe ser >= 1.", nameof(sampleCount));
        if (intervalMs < 0)
            throw new ArgumentException("intervalMs debe ser >= 0.", nameof(intervalMs));
        _intervalMs = intervalMs;
        _sampleCount = sampleCount;
    }

    public async Task<CollectResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var collectedAt = DateTimeOffset.UtcNow;

        // Acumular valores por nombre de métrica.
        var valuesByMetric = new Dictionary<string, List<double>>();

        for (var k = 0; k < _sampleCount; k++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var source in _sources)
            {
                CollectResult? result = null;
                try
                {
                    result = await source.CollectAsync(cancellationToken).ConfigureAwait(false);
                }
                catch { continue; } // mejor esfuerzo: omitir la fuente en esta muestra

                if (result is null)
                    continue;

                foreach (var metric in result.Metrics)
                {
                    if (!valuesByMetric.TryGetValue(metric.Name, out var list))
                    {
                        list = new List<double>();
                        valuesByMetric[metric.Name] = list;
                    }
                    list.Add(metric.Value);
                }
            }

            if (k < _sampleCount - 1 && _intervalMs > 0)
                await Task.Delay(_intervalMs, cancellationToken).ConfigureAwait(false);
        }

        var metrics = new List<Metric>
        {
            new("live.samples", "count", _sampleCount, collectedAt),
        };

        AddMetric(metrics, collectedAt, valuesByMetric, "cpu.usage_percent", "live.cpu.usage_avg", "%", list => list.Average());
        AddMetric(metrics, collectedAt, valuesByMetric, "cpu.usage_percent", "live.cpu.usage_max", "%", list => list.Max());
        AddMetric(metrics, collectedAt, valuesByMetric, "ram.available_percent", "live.ram.available_avg", "%", list => list.Average());
        AddMetric(metrics, collectedAt, valuesByMetric, "ram.available_percent", "live.ram.available_min", "%", list => list.Min());
        AddMetric(metrics, collectedAt, valuesByMetric, "health.events_24h", "live.health.events_max", "count", list => list.Max());
        AddMetric(metrics, collectedAt, valuesByMetric, "process.top_cpu_percent", "live.process.top_cpu_max", "%", list => list.Max());

        // M16 produce solo métricas temporales — sin hallazgos. Los verdicts son de M14.
        return CollectResult.Create(ModuleId, metrics, Array.Empty<DiagnosticFinding>());
    }

    private static void AddMetric(
        List<Metric> metrics,
        DateTimeOffset at,
        Dictionary<string, List<double>> values,
        string metricName,
        string outName,
        string unit,
        Func<List<double>, double> aggregate)
    {
        if (!values.TryGetValue(metricName, out var list) || list.Count == 0)
            return;

        metrics.Add(new Metric(outName, unit, aggregate(list), at));
    }
}
