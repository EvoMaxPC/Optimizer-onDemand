using Xunit;
using EVO.Core.Configuration;

namespace EVO.Core.Tests.Configuration;

public class AppSettingsTests
{
    private static string WriteTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), "EVO-AppSettingsTests-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, content);
        return path;
    }

    private const string ValidJson = """
    {
      "AppName": "EVO BOTTLENECK DETECTOR",
      "Version": "0.1.0",
      "ReadOnly": true,
      "Target": "Windows x64",
      "LogDirectory": "logs",
      "ReportsDirectory": "reports",
      "DataDirectory": "data",
      "Checkpoints": { "F01": "verified" }
    }
    """;

    [Fact]
    public void Load_ReadsValidConfig()
    {
        var path = WriteTempFile(ValidJson);
        try
        {
            var settings = AppSettings.Load(path);

            Assert.Equal("EVO BOTTLENECK DETECTOR", settings.AppName);
            Assert.Equal("0.1.0", settings.Version);
            Assert.True(settings.ReadOnly);
            Assert.Equal("logs", settings.LogDirectory);
            Assert.Equal("reports", settings.ReportsDirectory);
            Assert.Equal("data", settings.DataDirectory);
            Assert.NotNull(settings.Checkpoints);
            Assert.Equal("verified", settings.Checkpoints!["F01"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_MissingFile_Throws()
    {
        var path = Path.Combine(Path.GetTempPath(), "nonexistent-" + Guid.NewGuid().ToString("N") + ".json");

        var ex = Assert.Throws<AppSettingsException>(() => AppSettings.Load(path));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void Load_InvalidJson_Throws()
    {
        var path = WriteTempFile("{ this is not valid json ");
        try
        {
            Assert.Throws<AppSettingsException>(() => AppSettings.Load(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_MissingAppName_Throws()
    {
        var path = WriteTempFile("""
        { "Version": "0.1.0", "LogDirectory": "logs", "ReportsDirectory": "reports", "DataDirectory": "data" }
        """);
        try
        {
            var ex = Assert.Throws<AppSettingsException>(() => AppSettings.Load(path));
            Assert.Contains("AppName", ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Validate_RejectsNonReadOnlyConfig()
    {
        var path = WriteTempFile("""
        { "AppName": "X", "Version": "1.0.0", "ReadOnly": false, "LogDirectory": "logs", "ReportsDirectory": "reports", "DataDirectory": "data" }
        """);
        try
        {
            var ex = Assert.Throws<AppSettingsException>(() => AppSettings.Load(path));
            Assert.Contains("read-only", ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
