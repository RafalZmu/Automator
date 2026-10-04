namespace Automator.Application.Automation;

/// <summary>Runs cancellable service work independently from the serialized launcher command queue.</summary>
public sealed class AutomationRequestExecutor
{
    private readonly object _gate = new();
    private readonly Dictionary<(string ModuleId, string RequestId), CancellationTokenSource> _requests = [];

    public async Task<T> ExecuteAsync<T>(string moduleId, string requestId, CancellationToken contextToken,
        Func<CancellationToken, Task<T>> operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
        ArgumentNullException.ThrowIfNull(operation);

        var key = (moduleId, requestId);
        var lifetime = CancellationTokenSource.CreateLinkedTokenSource(contextToken);
        lock (_gate)
        {
            if (!_requests.TryAdd(key, lifetime))
            {
                lifetime.Dispose();
                throw new InvalidOperationException($"Request '{requestId}' is already active for module '{moduleId}'.");
            }
        }

        try
        {
            return await operation(lifetime.Token).ConfigureAwait(false);
        }
        finally
        {
            lock (_gate) _requests.Remove(key);
            lifetime.Dispose();
        }
    }

    public bool Cancel(string moduleId, string requestId)
    {
        CancellationTokenSource? lifetime;
        lock (_gate) _requests.TryGetValue((moduleId, requestId), out lifetime);
        if (lifetime is null) return false;
        TryCancel(lifetime);
        return true;
    }

    public int CancelModule(string moduleId) => CancelMatching(key => string.Equals(key.ModuleId, moduleId, StringComparison.Ordinal));

    public int CancelAll() => CancelMatching(static _ => true);

    private int CancelMatching(Func<(string ModuleId, string RequestId), bool> predicate)
    {
        CancellationTokenSource[] matches;
        lock (_gate)
            matches = _requests.Where(pair => predicate(pair.Key)).Select(pair => pair.Value).ToArray();
        foreach (var lifetime in matches) TryCancel(lifetime);
        return matches.Length;
    }

    private static void TryCancel(CancellationTokenSource source)
    {
        try { source.Cancel(); }
        catch (ObjectDisposedException) { }
    }
}
