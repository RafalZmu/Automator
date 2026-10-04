using System.Text.Json;
using System.Text.Json.Serialization;
using Automator.Application.Logging;

namespace Automator.Application.Automation;

/// <summary>
/// Owns the durable focus timer for the lifetime of the backend. The renderer only reads snapshots
/// and sends controls; it never owns a timer or authoritative session state.
/// </summary>
public sealed class AutomationFocusSessionCoordinator : IAutomationFocusSessionCoordinator, IAsyncDisposable
{
    public const string ModuleId = FocusSessionsModule.IdValue;
    public const string SettingsCollection = "settings";
    public const string SessionCollection = "sessions";
    public const string HistoryCollection = "history";
    public const int MaximumHistoryEntries = FocusSessionsModule.MaximumHistoryEntries;

    private const string CurrentId = "current";
    private const int RecordSchemaVersion = 1;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);
    private static readonly AutomationFocusSettings DefaultSettings = new(25, 5);
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private readonly IAutomationLibraryStore _libraryStore;
    private readonly IAutomationNotificationService _notifications;
    private readonly TimeProvider _timeProvider;
    private readonly IApplicationLog _log;
    private readonly SemaphoreSlim _stateGate = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly List<AutomationFocusSessionHistoryEntry> _history = [];

    private AutomationFocusSettings _settings = DefaultSettings;
    private AutomationFocusSessionSnapshot _session = CreateIdle(DefaultSettings);
    private DateTimeOffset? _startedUtc;
    private Task? _worker;
    private int _initialized;
    private int _disposed;

    public AutomationFocusSessionCoordinator(
        IAutomationLibraryStore libraryStore,
        IAutomationNotificationService notifications,
        TimeProvider? timeProvider = null)
        : this(libraryStore, notifications, timeProvider, null)
    {
    }

    public AutomationFocusSessionCoordinator(
        IAutomationLibraryStore libraryStore,
        IAutomationNotificationService notifications,
        TimeProvider? timeProvider,
        IApplicationLog? log)
    {
        _libraryStore = libraryStore ?? throw new ArgumentNullException(nameof(libraryStore));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _log = log ?? NullApplicationLog.Instance;
    }

    /// <summary>Loads durable state and starts the backend-owned timer loop.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            if (_initialized != 0) return;

            await LoadSettingsAsync(linked.Token).ConfigureAwait(false);
            await LoadHistoryAsync(linked.Token).ConfigureAwait(false);
            await RecoverCurrentSessionAsync(linked.Token).ConfigureAwait(false);
            linked.Token.ThrowIfCancellationRequested();

            Volatile.Write(ref _initialized, 1);
            _worker = Task.Run(() => WorkerLoopAsync(_lifetime.Token), CancellationToken.None);
        }
        finally { _stateGate.Release(); }
    }

    public async Task<AutomationFocusSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        using var linked = CreateOperationToken(cancellationToken);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            var now = _timeProvider.GetUtcNow();
            return new AutomationFocusSnapshot(WithCurrentRemaining(_session, now),
                _history.ToArray(), _settings);
        }
        finally { _stateGate.Release(); }
    }

    public async Task<AutomationFocusSessionSnapshot> SaveSettingsAsync(
        AutomationFocusSettings settings,
        CancellationToken cancellationToken)
    {
        FocusSessionsModule.ValidateSettings(settings);
        using var linked = CreateOperationToken(cancellationToken);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            await WriteRecordAsync(SettingsCollection, CurrentId, settings, linked.Token).ConfigureAwait(false);
            _settings = settings;
            if (_session.State == AutomationFocusSessionState.Idle)
                _session = CreateIdle(_settings);
            return WithCurrentRemaining(_session, _timeProvider.GetUtcNow());
        }
        finally { _stateGate.Release(); }
    }

    public async Task<AutomationFocusSessionSnapshot> StartAsync(CancellationToken cancellationToken)
    {
        using var linked = CreateOperationToken(cancellationToken);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            if (_session.State is not (AutomationFocusSessionState.Idle or AutomationFocusSessionState.Interrupted))
                throw new InvalidOperationException("End the current focus session before starting another.");

            var now = _timeProvider.GetUtcNow();
            var session = new AutomationFocusSessionSnapshot(
                AutomationFocusSessionState.Running,
                Guid.NewGuid().ToString("N"),
                AutomationFocusPhase.Focus,
                now.AddMinutes(_settings.FocusMinutes),
                null,
                0,
                _settings);
            await PersistSessionAsync(session, now, linked.Token).ConfigureAwait(false);
            _startedUtc = now;
            _session = session;
            return WithCurrentRemaining(session, now);
        }
        finally { _stateGate.Release(); }
    }

    public async Task<AutomationFocusSessionSnapshot> PauseAsync(CancellationToken cancellationToken)
    {
        using var linked = CreateOperationToken(cancellationToken);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            if (_session.State != AutomationFocusSessionState.Running)
                throw new InvalidOperationException("Only a running focus session can be paused.");

            var now = _timeProvider.GetUtcNow();
            var paused = _session with
            {
                State = AutomationFocusSessionState.Paused,
                RemainingMilliseconds = RemainingMilliseconds(_session, now),
                PhaseEndsAtUtc = null,
            };
            await PersistSessionAsync(paused, _startedUtc ?? now, linked.Token).ConfigureAwait(false);
            _session = paused;
            return paused;
        }
        finally { _stateGate.Release(); }
    }

    public async Task<AutomationFocusSessionSnapshot> ResumeAsync(CancellationToken cancellationToken)
    {
        using var linked = CreateOperationToken(cancellationToken);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            if (_session.State != AutomationFocusSessionState.Paused)
                throw new InvalidOperationException("Only a paused focus session can be resumed.");

            var now = _timeProvider.GetUtcNow();
            var remaining = Math.Max(0, _session.RemainingMilliseconds ?? 0);
            var resumed = _session with
            {
                State = AutomationFocusSessionState.Running,
                RemainingMilliseconds = null,
                PhaseEndsAtUtc = now.AddMilliseconds(remaining),
            };
            await PersistSessionAsync(resumed, _startedUtc ?? now, linked.Token).ConfigureAwait(false);
            _session = resumed;
            return WithCurrentRemaining(resumed, now);
        }
        finally { _stateGate.Release(); }
    }

    public async Task<AutomationFocusSessionSnapshot> SkipAsync(CancellationToken cancellationToken)
    {
        using var linked = CreateOperationToken(cancellationToken);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            if (_session.State is not (AutomationFocusSessionState.Running or AutomationFocusSessionState.Paused))
                throw new InvalidOperationException("A running or paused focus phase is required to skip.");

            var now = _timeProvider.GetUtcNow();
            var nextPhase = OtherPhase(_session.Phase!.Value);
            var duration = DurationFor(_session.Settings, nextPhase);
            var isPaused = _session.State == AutomationFocusSessionState.Paused;
            var skipped = _session with
            {
                Phase = nextPhase,
                PhaseEndsAtUtc = isPaused ? null : now.Add(duration),
                RemainingMilliseconds = isPaused ? (long)duration.TotalMilliseconds : null,
            };
            await PersistSessionAsync(skipped, _startedUtc ?? now, linked.Token).ConfigureAwait(false);
            _session = skipped;
            return WithCurrentRemaining(skipped, now);
        }
        finally { _stateGate.Release(); }
    }

    public async Task<AutomationFocusSessionSnapshot> EndAsync(CancellationToken cancellationToken)
    {
        using var linked = CreateOperationToken(cancellationToken);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            if (_session.State == AutomationFocusSessionState.Idle) return _session;

            var now = _timeProvider.GetUtcNow();
            if (_session.SessionId is { } id && _startedUtc is { } started)
            {
                var entry = CreateHistoryEntry(id, started, now,
                    _session.CompletedFocusPhases,
                    _session.State == AutomationFocusSessionState.Interrupted
                        ? AutomationFocusSessionState.Interrupted
                        : AutomationFocusSessionState.Idle);
                await WriteHistoryAsync(entry, linked.Token).ConfigureAwait(false);
            }

            await _libraryStore.DeleteAsync(ModuleId, SessionCollection, CurrentId, linked.Token).ConfigureAwait(false);
            _session = CreateIdle(_settings);
            _startedUtc = null;
            return _session;
        }
        finally { _stateGate.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _lifetime.Cancel();

        // Wait for any store mutation to complete before shutting down the worker. The persisted
        // running deadline is deliberately retained so startup recovery can resume or interrupt it.
        await _stateGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        _stateGate.Release();
        if (_worker is not null)
        {
            try { await _worker.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }
        _lifetime.Dispose();
    }

    private async Task WorkerLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                await Task.Delay(PollInterval, _timeProvider, cancellationToken).ConfigureAwait(false);
                try { await ProcessDuePhaseAsync(cancellationToken).ConfigureAwait(false); }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (Exception exception)
                {
                    // A failed durable transition is retried on the next poll. The in-memory session
                    // remains untouched until persistence succeeds.
                    _log.Write(ApplicationLogLevel.Warning, "FocusSessions.TickFailed",
                        "A focus timer transition failed and will be retried on the next tick.", exception);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            _log.Write(ApplicationLogLevel.Error, "FocusSessions.WorkerFailed",
                "The focus session timer stopped unexpectedly.", exception);
        }
    }

    private async Task ProcessDuePhaseAsync(CancellationToken cancellationToken)
    {
        AutomationNotification? notification = null;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            if (_initialized == 0 || _session.State != AutomationFocusSessionState.Running
                || _session.PhaseEndsAtUtc is not { } deadline || deadline > _timeProvider.GetUtcNow()) return;

            var now = _timeProvider.GetUtcNow();
            var oldPhase = _session.Phase!.Value;
            var nextPhase = OtherPhase(oldPhase);
            var completed = _session.CompletedFocusPhases
                + (oldPhase == AutomationFocusPhase.Focus ? 1 : 0);
            var transitioned = _session with
            {
                Phase = nextPhase,
                PhaseEndsAtUtc = now.Add(DurationFor(_session.Settings, nextPhase)),
                RemainingMilliseconds = null,
                CompletedFocusPhases = completed,
            };

            await PersistSessionAsync(transitioned, _startedUtc ?? now, linked.Token).ConfigureAwait(false);
            _session = transitioned;
            notification = nextPhase == AutomationFocusPhase.Break
                ? new AutomationNotification("Focus complete", "Time for a break.")
                : new AutomationNotification("Break complete", "Time to focus.");
        }
        finally { _stateGate.Release(); }

        if (notification is not null)
        {
            try { await _notifications.ShowAsync(notification, cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception exception)
            {
                _log.Write(ApplicationLogLevel.Warning, "FocusSessions.NotificationFailed",
                    "A focus phase changed, but its desktop notification could not be shown.", exception);
            }
        }
    }

    private async Task LoadSettingsAsync(CancellationToken cancellationToken)
    {
        var record = await _libraryStore.GetAsync(ModuleId, SettingsCollection, CurrentId, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            _settings = DefaultSettings;
            await WriteRecordAsync(SettingsCollection, CurrentId, _settings, cancellationToken).ConfigureAwait(false);
            return;
        }

        _settings = Deserialize<AutomationFocusSettings>(record, "focus settings");
        FocusSessionsModule.ValidateSettings(_settings);
    }

    private async Task LoadHistoryAsync(CancellationToken cancellationToken)
    {
        var records = await _libraryStore.ListAsync(ModuleId, HistoryCollection, cancellationToken).ConfigureAwait(false);
        _history.Clear();
        foreach (var record in records.OrderBy(item => item.UpdatedUtc))
            _history.Add(Deserialize<AutomationFocusSessionHistoryEntry>(record, "focus history entry"));
        await PruneHistoryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task RecoverCurrentSessionAsync(CancellationToken cancellationToken)
    {
        var record = await _libraryStore.GetAsync(ModuleId, SessionCollection, CurrentId, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            _session = CreateIdle(_settings);
            _startedUtc = null;
            return;
        }

        var persisted = Deserialize<PersistedFocusSession>(record, "focus session");
        ValidateSession(persisted.Session);
        _session = persisted.Session;
        _startedUtc = persisted.StartedUtc;

        if (_session.State == AutomationFocusSessionState.Running
            && _session.PhaseEndsAtUtc is { } deadline && deadline <= _timeProvider.GetUtcNow())
        {
            var now = _timeProvider.GetUtcNow();
            if (_session.SessionId is { } id && _startedUtc is { } started)
                await WriteHistoryAsync(CreateHistoryEntry(id, started, now,
                    _session.CompletedFocusPhases, AutomationFocusSessionState.Interrupted), cancellationToken).ConfigureAwait(false);

            var interrupted = _session with
            {
                State = AutomationFocusSessionState.Interrupted,
                PhaseEndsAtUtc = null,
                RemainingMilliseconds = null,
            };
            await PersistSessionAsync(interrupted, _startedUtc ?? now, cancellationToken).ConfigureAwait(false);
            _session = interrupted;
        }
    }

    private async Task WriteHistoryAsync(AutomationFocusSessionHistoryEntry entry, CancellationToken cancellationToken)
    {
        await WriteRecordAsync(HistoryCollection, entry.Id, entry, cancellationToken).ConfigureAwait(false);
        _history.RemoveAll(item => item.Id == entry.Id);
        _history.Add(entry);
        _history.Sort((left, right) => left.FinishedUtc.CompareTo(right.FinishedUtc));
        await PruneHistoryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task PruneHistoryAsync(CancellationToken cancellationToken)
    {
        if (_history.Count <= MaximumHistoryEntries) return;
        var excess = _history.Count - MaximumHistoryEntries;
        var removed = _history.Take(excess).ToArray();
        foreach (var item in removed)
            await _libraryStore.DeleteAsync(ModuleId, HistoryCollection, item.Id, cancellationToken).ConfigureAwait(false);
        _history.RemoveRange(0, excess);
    }

    private Task PersistSessionAsync(
        AutomationFocusSessionSnapshot session,
        DateTimeOffset startedUtc,
        CancellationToken cancellationToken) =>
        WriteRecordAsync(SessionCollection, CurrentId,
            new PersistedFocusSession(session, startedUtc), cancellationToken);

    private Task WriteRecordAsync<T>(string collection, string id, T value, CancellationToken cancellationToken) =>
        _libraryStore.UpsertAsync(new AutomationLibraryRecord(ModuleId, collection, id, RecordSchemaVersion,
            JsonSerializer.SerializeToElement(value, JsonOptions), _timeProvider.GetUtcNow()), cancellationToken);

    private static T Deserialize<T>(AutomationLibraryRecord record, string description)
    {
        if (record.SchemaVersion != RecordSchemaVersion)
            throw new InvalidDataException($"The saved {description} uses an unsupported schema version.");
        try
        {
            return record.Data.Deserialize<T>(JsonOptions)
                ?? throw new InvalidDataException($"The saved {description} is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"The saved {description} is invalid.", exception);
        }
    }

    private CancellationTokenSource CreateOperationToken(CancellationToken cancellationToken)
    {
        EnsureInitialized();
        return CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
    }

    private void EnsureInitialized()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (Volatile.Read(ref _initialized) == 0)
            throw new InvalidOperationException("The focus session coordinator has not been initialized.");
    }

    private static void ValidateSession(AutomationFocusSessionSnapshot session)
    {
        FocusSessionsModule.ValidateSettings(session.Settings);
        if (session.CompletedFocusPhases < 0)
            throw new InvalidDataException("The saved focus session has an invalid completed phase count.");
        if (session.State is AutomationFocusSessionState.Running or AutomationFocusSessionState.Paused)
        {
            if (string.IsNullOrWhiteSpace(session.SessionId) || session.Phase is null)
                throw new InvalidDataException("The saved focus session is missing its identity or phase.");
            if (session.State == AutomationFocusSessionState.Running && session.PhaseEndsAtUtc is null)
                throw new InvalidDataException("The running focus session is missing its phase deadline.");
            if (session.State == AutomationFocusSessionState.Paused && session.RemainingMilliseconds is null)
                throw new InvalidDataException("The paused focus session is missing its remaining time.");
        }
    }

    private static AutomationFocusSessionSnapshot WithCurrentRemaining(
        AutomationFocusSessionSnapshot session,
        DateTimeOffset now) => session.State == AutomationFocusSessionState.Running
            ? session with { RemainingMilliseconds = RemainingMilliseconds(session, now) }
            : session;

    private static long RemainingMilliseconds(AutomationFocusSessionSnapshot session, DateTimeOffset now) =>
        Math.Max(0, (long)Math.Ceiling(((session.PhaseEndsAtUtc ?? now) - now).TotalMilliseconds));

    private static TimeSpan DurationFor(AutomationFocusSettings settings, AutomationFocusPhase phase) =>
        TimeSpan.FromMinutes(phase == AutomationFocusPhase.Focus ? settings.FocusMinutes : settings.BreakMinutes);

    private static AutomationFocusPhase OtherPhase(AutomationFocusPhase phase) =>
        phase == AutomationFocusPhase.Focus ? AutomationFocusPhase.Break : AutomationFocusPhase.Focus;

    private static AutomationFocusSessionSnapshot CreateIdle(AutomationFocusSettings settings) =>
        new(AutomationFocusSessionState.Idle, null, null, null, null, 0, settings);

    private static AutomationFocusSessionHistoryEntry CreateHistoryEntry(
        string id,
        DateTimeOffset startedUtc,
        DateTimeOffset finishedUtc,
        int completedFocusPhases,
        AutomationFocusSessionState finalState) =>
        new(id, startedUtc.ToUniversalTime(), finishedUtc.ToUniversalTime(), completedFocusPhases,
            finalState, Math.Max(0, (long)(finishedUtc - startedUtc).TotalMilliseconds));

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }

    private sealed record PersistedFocusSession(AutomationFocusSessionSnapshot Session, DateTimeOffset StartedUtc);
}
