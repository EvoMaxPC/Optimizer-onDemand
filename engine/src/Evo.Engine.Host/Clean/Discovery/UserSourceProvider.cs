namespace Evo.Engine.Host.Clean.Discovery;

public sealed class UserSourceProvider : IDiscoverySourceProvider
{
    public string ProviderName => "UserSourceProvider";

    public IReadOnlyList<DiscoveredSource> Discover(DiscoveryOptions options, CancellationToken ct)
    {
        string user = Environment.UserName;
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string temp = Path.GetTempPath();

        var defs = new (string Id, string Name, string Subcat, string Path, string Risk, string Reason, bool MetadataOnly)[]
        {
            ("user.temp", "Temp del usuario", "Temporary", temp, "Low", "Directorio temporal del usuario actual.", false),
            ("user.localAppData", "AppData Local", "Profile", local, "High", "Ubicación de datos locales de aplicaciones.", true),
            ("user.roamingAppData", "AppData Roaming", "Profile", roaming, "High", "Ubicación de datos roaming de aplicaciones.", true),
            ("user.downloads", "Descargas", "Personal", Path.Combine(profile, "Downloads"), "High", "Carpeta personal protegida; nunca basura automática.", true),
            ("user.desktop", "Escritorio", "Personal", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "High", "Carpeta personal protegida; nunca basura automática.", true),
            ("user.documents", "Documentos", "Personal", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "High", "Carpeta personal protegida; nunca basura automática.", true)
        };

        var results = new List<DiscoveredSource>(defs.Length);
        foreach (var d in defs)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(d.Path)) continue;
            results.Add(SourceClassifier.InspectDirectory(
                d.Id, d.Name, "User", d.Subcat, d.Path, "Directory", "UserProfile", user,
                d.Risk, d.Reason, options, ct, d.MetadataOnly));
        }
        return results;
    }
}
