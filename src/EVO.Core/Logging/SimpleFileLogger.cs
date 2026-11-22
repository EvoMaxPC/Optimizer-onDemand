using System.Text;

namespace EVO.Core.Logging;

/// <summary>
/// Logger de archivo simple de EVO — una línea por evento, UTF-8, UTC.
/// Thread-safe (lock interno). Las entradas se escriben en logs/EVO-yyyy-MM-dd.log.
/// </summary>
public sealed class SimpleFileLogger
{
    private readonly string _logDirectory;
    private readonly string _filePath;
    private readonly object _lock = new();

    /// <summary>
    /// Crea un logger para el directorio indicado (se crea si no existe).
    /// El archivo es por día: logs/EVO-yyyy-MM-dd.log.
    /// </summary>
    public SimpleFileLogger(string logDirectory)
    {
        _logDirectory = logDirectory ?? throw new ArgumentNullException(nameof(logDirectory));
        // Rutas relativas se resuelven contra el base directory de la aplicación,
        // de modo que funcionan en una publicación portable (dist/) sin ninguna raíz.
        if (!Path.IsPathRooted(_logDirectory))
        {
            _logDirectory = Path.Combine(AppContext.BaseDirectory, _logDirectory);
        }
        Directory.CreateDirectory(_logDirectory);
        var today = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
        _filePath = Path.Combine(_logDirectory, $"EVO-{today}.log");
    }

    /// <summary>Ruta del archivo de log activo.</summary>
    public string LogFilePath => _filePath;

    /// <summary>Ruta del directorio de log.</summary>
    public string LogDirectory => _logDirectory;

    /// <summary>Registra un evento.</summary>
    public void Log(LogLevel level, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty.", nameof(message));

        var line = $"{DateTimeOffset.UtcNow:yyyy-MM-ddTHH:mm:ss.fff}Z [{level}] {message}";
        lock (_lock)
        {
            File.AppendAllText(_filePath, line + Environment.NewLine, new UTF8Encoding(false));
        }
    }

    public void Debug(string message) => Log(LogLevel.Debug, message);
    public void Info(string message) => Log(LogLevel.Info, message);
    public void Warning(string message) => Log(LogLevel.Warning, message);
    public void Error(string message) => Log(LogLevel.Error, message);
}
