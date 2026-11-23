namespace Evo.Engine.Host.Clean.Discovery;

public sealed class StorageSourceProvider : IDiscoverySourceProvider
{
    public string ProviderName => "StorageSourceProvider";

    public IReadOnlyList<DiscoveredSource> Discover(DiscoveryOptions options, CancellationToken ct)
    {
        var results = new List<DiscoveredSource>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            ct.ThrowIfCancellationRequested();
            if (!drive.IsReady)
            {
                results.Add(new DiscoveredSource(
                    $"storage.{drive.Name.Replace(":", "").Replace("\\", "").Replace("/", "")}",
                    $"Unidad {drive.Name}",
                    "Storage",
                    drive.DriveType.ToString(),
                    drive.Name,
                    "Volume",
                    "Storage",
                    null,
                    true,
                    false,
                    null,
                    null,
                    null,
                    DiscoverySourceState.Inaccessible.ToString(),
                    "High",
                    "Unidad no lista o inaccesible.",
                    new List<string> { "drive_not_ready" },
                    new List<string>()));
                continue;
            }

            results.Add(new DiscoveredSource(
                $"storage.{drive.Name.Replace(":", "").Replace("\\", "").Replace("/", "")}",
                $"Unidad {drive.Name} ({drive.DriveFormat})",
                "Storage",
                drive.DriveType.ToString(),
                drive.RootDirectory.FullName,
                "Volume",
                drive.DriveFormat,
                null,
                true,
                true,
                null,
                drive.TotalSize - drive.AvailableFreeSpace,
                null,
                DiscoverySourceState.Accessible.ToString(),
                "High",
                $"Volumen {drive.DriveFormat}: {drive.AvailableFreeSpace / (1024 * 1024 * 1024)} GB libres de {drive.TotalSize / (1024 * 1024 * 1024)} GB.",
                new List<string> { "volume_metadata_only" },
                new List<string>()));
        }
        return results;
    }
}
