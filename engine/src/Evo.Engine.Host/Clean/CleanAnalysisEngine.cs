using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Evo.Engine.Host.Clean;

public static class CleanAnalysisEngine
{
    public static CleanPlan Analyze(int maxPerSource = 50)
    {
        var candidates = new List<CleanCandidate>();
        string tempDir = Path.GetTempPath();

        if (Directory.Exists(tempDir))
        {
            foreach (string file in Directory.EnumerateFiles(tempDir, "*", SearchOption.TopDirectoryOnly))
            {
                if (candidates.Count >= maxPerSource) break;
                try
                {
                    var info = new FileInfo(file);
                    if (!info.Exists) continue;
                    bool locked = IsFileLocked(info.FullName);
                    var classification = locked ? CleanClassification.Protected : CleanClassification.Safe;
                    string reason = locked
                        ? "Archivo en uso por otro proceso (protegido)."
                        : "Archivo temporal de usuario sin bloqueo activo.";

                    candidates.Add(new CleanCandidate(
                        "win.userTemp",
                        info.FullName,
                        info.Length,
                        info.CreationTimeUtc.ToString("O"),
                        info.LastWriteTimeUtc.ToString("O"),
                        classification.ToString(),
                        CleanRiskLevel.Low.ToString(),
                        locked,
                        reason));
                }
                catch
                {
                    // Skip inaccessible files
                }
            }
        }

        long totalRecoverable = candidates
            .Where(c => c.Classification == CleanClassification.Safe.ToString())
            .Sum(c => c.SizeBytes);

        string planId = $"plan-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        string created = DateTime.UtcNow.ToString("O");
        string expires = DateTime.UtcNow.AddMinutes(15).ToString("O");

        string payload = JsonSerializer.Serialize(new { planId, created, candidates });
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

        return new CleanPlan(
            planId,
            created,
            expires,
            hash,
            totalRecoverable,
            candidates.Count,
            candidates);
    }

    private static bool IsFileLocked(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch
        {
            return true;
        }
    }
}
