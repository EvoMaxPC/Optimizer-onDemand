namespace Evo.Engine.Host.Clean.Discovery;

public enum DiscoveryLifecycleState
{
    Idle,
    Queued,
    Running,
    Cancelling,
    Completed,
    CompletedWithWarnings,
    Failed,
    Cancelled
}

public sealed class DiscoveryEngine
{
    private readonly IReadOnlyList<IDiscoverySourceProvider> _providers;
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private DiscoveryResult? _lastResult;
    private DiscoveryLifecycleState _state = DiscoveryLifecycleState.Idle;
    private string? _activeRunId;
    private string? _currentProvider;
    private string? _cancelReason;
    private int _processedProviders;
    private readonly List<DiscoveredSource> _partialSources = new();
    private readonly List<string> _partialErrors = new();

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

    public object StartBackground(DiscoveryOptions? options = null)
    {
        lock (_gate)
        {
            if (_state is DiscoveryLifecycleState.Running or DiscoveryLifecycleState.Queued or DiscoveryLifecycleState.Cancelling)
            {
                return new { runId = _activeRunId, status = _state.ToString(), alreadyRunning = true };
            }

            string runId = $"disc-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
            _activeRunId = runId;
            _state = DiscoveryLifecycleState.Queued;
            _currentProvider = null;
            _cancelReason = null;
            _processedProviders = 0;
            _partialSources.Clear();
            _partialErrors.Clear();

            _ = Task.Run(() => RunSync(options, CancellationToken.None, runId));
            return new { runId, status = _state.ToString(), alreadyRunning = false };
        }
    }

    public DiscoveryResult RunSync(DiscoveryOptions? options = null, CancellationToken externalCt = default, string? existingRunId = null)
    {
        var opts = options ?? new DiscoveryOptions();
        string runId = existingRunId ?? $"disc-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        string startedAt = DateTime.UtcNow.ToString("O");

        using var timeoutCts = new CancellationTokenSource(opts.TimeoutMs);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(externalCt, timeoutCts.Token);

        lock (_gate)
        {
            _cts = linked;
            _activeRunId = runId;
            _state = DiscoveryLifecycleState.Running;
            _processedProviders = 0;
            _partialSources.Clear();
            _partialErrors.Clear();
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

            lock (_gate)
            {
                _currentProvider = provider.ProviderName;
            }

            try
            {
                var found = provider.Discover(opts, linked.Token);
                foreach (var src in found)
                {
                    if (seenPaths.Add(src.Path))
                    {
                        sources.Add(src);
                        lock (_gate)
                        {
                            _partialSources.Add(src);
                        }
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
                string err = $"{provider.ProviderName}: {ex.Message}";
                providerErrors.Add(err);
                lock (_gate)
                {
                    _partialErrors.Add(err);
                }
            }
            finally
            {
                lock (_gate)
                {
                    _processedProviders++;
                }
            }
        }

        DiscoveryLifecycleState finalState = cancelled
            ? DiscoveryLifecycleState.Cancelled
            : (failedProviders > 0 || sources.Any(s => !s.Accessible))
                ? DiscoveryLifecycleState.CompletedWithWarnings
                : DiscoveryLifecycleState.Completed;

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

        var result = new DiscoveryResult(runId, finalState.ToString(), sources, diagnostics);
        lock (_gate)
        {
            _lastResult = result;
            _state = finalState;
            _currentProvider = null;
            _cts = null;
        }
        return result;
    }

    public object GetStatus()
    {
        lock (_gate)
        {
            int progress = _providers.Count == 0
                ? 0
                : (int)Math.Round((_processedProviders * 100.0) / _providers.Count);

            return new
            {
                runId = _activeRunId ?? _lastResult?.RunId,
                status = _state.ToString(),
                progress,
                currentProvider = _currentProvider,
                processedSources = _partialSources.Count,
                totalProviders = _providers.Count,
                errors = _partialErrors.ToList(),
                cancelReason = _cancelReason,
                hasResult = _lastResult is not null,
                diagnostics = _lastResult?.Diagnostics
            };
        }
    }

    public bool Cancel(string reason = "user_requested")
    {
        lock (_gate)
        {
            if (_cts is not null && !_cts.IsCancellationRequested)
            {
                _state = DiscoveryLifecycleState.Cancelling;
                _cancelReason = reason;
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
