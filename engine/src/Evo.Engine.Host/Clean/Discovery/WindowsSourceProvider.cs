namespace Evo.Engine.Host.Clean.Discovery;

public sealed class WindowsSourceProvider : IDiscoverySourceProvider
{
    public string ProviderName => "WindowsSourceProvider";

    public IReadOnlyList<DiscoveredSource> Discover(DiscoveryOptions options, CancellationToken ct)
    {
        string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        var defs = new (string Id, string Name, string Subcat, string Path, string Risk, string Reason, bool MetadataOnly)[]
        {
            ("win.temp", "Windows Temp", "Temporary", Path.Combine(winDir, "Temp"), "Low", "Archivos temporales del sistema Windows.", false),
            ("win.prefetch", "Windows Prefetch", "Cache", Path.Combine(winDir, "Prefetch"), "High", "Solo identificación; no limpiar rutinariamente.", true),
            ("win.updateCache", "Windows Update Download", "Update", Path.Combine(winDir, "SoftwareDistribution", "Download"), "Medium", "Caché de descargas de Windows Update.", false),
            ("win.wer.system", "Windows Error Reporting (Sistema)", "Diagnostics", Path.Combine(commonAppData, "Microsoft", "Windows", "WER"), "Low", "Informes de errores del sistema.", false),
            ("win.wer.user", "Windows Error Reporting (Usuario)", "Diagnostics", Path.Combine(localAppData, "Microsoft", "Windows", "WER"), "Low", "Informes de errores del usuario.", false),
            ("win.crashDumps", "Crash Dumps", "Diagnostics", Path.Combine(localAppData, "CrashDumps"), "Medium", "Volcados de fallos de aplicaciones.", false),
            ("win.winsxs", "Component Store (WinSxS)", "Maintenance", Path.Combine(winDir, "WinSxS"), "High", "Solo identificación; nunca modificar directamente.", true)
        };

        var results = new List<DiscoveredSource>(defs.Length);
        foreach (var d in defs)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(d.Path)) continue;
            results.Add(SourceClassifier.InspectDirectory(
                d.Id, d.Name, "Windows", d.Subcat, d.Path, "Directory", "Windows", null,
                d.Risk, d.Reason, options, ct, d.MetadataOnly));
        }
        return results;
    }
}
