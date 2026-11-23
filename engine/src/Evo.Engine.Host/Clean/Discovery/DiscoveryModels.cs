namespace Evo.Engine.Host.Clean.Discovery;

public enum DiscoverySourceState
{
    Discovered,
    Accessible,
    Inspected,
    PartiallyInspected,
    Inaccessible,
    PendingAnalysis
}

public sealed record DiscoveryOptions(
    int MaxDepth = 2,
    int MaxFilesPerSource = 500,
    int TimeoutMs = 15_000);

public sealed record DiscoveredSource(
    string Id,
    string Name,
    string Category,
    string Subcategory,
    string Path,
    string SourceType,
    string RelatedComponent,
    string? OwnerUser,
    bool Exists,
    bool Accessible,
    int? FileCount,
    long? TotalBytes,
    string? LastModifiedUtc,
    string State,
    string PreliminaryRisk,
    string InclusionReason,
    List<string> Restrictions,
    List<string> Errors);

public sealed record DiscoveryDiagnostics(
    string StartedAtUtc,
    string? CompletedAtUtc,
    bool Cancelled,
    int TotalProviders,
    int FailedProviders,
    int TotalSources,
    int AccessibleSources,
    int InaccessibleSources,
    List<string> ProviderErrors);

public sealed record DiscoveryResult(
    string RunId,
    string Status,
    List<DiscoveredSource> Sources,
    DiscoveryDiagnostics Diagnostics);

public interface IDiscoverySourceProvider
{
    string ProviderName { get; }
    IReadOnlyList<DiscoveredSource> Discover(DiscoveryOptions options, CancellationToken ct);
}
