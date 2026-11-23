using System.Security.Cryptography;

namespace Evo.Engine.Host.Clean;

public sealed record DuplicateGroup(string Sha256, long SizeBytes, List<string> Paths);

public static class DuplicateIntelligence
{
    public static List<DuplicateGroup> FindDuplicatesInTemp(int maxFiles = 100)
    {
        string tempDir = Path.GetTempPath();
        if (!Directory.Exists(tempDir)) return new List<DuplicateGroup>();

        var bySize = new Dictionary<long, List<string>>();
        int scanned = 0;

        foreach (string file in Directory.EnumerateFiles(tempDir, "*", SearchOption.TopDirectoryOnly))
        {
            if (scanned >= maxFiles) break;
            try
            {
                var info = new FileInfo(file);
                if (!info.Exists || info.Length == 0) continue;
                scanned++;
                if (!bySize.TryGetValue(info.Length, out var list))
                {
                    list = new List<string>();
                    bySize[info.Length] = list;
                }
                list.Add(info.FullName);
            }
            catch
            {
                // Skip inaccessible files
            }
        }

        var groups = new List<DuplicateGroup>();
        foreach (var (size, paths) in bySize)
        {
            if (paths.Count < 2) continue;
            var byHash = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (string p in paths)
            {
                try
                {
                    using var stream = File.OpenRead(p);
                    string hash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
                    if (!byHash.TryGetValue(hash, out var hList))
                    {
                        hList = new List<string>();
                        byHash[hash] = hList;
                    }
                    hList.Add(p);
                }
                catch
                {
                    // Skip locked files
                }
            }

            foreach (var (hash, hPaths) in byHash)
            {
                if (hPaths.Count >= 2)
                {
                    groups.Add(new DuplicateGroup(hash, size, hPaths));
                }
            }
        }

        return groups;
    }
}
