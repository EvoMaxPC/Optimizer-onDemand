using Xunit;
using EVO.Core.Logging;

namespace EVO.Core.Tests.Logging;

public class FileLoggerTests : IDisposable
{
    private readonly string _tempDir;

    public FileLoggerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "EVO-FileLoggerTests-" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Logger_CreatesDirectoryAndLogFile()
    {
        var logger = new SimpleFileLogger(_tempDir);

        Assert.True(Directory.Exists(_tempDir));
        Assert.EndsWith(".log", logger.LogFilePath);
    }

    [Fact]
    public void Logger_AppendsTimestampedLinesToFile()
    {
        var logger = new SimpleFileLogger(_tempDir);

        logger.Info("foundation start");
        logger.Warning("check pending");

        var lines = File.ReadAllLines(logger.LogFilePath);
        Assert.Equal(2, lines.Length);
        Assert.Contains("Z [Info] foundation start", lines[0]);
        Assert.Contains("Z [Warning] check pending", lines[1]);
        Assert.Matches("^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z", lines[0]);
    }

    [Fact]
    public void Logger_RejectsEmptyMessage()
    {
        var logger = new SimpleFileLogger(_tempDir);

        Assert.Throws<ArgumentException>(() => logger.Info("  "));
    }

    [Fact]
    public void Logger_TodayFileNamedByDate()
    {
        var logger = new SimpleFileLogger(_tempDir);
        var expectedPrefix = "EVO-" + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd") + ".log";

        Assert.EndsWith(expectedPrefix, logger.LogFilePath);
    }
}
