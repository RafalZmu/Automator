using System.Threading.Channels;

namespace Automator.Application.Launcher;

/// <summary>Runs state-changing commands one at a time on a single asynchronous reader.</summary>
public sealed class SerializedCommandQueue : IAsyncDisposable
{
    private readonly Channel<Func<ValueTask>> _commands = Channel.CreateUnbounded<Func<ValueTask>>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false, AllowSynchronousContinuations = false });
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task _reader;
    private int _completed;

    public SerializedCommandQueue(Action<Exception>? unhandledError = null)
    {
        _reader = RunAsync(unhandledError);
    }

    public Task<T> EnqueueAsync<T>(Func<ValueTask<T>> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (Volatile.Read(ref _completed) != 0)
            return Task.FromException<T>(new ObjectDisposedException(nameof(SerializedCommandQueue)));

        var queued = _commands.Writer.TryWrite(async () =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                completion.TrySetResult(await command().ConfigureAwait(false));
            }
            catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
            {
                completion.TrySetCanceled(exception.CancellationToken);
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        });

        if (!queued) completion.TrySetException(new ObjectDisposedException(nameof(SerializedCommandQueue)));
        return completion.Task;
    }

    public bool TryPost(Func<ValueTask> command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (Volatile.Read(ref _completed) != 0) return false;
        return _commands.Writer.TryWrite(async () => await command().ConfigureAwait(false));
    }

    private async Task RunAsync(Action<Exception>? unhandledError)
    {
        try
        {
            await foreach (var command in _commands.Reader.ReadAllAsync(_shutdown.Token).ConfigureAwait(false))
            {
                try { await command().ConfigureAwait(false); }
                catch (Exception exception) { unhandledError?.Invoke(exception); }
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested) { }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _completed, 1) != 0) return;
        _commands.Writer.TryComplete();
        await _reader.ConfigureAwait(false);
        _shutdown.Cancel();
        _shutdown.Dispose();
    }
}
