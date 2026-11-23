using Evo.Engine.Host.Clean.Discovery;
using Xunit;

namespace Evo.Engine.Host.Tests;

public class DiscoveryEngineTests
{
    private sealed class FailingProvider : IDiscoverySourceProvider
    {
        public string ProviderName => "FailingProvider";
        public IReadOnlyList<DiscoveredSource> Discover(DiscoveryOptions options, CancellationToken ct)
            => throw new InvalidOperationException("Simulated provider failure");
    }

    private sealed class DuplicatePathProvider(string path) : IDiscoverySourceProvider
    {
        public string ProviderName => "DuplicatePathProvider";
        public IReadOnlyList<DiscoveredSource> Discover(DiscoveryOptions options, CancellationToken ct) =>
        [
            SourceClassifier.InspectDirectory("d1", "Dup1", "Test", "Sub", path, "Directory", "Test", null, "Low", "Test", options, ct),
            SourceClassifier.InspectDirectory("d2", "Dup2", "Test", "Sub", path, "Directory", "Test", null, "Low", "Test", options, ct)
        ];
    }

    [Fact]
    public void DiscoveryEngine_DiscoversRealSources_WithoutModifyingFilesystem()
    {
        var engine = new DiscoveryEngine();
        var result = engine.RunSync(new DiscoveryOptions(MaxDepth: 1, MaxFilesPerSource: 20, TimeoutMs: 10_000));

        Assert.True(result.Status is "Completed" or "CompletedWithWarnings");
        Assert.NotEmpty(result.Sources);
        Assert.Contains(result.Sources, s => s.Category == "Storage");
        Assert.Contains(result.Sources, s => s.Category == "User");
    }

    [Fact]
    public void DiscoveryEngine_IsolatesProviderFailure()
    {
        var engine = new DiscoveryEngine([new FailingProvider(), new StorageSourceProvider()]);
        var result = engine.RunSync();

        Assert.Equal("CompletedWithWarnings", result.Status);
        Assert.Equal(1, result.Diagnostics.FailedProviders);
        Assert.Single(result.Diagnostics.ProviderErrors);
        Assert.NotEmpty(result.Sources);
    }

    [Fact]
    public void DiscoveryEngine_DeduplicatesPaths()
    {
        string temp = Path.GetTempPath();
        var engine = new DiscoveryEngine([new DuplicatePathProvider(temp)]);
        var result = engine.RunSync(new DiscoveryOptions(MaxDepth: 1, MaxFilesPerSource: 5));

        Assert.Single(result.Sources);
    }

    [Fact]
    public void DiscoveryEngine_HandlesNonExistentPath_AndCancellation()
    {
        string missing = Path.Combine(Path.GetTempPath(), $"evo-missing-{Guid.NewGuid():N}");
        var src = SourceClassifier.InspectDirectory("m1", "Missing", "Test", "Sub", missing, "Directory", "Test", null, "Low", "Test", new DiscoveryOptions(), CancellationToken.None);
        Assert.False(src.Exists);
        Assert.Equal(DiscoverySourceState.Discovered.ToString(), src.State);

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var engine = new DiscoveryEngine();
        var cancelled = engine.RunSync(new DiscoveryOptions(), cts.Token);
        Assert.Equal("cancelled", cancelled.Status);
        Assert.True(cancelled.Diagnostics.Cancelled);
    }
}
