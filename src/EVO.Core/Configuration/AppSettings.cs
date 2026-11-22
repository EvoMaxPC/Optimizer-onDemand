using System.Text.Json;
using System.Text.Json.Serialization;

namespace EVO.Core.Configuration;

/// <summary>Error de configuración.</summary>
public sealed class AppSettingsException : Exception
{
    public AppSettingsException(string message) : base(message) { }
}

/// <summary>
/// Configuración de EVO BOTTLENECK DETECTOR, cargada desde config/appsettings.json.
/// </summary>
public sealed class AppSettings
{
    [JsonPropertyName("AppName")]
    public string AppName { get; set; } = "";

    [JsonPropertyName("Version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("ReadOnly")]
    public bool ReadOnly { get; set; } = true;

    [JsonPropertyName("Target")]
    public string Target { get; set; } = "Windows x64";

    [JsonPropertyName("Framework")]
    public string Framework { get; set; } = ".NET 10 LTS";

    [JsonPropertyName("LogDirectory")]
    public string LogDirectory { get; set; } = "logs";

    [JsonPropertyName("ReportsDirectory")]
    public string ReportsDirectory { get; set; } = "reports";

    [JsonPropertyName("DataDirectory")]
    public string DataDirectory { get; set; } = "data";

    [JsonPropertyName("Telemetry")]
    public bool Telemetry { get; set; } = false;

    [JsonPropertyName("ExternalConnections")]
    public bool ExternalConnections { get; set; } = false;

    [JsonPropertyName("Checkpoints")]
    public Dictionary<string, string>? Checkpoints { get; set; }

    /// <summary>Carga y valida un archivo de configuración.</summary>
    public static AppSettings Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new AppSettingsException("Path is required.");

        if (!File.Exists(path))
            throw new AppSettingsException($"Config file not found: {path}");

        AppSettings? settings;
        try
        {
            string json = File.ReadAllText(path);
            settings = JsonSerializer.Deserialize<AppSettings>(json)
                       ?? throw new AppSettingsException($"Config file is empty: {path}");
        }
        catch (JsonException ex)
        {
            throw new AppSettingsException($"Invalid JSON in config file {path}: {ex.Message}");
        }

        Validate(settings);
        return settings;
    }

    /// <summary>
    /// Carga la configuración por defecto desde &lt;BaseDirectory&gt;/config/appsettings.json.
    /// Resolve relative to AppContext.BaseDirectory so portable self-contained
    /// publish works from a single folder with no installed .NET runtime.
    /// </summary>
    public static AppSettings LoadDefault()
        => Load(Path.Combine(AppContext.BaseDirectory, "config", "appsettings.json"));

    private static void Validate(AppSettings s)
    {
        if (string.IsNullOrWhiteSpace(s.AppName))
            throw new AppSettingsException("Config missing required field: AppName");
        if (string.IsNullOrWhiteSpace(s.Version))
            throw new AppSettingsException("Config missing required field: Version");
        if (string.IsNullOrWhiteSpace(s.LogDirectory))
            throw new AppSettingsException("Config missing required field: LogDirectory");
        if (string.IsNullOrWhiteSpace(s.ReportsDirectory))
            throw new AppSettingsException("Config missing required field: ReportsDirectory");
        if (string.IsNullOrWhiteSpace(s.DataDirectory))
            throw new AppSettingsException("Config missing required field: DataDirectory");
        if (s.ReadOnly != true)
            throw new AppSettingsException("EVO must be read-only by default (ReadOnly must be true).");
    }
}
