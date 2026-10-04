using System.Threading.Channels;
using Automator.Application.Logging;
using Automator.Core.Automation;

namespace Automator.Application.Automation;

public sealed record KeyboardDeliveryState(
    string ModuleId,
    bool Visible,
    bool NativeForeground,
    bool RendererFocused,
    bool NativeDialog,
    bool KeyRecording)
{
    public bool CanDeliver => Visible && NativeForeground && RendererFocused && !NativeDialog && !KeyRecording;
}

/// <summary>Asynchronously fans the host's normalized key events out to the eligible active module.</summary>
public sealed class AutomationKeyboardEventHub
{
    public const int SubscriberCapacity = 256;

    private readonly object _gate = new();
    private readonly Dictionary<Guid, Subscriber> _subscribers = [];
    private readonly IApplicationLog _log;
    private KeyboardDeliveryState? _deliveryContext;

    public AutomationKeyboardEventHub(IApplicationLog? log = null) => _log = log ?? NullApplicationLog.Instance;

    public int ActiveSubscriptionCount
    {
        get { lock (_gate) return _subscribers.Count; }
    }

    public void SetDeliveryContext(KeyboardDeliveryState? state)
    {
        List<Subscriber>? revoked = null;
        lock (_gate)
        {
            var previous = _deliveryContext;
            _deliveryContext = state;
            if (previous is { CanDeliver: true } &&
                (state is not { CanDeliver: true } || !string.Equals(previous.ModuleId, state.ModuleId, StringComparison.Ordinal)))
            {
                revoked = _subscribers.Values.ToList();
                _subscribers.Clear();
            }
        }

        if (revoked is not null)
            foreach (var subscriber in revoked)
                StopSubscriber(subscriber);
    }

    public void Publish(KeyInputEvent inputEvent)
    {
        ArgumentNullException.ThrowIfNull(inputEvent);
        List<Subscriber>? overflowed = null;
        lock (_gate)
        {
            if (_deliveryContext is not { CanDeliver: true } state) return;
            foreach (var subscriber in _subscribers.Values.Where(item => string.Equals(item.ModuleId, state.ModuleId, StringComparison.Ordinal)).ToArray())
            {
                if (subscriber.Events.Writer.TryWrite(inputEvent)) continue;
                _subscribers.Remove(subscriber.Id);
                (overflowed ??= []).Add(subscriber);
            }
        }

        if (overflowed is not null)
        {
            foreach (var subscriber in overflowed)
            {
                _log.Write(ApplicationLogLevel.Warning, "automation.keyboard.subscriber-overflow",
                    "A keyboard subscriber exceeded its bounded event queue and was detached.",
                    properties: new Dictionary<string, object?> { ["moduleId"] = subscriber.ModuleId, ["capacity"] = SubscriberCapacity });
                StopSubscriber(subscriber);
            }
        }
    }

    public ValueTask<IAsyncDisposable> SubscribeAsync(string moduleId, Func<KeyInputEvent, ValueTask> receiver, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        ArgumentNullException.ThrowIfNull(receiver);
        cancellationToken.ThrowIfCancellationRequested();

        var subscriber = new Subscriber(Guid.NewGuid(), moduleId, receiver);
        lock (_gate)
        {
            if (_deliveryContext is not { CanDeliver: true } state ||
                !string.Equals(moduleId, state.ModuleId, StringComparison.Ordinal))
                throw new InvalidOperationException("Keyboard input can only be subscribed while its module is active and eligible.");
            _subscribers.Add(subscriber.Id, subscriber);
            subscriber.Start(DispatchAsync);
            subscriber.CancellationRegistration = cancellationToken.Register(() => Detach(subscriber));
        }

        return ValueTask.FromResult<IAsyncDisposable>(new Subscription(this, subscriber));
    }

    private async Task DispatchAsync(Subscriber subscriber)
    {
        try
        {
            await foreach (var inputEvent in subscriber.Events.Reader.ReadAllAsync(subscriber.Stop.Token).ConfigureAwait(false))
                await subscriber.Receiver(inputEvent).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (subscriber.Stop.IsCancellationRequested) { }
        catch (Exception exception)
        {
            _log.Write(ApplicationLogLevel.Warning, "automation.keyboard.subscriber-failed",
                "A keyboard subscriber failed and was detached.", exception,
                new Dictionary<string, object?> { ["moduleId"] = subscriber.ModuleId });
            Detach(subscriber);
        }
        finally
        {
            subscriber.CancellationRegistration.Dispose();
        }
    }

    private void Detach(Subscriber subscriber)
    {
        lock (_gate) _subscribers.Remove(subscriber.Id);
        StopSubscriber(subscriber);
    }

    private static void StopSubscriber(Subscriber subscriber)
    {
        if (!subscriber.TryBeginStop()) return;
        try
        {
            subscriber.Events.Writer.TryComplete();
            while (subscriber.Events.Reader.TryRead(out _)) { }
            subscriber.Stop.Cancel();
        }
        finally
        {
            subscriber.CompleteStop();
        }
    }

    private async ValueTask DisposeSubscriberAsync(Subscriber subscriber)
    {
        Detach(subscriber);
        await subscriber.Stopped.ConfigureAwait(false);
        try { await subscriber.Worker.ConfigureAwait(false); }
        finally { subscriber.DisposeStop(); }
    }

    private sealed class Subscriber(Guid id, string moduleId, Func<KeyInputEvent, ValueTask> receiver)
    {
        public Guid Id { get; } = id;
        public string ModuleId { get; } = moduleId;
        public Func<KeyInputEvent, ValueTask> Receiver { get; } = receiver;
        public Channel<KeyInputEvent> Events { get; } = Channel.CreateBounded<KeyInputEvent>(new BoundedChannelOptions(SubscriberCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });
        public CancellationTokenSource Stop { get; } = new();
        public CancellationTokenRegistration CancellationRegistration { get; set; }
        public Task Worker { get; private set; } = Task.CompletedTask;
        private readonly TaskCompletionSource _stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _stopStarted;
        private int _stopDisposed;
        public Task Stopped => _stopped.Task;
        public bool TryBeginStop() => Interlocked.CompareExchange(ref _stopStarted, 1, 0) == 0;
        public void CompleteStop() => _stopped.TrySetResult();
        public void DisposeStop()
        {
            if (Interlocked.Exchange(ref _stopDisposed, 1) == 0) Stop.Dispose();
        }
        public void Start(Func<Subscriber, Task> dispatch) => Worker = dispatch(this);
    }

    private sealed class Subscription(AutomationKeyboardEventHub hub, Subscriber subscriber) : IAsyncDisposable
    {
        private int _disposed;
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            await hub.DisposeSubscriberAsync(subscriber).ConfigureAwait(false);
        }
    }
}
