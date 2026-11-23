namespace Evo.Engine.Host.Clean.Discovery;

public sealed class DiscoveryEngine
{
    private readonly IReadOnlyList<IDiscoverySourceProvider> _providers;
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private DiscoveryResult? _lastResult;
    private string _status = "idle";

    public DiscoveryEngine(IReadOnlyList<IDiscoverySourceProvider>? providers = null)
    {
        _providers = providers ??
        [
            new WindowsSourceProvider(),
            new UserSourceProvider(),
            new ApplicationSourceProvider(),
            new StorageSourceProvider()
        ];
    }

    public static DiscoveryEngine Shared { get; } = new();

    public DiscoveryResult RunSync(DiscoveryOptions? options = null, CancellationToken externalCt = default)
    {
        var opts = options ?? new DiscoveryOptions();
        string runId = $"disc-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        string startedAt = DateTime.UtcNow.ToString("O");

        using var timeoutCts = new CancellationTokenSource(opts.TimeoutMs);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(externalCt, timeoutCts.Token);

        lock (_gate)
        {
            _cts = linked;
            _status = "running";
        }

        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sources = new List<DiscoveredSource>();
        var providerErrors = new List<string>();
        int failedProviders = 0;
        bool cancelled = false;

        foreach (var provider in _providers)
        {
            if (linked.Token.IsCancellationRequested)
            {
                cancelled = true;
                break;
            }

            try
            {
                var found = provider.Discover(opts, linked.Token);
                foreach (var src in found)
                {
                    if (seenPaths.Add(src.Path))
                    {
                        sources.Add(src);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
                break;
            }
            catch (Exception ex)
            {
                failedProviders++;
                providerErrors.Add($"{provider.ProviderName}: {ex.Message}");
            }
        }

        string finalStatus = cancelled ? "cancelled" : "completed";
        var diagnostics = new DiscoveryDiagnostics(
            startedAt,
            DateTime.UtcNow.ToString("O"),
            cancelled,
            _providers.Count,
            failedProviders,
            sources.Count,
            sources.Count(s => s.Accessible),
            sources.Count(s => !s.Accessible),
            providerErrors);

        var result = new DiscoveryResult(runId, finalStatus, sources, diagnostics);
        lock (_gate)
        {
            _lastResult = result;
            _status = finalStatus;
            _cts = null;
        }
        return result;
    }

    public object GetStatus()
    {
        lock (_gate)
        {
            return new
            {
                status = _status,
                hasResult = _lastResult is not null,
                runId = _lastResult?.RunId,
                diagnostics = _lastResult?.Diagnostics
            };
        }
    }

    public bool Cancel()
    {
        lock (_gate)
        {
            if (_cts is not null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                return true;
            }
            return false;
        }
    }

    public DiscoveryResult? GetResults()
    {
        lock (_gate)
        {
            return _lastResult;
        }
    }
}
