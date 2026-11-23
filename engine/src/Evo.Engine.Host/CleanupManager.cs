using System.Text;
using System.Text.Json;

namespace Evo.Engine.Host;

public sealed record CleanupCandidate(string Path, long SizeBytes, string LastWriteUtc);

public sealed record QuarantineEntry(string OriginalPath, string QuarantinedPath, long SizeBytes);

public sealed record QuarantineManifest(string BatchId, string CreatedAtUtc, List<QuarantineEntry> Entries);

public static class CleanupManager
{
    private static string GetQuarantineRoot()
    {
        string root = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EvoOptimizer",
            "quarantine");
        Directory.CreateDirectory(root);
        return root;
    }

    public static List<CleanupCandidate> ScanTemp(int maxItems = 50)
    {
        string tempDir = System.IO.Path.GetTempPath();
        if (!Directory.Exists(tempDir))
        {
            return new List<CleanupCandidate>();
        }

        var results = new List<CleanupCandidate>();
        foreach (string file in Directory.EnumerateFiles(tempDir, "*", SearchOption.TopDirectoryOnly))
        {
            if (results.Count >= maxItems) break;
            try
            {
                var info = new FileInfo(file);
                if (!info.Exists) continue;
                results.Add(new CleanupCandidate(info.FullName, info.Length, info.LastWriteTimeUtc.ToString("O")));
            }
            catch
            {
                // Skip locked or inaccessible temp files
            }
        }
        return results;
    }

    public static QuarantineManifest Quarantine(IEnumerable<string> paths)
    {
        string batchId = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        string batchDir = System.IO.Path.Combine(GetQuarantineRoot(), batchId);
        Directory.CreateDirectory(batchDir);

        string tempRoot = System.IO.Path.GetFullPath(System.IO.Path.GetTempPath());
        var entries = new List<QuarantineEntry>();

        foreach (string rawPath in paths)
        {
            try
            {
                string fullPath = System.IO.Path.GetFullPath(rawPath);
                if (!fullPath.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase)) continue;
                if (!File.Exists(fullPath)) continue;

                var info = new FileInfo(fullPath);
                string destPath = System.IO.Path.Combine(batchDir, $"{Guid.NewGuid():N}_{info.Name}");
                File.Move(fullPath, destPath);
                entries.Add(new QuarantineEntry(fullPath, destPath, info.Length));
            }
            catch
            {
                // Skip files locked by other processes
            }
        }

        var manifest = new QuarantineManifest(batchId, DateTime.UtcNow.ToString("O"), entries);
        string manifestPath = System.IO.Path.Combine(batchDir, "manifest.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest), Encoding.UTF8);
        return manifest;
    }

    public static object Restore(string batchId)
    {
        string batchDir = System.IO.Path.Combine(GetQuarantineRoot(), batchId);
        string manifestPath = System.IO.Path.Combine(batchDir, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException($"Quarantine batch not found: {batchId}");
        }

        var manifest = JsonSerializer.Deserialize<QuarantineManifest>(File.ReadAllText(manifestPath, Encoding.UTF8))
            ?? throw new InvalidDataException("Invalid quarantine manifest");

        int restored = 0;
        foreach (var entry in manifest.Entries)
        {
            try
            {
                if (!File.Exists(entry.QuarantinedPath)) continue;
                string? parent = System.IO.Path.GetDirectoryName(entry.OriginalPath);
                if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                if (!File.Exists(entry.OriginalPath))
                {
                    File.Move(entry.QuarantinedPath, entry.OriginalPath);
                    restored++;
                }
            }
            catch
            {
                // Skip files that cannot be restored
            }
        }

        return new { batchId, restored, total = manifest.Entries.Count };
    }
}
