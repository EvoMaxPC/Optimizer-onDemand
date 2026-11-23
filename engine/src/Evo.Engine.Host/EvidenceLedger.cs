using System.Text;
using System.Text.Json;

namespace Evo.Engine.Host;

public sealed record LedgerEntry(
    string Id,
    string TimestampUtc,
    string Operation,
    string Status,
    object? Details);

public static class EvidenceLedger
{
    private static string GetLedgerPath()
    {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EvoOptimizer");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "ledger.ndjson");
    }

    public static void Append(string operation, string status, object? details = null)
    {
        try
        {
            var entry = new LedgerEntry(
                Guid.NewGuid().ToString("N"),
                DateTime.UtcNow.ToString("O"),
                operation,
                status,
                details);
            string line = JsonSerializer.Serialize(entry) + Environment.NewLine;
            File.AppendAllText(GetLedgerPath(), line, Encoding.UTF8);
        }
        catch
        {
            // Do not fail primary operation if ledger write fails
        }
    }

    public static List<JsonElement> List(int limit = 100)
    {
        string path = GetLedgerPath();
        if (!File.Exists(path))
        {
            return new List<JsonElement>();
        }

        var entries = new List<JsonElement>();
        foreach (string line in File.ReadLines(path, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                using var doc = JsonDocument.Parse(line);
                entries.Add(doc.RootElement.Clone());
            }
            catch
            {
                // Skip corrupted NDJSON line
            }
        }

        entries.Reverse();
        return entries.Take(Math.Clamp(limit, 1, 500)).ToList();
    }
}
