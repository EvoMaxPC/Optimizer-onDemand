using System.Management;
using EVO.Core.Contracts;
using EVO.Core.Findings;

namespace EVO.Collectors.Hardware;

/// <summary>
/// M01 — System Intelligence: inventario completo del sistema Windows.
/// Produce solo métricas de inventario (SO, CPU, RAM, almacenamiento, GPU).
/// NO correlaciona ni detecta anomalías todavía (eso pertenece a M14).
/// </summary>
/// <remarks>
/// Recolección best-effort por sección: una sección que falle (driver raro,
/// equipo sin GPU, inventario incompleto) se omite sin abortar todo.
/// </remarks>
public sealed class SystemIntelligenceCollector : ICollector
{
    public string ModuleId => "M01";

    public Task<CollectResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var collectedAt = DateTimeOffset.UtcNow;
        var metrics = new List<Metric>();

        try { CollectOs(metrics, collectedAt); } catch { /* mejor esfuerzo: omitir la sección */ }
        try { CollectCpu(metrics, collectedAt); } catch { /* mejor esfuerzo */ }
        try { CollectStorage(metrics, collectedAt); } catch { /* mejor esfuerzo */ }
        try { CollectGpu(metrics, collectedAt); } catch { /* mejor esfuerzo */ }

        return Task.FromResult(CollectResult.Create(ModuleId, metrics, Array.Empty<DiagnosticFinding>()));
    }

    private static IEnumerable<ManagementObject> Query(string query)
    {
        using var searcher = new ManagementObjectSearcher(query);
        foreach (ManagementObject obj in searcher.Get())
        {
            yield return obj;
        }
    }

    private static void CollectOs(List<Metric> metrics, DateTimeOffset at)
    {
        foreach (ManagementObject os in Query("SELECT Caption, Version, BuildNumber, OSArchitecture, CSName, TotalVisibleMemorySize FROM Win32_OperatingSystem"))
        {
            var caption = os["Caption"]?.ToString();
            if (!string.IsNullOrWhiteSpace(caption))
                metrics.Add(new Metric("os.caption", "", 0, at, caption));

            var version = os["Version"]?.ToString();
            if (!string.IsNullOrWhiteSpace(version))
                metrics.Add(new Metric("os.version", "", 0, at, version));

            var build = os["BuildNumber"]?.ToString();
            if (!string.IsNullOrWhiteSpace(build))
                metrics.Add(new Metric("os.build", "", 0, at, build));

            var arch = os["OSArchitecture"]?.ToString();
            if (!string.IsNullOrWhiteSpace(arch))
                metrics.Add(new Metric("os.architecture", "", 0, at, arch));

            var computerName = os["CSName"]?.ToString();
            if (!string.IsNullOrWhiteSpace(computerName))
                metrics.Add(new Metric("os.computer_name", "", 0, at, computerName));

            // TotalVisibleMemorySize: KB (int/uint) → MB
            var memKb = os["TotalVisibleMemorySize"];
            if (memKb is not null && long.TryParse(memKb.ToString(), out long kb))
            {
                metrics.Add(new Metric("os.total_memory_mb", "MB", kb / 1024, at));
            }
        }
    }

    private static void CollectCpu(List<Metric> metrics, DateTimeOffset at)
    {
        int physicalCores = 0;
        int logicalProcessors = 0;
        long maxClock = 0;
        string? name = null;

        foreach (ManagementObject cpu in Query("SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor"))
        {
            name ??= cpu["Name"]?.ToString();

            if (cpu["NumberOfCores"] is { } c && int.TryParse(c.ToString(), out int parsedCores))
                physicalCores += parsedCores;

            if (cpu["NumberOfLogicalProcessors"] is { } lp && int.TryParse(lp.ToString(), out int parsedLp))
                logicalProcessors += parsedLp;

            if (cpu["MaxClockSpeed"] is { } clock && long.TryParse(clock.ToString(), out long ms) && ms > maxClock)
                maxClock = ms;
        }

        if (!string.IsNullOrWhiteSpace(name))
            metrics.Add(new Metric("cpu.name", "", 0, at, name));
        if (physicalCores > 0)
            metrics.Add(new Metric("cpu.cores", "cores", physicalCores, at));
        if (logicalProcessors > 0)
            metrics.Add(new Metric("cpu.logical_processors", "logical_processors", logicalProcessors, at));
        if (maxClock > 0)
            metrics.Add(new Metric("cpu.max_clock_mhz", "MHz", maxClock, at));
    }

    private static void CollectStorage(List<Metric> metrics, DateTimeOffset at)
    {
        foreach (ManagementObject disk in Query("SELECT DeviceID, Size, FreeSpace FROM Win32_LogicalDisk WHERE DriveType = 3"))
        {
            var deviceId = disk["DeviceID"]?.ToString();
            if (string.IsNullOrWhiteSpace(deviceId))
                continue;

            var id = deviceId.TrimEnd(':').Trim();

            var size = disk["Size"];
            if (size is not null && long.TryParse(size.ToString(), out long sizeBytes))
                metrics.Add(new Metric($"disk.{id}.total_gb", "GB", sizeBytes / (1024.0 * 1024 * 1024), at));

            var free = disk["FreeSpace"];
            if (free is not null && long.TryParse(free.ToString(), out long freeBytes))
                metrics.Add(new Metric($"disk.{id}.free_gb", "GB", freeBytes / (1024.0 * 1024 * 1024), at));
        }
    }

    private static void CollectGpu(List<Metric> metrics, DateTimeOffset at)
    {
        int index = 0;
        foreach (ManagementObject gpu in Query("SELECT Name, AdapterRAM FROM Win32_VideoController"))
        {
            var name = gpu["Name"]?.ToString();
            if (!string.IsNullOrWhiteSpace(name))
                metrics.Add(new Metric($"gpu.{index}.name", "", 0, at, name));

            var ram = gpu["AdapterRAM"];
            if (ram is not null && long.TryParse(ram.ToString(), out long ramBytes))
                metrics.Add(new Metric($"gpu.{index}.adapter_ram_mb", "MB", ramBytes / (1024L * 1024), at));

            index++;
        }
    }
}
