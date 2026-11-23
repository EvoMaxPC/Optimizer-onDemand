namespace Evo.Engine.Host.Clean;

public sealed record CleanVerificationResult(
    int VerifiedMoved,
    int MissingFiles,
    long ActualRecoveredBytes,
    string VerifiedAtUtc);

public static class CleanVerificationEngine
{
    public static CleanVerificationResult VerifyQuarantine(QuarantineManifest manifest)
    {
        int verified = 0;
        int missing = 0;
        long recovered = 0;

        foreach (var entry in manifest.Entries)
        {
            if (File.Exists(entry.QuarantinedPath) && !File.Exists(entry.OriginalPath))
            {
                verified++;
                recovered += entry.SizeBytes;
            }
            else
            {
                missing++;
            }
        }

        return new CleanVerificationResult(verified, missing, recovered, DateTime.UtcNow.ToString("O"));
    }
}
