using System.Runtime.InteropServices;

namespace Evo.Engine.Host;

public sealed record DriveSummary(string Name, string Format, long TotalBytes, long FreeBytes);

public sealed record SystemDiagnostics(
    string OsDescription,
    string OsVersion,
    string Architecture,
    int ProcessorCount,
    long TotalAvailableMemoryBytes,
    List<DriveSummary> Drives,
    List<string> AppliedActionIds,
    string CollectedAtUtc);

public static class DiagnosticsCollector
{
    public static SystemDiagnostics Collect()
    {
        var drives = new List<DriveSummary>();
        foreach (var d in DriveInfo.GetDrives())
        {
            if (!d.IsReady) continue;
            drives.Add(new DriveSummary(d.Name, d.DriveFormat, d.TotalSize, d.AvailableFreeSpace));
        }

        long totalMem = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        var appliedIds = ActionExecutor.ListSnapshots()
            .Select(s => s.TryGetProperty("actionId", out var id) ? id.GetString() : null)
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new SystemDiagnostics(
            RuntimeInformation.OSDescription,
            Environment.OSVersion.VersionString,
            RuntimeInformation.OSArchitecture.ToString(),
            Environment.ProcessorCount,
            totalMem,
            drives,
            appliedIds,
            DateTime.UtcNow.ToString("O"));
    }
}
