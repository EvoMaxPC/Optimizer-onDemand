namespace Evo.Engine.Host.Clean.Discovery;

public sealed class ApplicationSourceProvider : IDiscoverySourceProvider
{
    public string ProviderName => "ApplicationSourceProvider";

    public IReadOnlyList<DiscoveredSource> Discover(DiscoveryOptions options, CancellationToken ct)
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string user = Environment.UserName;

        var defs = new (string Id, string Name, string Subcat, string App, string Path, string Risk, string Reason)[]
        {
            ("app.edge.cache", "Microsoft Edge Cache", "Browser", "Microsoft Edge", Path.Combine(local, "Microsoft", "Edge", "User Data", "Default", "Cache"), "Low", "Caché web de Microsoft Edge."),
            ("app.chrome.cache", "Google Chrome Cache", "Browser", "Google Chrome", Path.Combine(local, "Google", "Chrome", "User Data", "Default", "Cache"), "Low", "Caché web de Google Chrome."),
            ("app.firefox.cache", "Mozilla Firefox Cache", "Browser", "Mozilla Firefox", Path.Combine(local, "Mozilla", "Firefox", "Profiles"), "Low", "Perfiles y cachés de Firefox."),
            ("app.dev.npm", "npm Cache", "Development", "npm", Path.Combine(local, "npm-cache"), "Medium", "Caché local de paquetes npm."),
            ("app.dev.pnpm", "pnpm Store", "Development", "pnpm", Path.Combine(local, "pnpm", "store"), "Medium", "Almacén content-addressable de pnpm."),
            ("app.dev.nuget", "NuGet Packages Cache", "Development", "NuGet", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages"), "Medium", "Caché local de paquetes NuGet."),
            ("app.game.steam", "Steam Logs/Cache", "Gaming", "Steam", Path.Combine(local, "Steam", "htmlcache"), "Low", "Caché web del cliente Steam."),
            ("app.msg.discord", "Discord Cache", "Messaging", "Discord", Path.Combine(roaming, "discord", "Cache"), "Low", "Caché multimedia de Discord.")
        };

        var results = new List<DiscoveredSource>(defs.Length);
        foreach (var d in defs)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(d.Path)) continue;
            results.Add(SourceClassifier.InspectDirectory(
                d.Id, d.Name, "Application", d.Subcat, d.Path, "Directory", d.App, user,
                d.Risk, d.Reason, options, ct, metadataOnly: false));
        }
        return results;
    }
}
