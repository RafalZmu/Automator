using Automator.Core.Launcher;

namespace Automator.Application.Launcher;

public interface ILaunchDelay
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

public interface IApplicationActivator
{
    Task ActivateAsync(AppBinding binding, string? previousForegroundHwnd, CancellationToken cancellationToken);
}

/// <summary>Debounces the alias recognition delay while keeping launched activation independent.</summary>
public sealed class AliasLaunchScheduler : IDisposable
{
    public static readonly TimeSpan DefaultDelay = TimeSpan.FromMilliseconds(420);

    private readonly object _sync = new();
    private readonly ILaunchDelay _delay;
    private readonly IApplicationActivator _activator;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _pendingDelay;
    private long _generation;
    private bool _disposed;

    public AliasLaunchScheduler(ILaunchDelay delay, IApplicationActivator activator)
    {
        _delay = delay ?? throw new ArgumentNullException(nameof(delay));
        _activator = activator ?? throw new ArgumentNullException(nameof(activator));
    }

    public Task ScheduleAsync(AppBinding binding, string? previousForegroundHwnd)
    {
        ArgumentNullException.ThrowIfNull(binding);
        CancellationTokenSource delayCancellation;
        long generation;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            CancelPendingLocked();
            generation = ++_generation;
            delayCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            _pendingDelay = delayCancellation;
        }

        return RunAsync(Clone(binding), previousForegroundHwnd, generation, delayCancellation);
    }

    public void CancelPending()
    {
        lock (_sync)
        {
            _generation++;
            CancelPendingLocked();
        }
    }

    private async Task RunAsync(AppBinding binding, string? previousForegroundHwnd, long generation, CancellationTokenSource delayCancellation)
    {
        try
        {
            await _delay.DelayAsync(DefaultDelay, delayCancellation.Token).ConfigureAwait(false);
            lock (_sync)
            {
                if (_disposed || _generation != generation || delayCancellation.IsCancellationRequested) return;
                if (ReferenceEquals(_pendingDelay, delayCancellation)) _pendingDelay = null;
            }

            // Once activation begins, query/tab/panel changes may cancel only another pending delay.
            await _activator.ActivateAsync(binding, previousForegroundHwnd, _lifetime.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (delayCancellation.IsCancellationRequested || _lifetime.IsCancellationRequested)
        {
            // Cancellation is expected when the user changes context during the delay or host shutdown begins.
        }
        finally
        {
            lock (_sync)
            {
                if (ReferenceEquals(_pendingDelay, delayCancellation)) _pendingDelay = null;
            }

            delayCancellation.Dispose();
        }
    }

    private void CancelPendingLocked()
    {
        var cancellation = _pendingDelay;
        _pendingDelay = null;
        cancellation?.Cancel();
    }

    private static AppBinding Clone(AppBinding binding) => new(binding.Id, binding.Name, binding.TargetPath, binding.Alias, binding.Arguments);

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            _generation++;
            CancelPendingLocked();
            _lifetime.Cancel();
        }
    }
}
