using Evo.Engine.Host.Clean;
using Evo.Engine.Host.Clean.Discovery;

namespace Evo.Engine.Host.Contracts;

public interface IDiscoveryProvider
{
    string ProviderName { get; }
    IReadOnlyList<DiscoveredSource> Discover(DiscoveryOptions options, CancellationToken ct);
}

public interface IActionExecutor
{
    object Apply(string actionId);
}

public interface ICleanupExecutor
{
    QuarantineManifest Quarantine(IEnumerable<string> paths);
}

public interface IOptimizationExecutor
{
    object ExecutePlan(string profile);
}

public interface IRollbackExecutor
{
    object RollbackAction(string actionId);
    object RestoreQuarantine(string batchId);
}

public interface IPermissionManager
{
    bool HasRequiredPrivileges(string operationId);
}

public interface IOperationJournal
{
    void Record(string operation, string status, object? details = null);
}
