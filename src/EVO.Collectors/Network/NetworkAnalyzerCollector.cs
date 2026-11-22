using System.Management;
using EVO.Core.Contracts;
using EVO.Core.Findings;

namespace EVO.Collectors.Network;

/// <summary>
/// M11 — Network Analyzer: auditoría de red y tráfico.
/// Mide adaptadores de red (total, habilitados) y, cuando está disponible,
/// el total de bytes y paquetes por segundo por todas las interfaces.
/// NO declara causas ni cuellos de botella: es una auditoría de red.
/// </summary>
/// <remarks>
/// Recolección best-effort: si una consulta falla, la sección se omite sin abortar.
/// Detalle por interfaz (nombre, IP) queda fuera de alcance — el dashboard (M15) lo consume.
/// </remarks>
public sealed class NetworkAnalyzerCollector : ICollector
{
    /// <summary>Identificador del módulo.</summary>
    public string ModuleId => "M11";

    public Task<CollectResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var collectedAt = DateTimeOffset.UtcNow;
        var metrics = new List<Metric>();

        int adaptersTotal = 0;
        int adaptersEnabled = 0;
        double bytesSentMbSec = 0.0;
        double bytesReceivedMbSec = 0.0;
        double packetsSentPerSec = 0.0;
        double packetsReceivedPerSec = 0.0;

        try { CollectAdapters(ref adaptersTotal, ref adaptersEnabled); }
        catch { /* mejor esfuerzo */ }

        try { CollectTraffic(ref bytesSentMbSec, ref bytesReceivedMbSec, ref packetsSentPerSec, ref packetsReceivedPerSec); }
        catch { /* mejor esfuerzo */ }

        metrics.Add(new Metric("network.adapters.count", "count", adaptersTotal, collectedAt));
        metrics.Add(new Metric("network.adapters.enabled", "count", adaptersEnabled, collectedAt));
        metrics.Add(new Metric("network.bytes_sent_mb_sec", "MB/s", bytesSentMbSec, collectedAt));
        metrics.Add(new Metric("network.bytes_received_mb_sec", "MB/s", bytesReceivedMbSec, collectedAt));
        metrics.Add(new Metric("network.packets_sent_per_sec", "/sec", packetsSentPerSec, collectedAt));
        metrics.Add(new Metric("network.packets_received_per_sec", "/sec", packetsReceivedPerSec, collectedAt));

        // M11 es auditoría: produce solo métricas, sin hallazgos.
        return Task.FromResult(CollectResult.Create(ModuleId, metrics, Array.Empty<DiagnosticFinding>()));
    }

    private static void CollectAdapters(ref int adaptersTotal, ref int adaptersEnabled)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT State FROM Win32_NetworkAdapter");

        foreach (ManagementObject row in searcher.Get())
        {
            adaptersTotal++;

            var stateRaw = row["State"];
            if (stateRaw is not null)
            {
                var state = stateRaw.ToString()?.ToLowerInvariant();
                if (state is "ok" or "connected")
                    adaptersEnabled++;
            }
        }
    }

    private static void CollectTraffic(ref double bytesSentMbSec, ref double bytesReceivedMbSec, ref double packetsSentPerSec, ref double packetsReceivedPerSec)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT 'Bytes Sent/sec', 'Packets Sent/sec', 'Bytes Received/sec', 'Packets Received/sec' FROM Win32_PerfFormattedData_Tcpip_NetworkInterface");

        foreach (ManagementObject row in searcher.Get())
        {
            var sentRaw = row["Bytes Sent/sec"];
            if (sentRaw is not null && double.TryParse(sentRaw.ToString(), out double sentBytesPerSec))
                bytesSentMbSec += sentBytesPerSec / (1024.0 * 1024);

            var receivedRaw = row["Bytes Received/sec"];
            if (receivedRaw is not null && double.TryParse(receivedRaw.ToString(), out double receivedBytesPerSec))
                bytesReceivedMbSec += receivedBytesPerSec / (1024.0 * 1024);

            var packetsSentRaw = row["Packets Sent/sec"];
            if (packetsSentRaw is not null && double.TryParse(packetsSentRaw.ToString(), out double packetsSent))
                packetsSentPerSec += packetsSent;

            var packetsReceivedRaw = row["Packets Received/sec"];
            if (packetsReceivedRaw is not null && double.TryParse(packetsReceivedRaw.ToString(), out double packetsReceived))
                packetsReceivedPerSec += packetsReceived;
        }
    }
}
