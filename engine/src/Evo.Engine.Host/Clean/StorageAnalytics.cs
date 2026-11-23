using Evo.Engine.Host;

namespace Evo.Engine.Host.Clean;

public sealed record LargeFileEntry(string Path, long SizeBytes, string LastWriteUtc);

public sealed record StorageAnalyticsReport(
    List<DriveSummary> Volumes,
    List<LargeFileEntry> LargestTempFiles,
    string AnalyzedAtUtc);

public static class StorageAnalytics
{
    public static StorageAnalyticsReport Analyze(int topN = 10)
    {
        var drives = new List<DriveSummary>();
        foreach (var d in DriveInfo.GetDrives())
        {
            if (!d.IsReady) continue;
            drives.Add(new DriveSummary(d.Name, d.DriveFormat, d.TotalSize, d.AvailableFreeSpace));
        }

        var largeFiles = new List<LargeFileEntry>();
        string tempDir = Path.GetTempPath();
        if (Directory.Exists(tempDir))
        {
            largeFiles = Directory.EnumerateFiles(tempDir, "*", SearchOption.TopDirectoryOnly)
                .Select(p =>
                {
                    try { return new FileInfo(p); }
                    catch { return null; }
                })
                .Where(fi => fi is not null && fi.Exists)
                .OrderByDescending(fi => fi!.Length)
                .Take(topN)
                .Select(fi => new LargeFileEntry(fi!.FullName, fi.Length, fi.LastWriteTimeUtc.ToString("O")))
                .ToList();
        }

        return new StorageAnalyticsReport(drives, largeFiles, DateTime.UtcNow.ToString("O"));
    }
}
