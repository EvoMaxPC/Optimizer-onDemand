using System.Diagnostics;

namespace Evo.Engine.Host;

public sealed record LabMeasurement(
    int Samples,
    double MedianMs,
    double MinMs,
    double MaxMs,
    double StdDevMs,
    long WorkingSetBytes,
    string MeasuredAtUtc);

public static class LabBenchmark
{
    public static LabMeasurement Measure(int samples = 7)
    {
        int count = Math.Clamp(samples, 3, 25);
        var timings = new List<double>(count);

        for (int i = 0; i < count; i++)
        {
            var sw = Stopwatch.StartNew();
            _ = GC.GetTotalMemory(forceFullCollection: false);
            _ = Environment.TickCount64;
            sw.Stop();
            timings.Add(sw.Elapsed.TotalMilliseconds);
        }

        timings.Sort();
        double median = count % 2 == 1
            ? timings[count / 2]
            : (timings[(count / 2) - 1] + timings[count / 2]) / 2.0;

        double mean = timings.Average();
        double variance = timings.Sum(t => Math.Pow(t - mean, 2)) / count;
        double stdDev = Math.Sqrt(variance);

        return new LabMeasurement(
            count,
            Math.Round(median, 4),
            Math.Round(timings[0], 4),
            Math.Round(timings[^1], 4),
            Math.Round(stdDev, 4),
            Environment.WorkingSet,
            DateTime.UtcNow.ToString("O"));
    }
}
