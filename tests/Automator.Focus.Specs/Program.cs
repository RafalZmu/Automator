using System.Text.Json;
using Automator.Application.Automation;
using Automator.Application.Logging;

var checks = new (string Name, Func<Task> Run)[]
{
    ("settings edits affect the next session without changing frozen active durations", SettingsAreFrozenPerSession),
    ("settings enforce host duration bounds", SettingsRespectHostBounds),
    ("pause resume skip and end preserve timer state and append history", ControlsPersistStateAndHistory),
    ("natural phase transitions persist before best effort notification", NaturalBoundaryPersistsBeforeNotification),
    ("timer retries durable transition failures and logs them", WorkerFailureIsLoggedAndRetried),
    ("startup resumes future and paused sessions and interrupts overdue running sessions", StartupRecoveryIsExplicit),
    ("disposal stops the timer worker while preserving the active snapshot", DisposalStopsTheWorker),
    ("work time requires a description and persists optional tags to history", WorkTimeRequiresDescriptionAndStoresTags),
    ("work time pending entry can resume accumulated time or be discarded", WorkTimePendingCanResumeAndDiscard),
    ("saved work-time entries can be edited and deleted without losing interval timing", WorkTimeEntriesCanBeEditedAndDeleted),
    ("active work time persists across backend restart", ActiveWorkTimeSurvivesRestart),
};

var failures = 0;
foreach (var (name, run) in checks)
{
    try
    {
        await run();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.WriteLine($"FAIL {name}: {exception.Message}");
    }
}

Console.WriteLine($"{checks.Length - failures}/{checks.Length} checks passed");
return failures == 0 ? 0 : 1;

static async Task SettingsAreFrozenPerSession()
{
    var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));
    var store = new FocusLibraryStore();
    var notifications = new RecordingNotificationService(store);
    await using var lease = await CoordinatorHarness.CreateAsync(store, notifications, time);
    var coordinator = lease.Coordinator;

    var initial = await coordinator.GetSnapshotAsync(CancellationToken.None);
    Check.Equal(new AutomationFocusSettings(25, 5), initial.Settings);
    await coordinator.SaveSettingsAsync(new AutomationFocusSettings(45, 12), CancellationToken.None);
    var started = await coordinator.StartAsync(CancellationToken.None);
    Check.Equal(new AutomationFocusSettings(45, 12), started.Settings);

    await coordinator.SaveSettingsAsync(new AutomationFocusSettings(50, 10), CancellationToken.None);
    var afterEdit = await coordinator.GetSnapshotAsync(CancellationToken.None);
    Check.Equal(new AutomationFocusSettings(50, 10), afterEdit.Settings);
    Check.Equal(new AutomationFocusSettings(45, 12), afterEdit.Session.Settings);
}

static async Task ControlsPersistStateAndHistory()
{
    var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));
    var store = new FocusLibraryStore();
    var notifications = new RecordingNotificationService(store);
    await using var lease = await CoordinatorHarness.CreateAsync(store, notifications, time);
    var coordinator = lease.Coordinator;
    await coordinator.SaveSettingsAsync(new AutomationFocusSettings(1, 2), CancellationToken.None);
    var started = await coordinator.StartAsync(CancellationToken.None);

    time.Advance(TimeSpan.FromSeconds(17));
    var paused = await coordinator.PauseAsync(CancellationToken.None);
    Check.Equal(AutomationFocusSessionState.Paused, paused.State);
    Check.Equal(43_000L, paused.RemainingMilliseconds);
    time.Advance(TimeSpan.FromMinutes(3));
    var resumed = await coordinator.ResumeAsync(CancellationToken.None);
    Check.Equal(AutomationFocusSessionState.Running, resumed.State);
    Check.Equal(time.GetUtcNow().AddSeconds(43), resumed.PhaseEndsAtUtc);

    var skippedToBreak = await coordinator.SkipAsync(CancellationToken.None);
    Check.Equal(AutomationFocusPhase.Break, skippedToBreak.Phase);
    Check.Equal(2, skippedToBreak.Settings.BreakMinutes);
    var pausedBreak = await coordinator.PauseAsync(CancellationToken.None);
    Check.Equal(120_000L, pausedBreak.RemainingMilliseconds);
    var skippedToFocus = await coordinator.SkipAsync(CancellationToken.None);
    Check.Equal(AutomationFocusSessionState.Paused, skippedToFocus.State);
    Check.Equal(AutomationFocusPhase.Focus, skippedToFocus.Phase);
    Check.Equal(60_000L, skippedToFocus.RemainingMilliseconds);
    Check.Equal(0, notifications.Count);

    var ended = await coordinator.EndAsync(CancellationToken.None);
    Check.Equal(AutomationFocusSessionState.Idle, ended.State);
    Check.Equal(null, ended.SessionId);
    var snapshot = await coordinator.GetSnapshotAsync(CancellationToken.None);
    Check.Equal(1, snapshot.History.Count);
    Check.Equal(started.SessionId, snapshot.History[0].Id);
    Check.Equal(AutomationFocusSessionState.Idle, snapshot.History[0].FinalState);
    Check.True(snapshot.History[0].DurationMilliseconds >= 180_017);
    Check.True(store.LastRecord("focus-sessions", "history", started.SessionId!) is not null);
}

static async Task SettingsRespectHostBounds()
{
    var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));
    var store = new FocusLibraryStore();
    await using var lease = await CoordinatorHarness.CreateAsync(store, new RecordingNotificationService(store), time);
    var coordinator = lease.Coordinator;

    await Check.ThrowsAsync<InvalidDataException>(
        () => coordinator.SaveSettingsAsync(new AutomationFocusSettings(0, 5), CancellationToken.None));
    await Check.ThrowsAsync<InvalidDataException>(
        () => coordinator.SaveSettingsAsync(new AutomationFocusSettings(25, 121), CancellationToken.None));
    var snapshot = await coordinator.GetSnapshotAsync(CancellationToken.None);
    Check.Equal(new AutomationFocusSettings(25, 5), snapshot.Settings);
}

static async Task NaturalBoundaryPersistsBeforeNotification()
{
    var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));
    var store = new FocusLibraryStore();
    var notifications = new RecordingNotificationService(store);
    await using var lease = await CoordinatorHarness.CreateAsync(store, notifications, time);
    var coordinator = lease.Coordinator;
    await coordinator.SaveSettingsAsync(new AutomationFocusSettings(1, 1), CancellationToken.None);
    await coordinator.StartAsync(CancellationToken.None);
    await time.WaitForTimerAsync();
    store.Events.Clear();

    time.Advance(TimeSpan.FromMinutes(1));
    await Check.EventuallyAsync(async () =>
    {
        var current = await coordinator.GetSnapshotAsync(CancellationToken.None);
        return current.Session.Phase == AutomationFocusPhase.Break && notifications.Count == 1;
    });

    var snapshot = await coordinator.GetSnapshotAsync(CancellationToken.None);
    Check.Equal(AutomationFocusPhase.Break, snapshot.Session.Phase);
    Check.Equal(1, snapshot.Session.CompletedFocusPhases);
    Check.Equal("break", notifications.PersistedPhaseAtNotification);
    var stateWrite = store.Events.FindIndex(value => value == "upsert:sessions:current");
    var notification = store.Events.FindIndex(value => value == "notification");
    Check.True(stateWrite >= 0 && notification > stateWrite);
    Check.Equal(AutomationFocusPhase.Break, snapshot.Session.Phase);

    notifications.ShouldThrow = true;
    time.Advance(TimeSpan.FromMinutes(1));
    await Check.EventuallyAsync(async () =>
    {
        var current = await coordinator.GetSnapshotAsync(CancellationToken.None);
        return current.Session.Phase == AutomationFocusPhase.Focus;
    });
    Check.Equal(AutomationFocusPhase.Focus, (await coordinator.GetSnapshotAsync(CancellationToken.None)).Session.Phase);
}

static async Task StartupRecoveryIsExplicit()
{
    var origin = new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);
    var store = new FocusLibraryStore();
    var time = new ManualTimeProvider(origin);
    await using var first = await CoordinatorHarness.CreateAsync(store, new RecordingNotificationService(store), time);
    await first.Coordinator.SaveSettingsAsync(new AutomationFocusSettings(10, 3), CancellationToken.None);
    var running = await first.Coordinator.StartAsync(CancellationToken.None);
    await first.DisposeAsync();

    time.Advance(TimeSpan.FromMinutes(2));
    await using (var restarted = await CoordinatorHarness.CreateAsync(store, new RecordingNotificationService(store), time))
    {
        var restored = await restarted.Coordinator.GetSnapshotAsync(CancellationToken.None);
        Check.Equal(AutomationFocusSessionState.Running, restored.Session.State);
        Check.Equal(running.PhaseEndsAtUtc, restored.Session.PhaseEndsAtUtc);
        Check.Equal(running.SessionId, restored.Session.SessionId);
        await restarted.Coordinator.PauseAsync(CancellationToken.None);
    }

    time.Advance(TimeSpan.FromHours(2));
    await using (var pausedRestart = await CoordinatorHarness.CreateAsync(store, new RecordingNotificationService(store), time))
    {
        var restoredPaused = await pausedRestart.Coordinator.GetSnapshotAsync(CancellationToken.None);
        Check.Equal(AutomationFocusSessionState.Paused, restoredPaused.Session.State);
        Check.True(restoredPaused.Session.RemainingMilliseconds > 0);
    }

    var expiredStore = new FocusLibraryStore();
    var expiredClock = new ManualTimeProvider(origin);
    await using var expiring = await CoordinatorHarness.CreateAsync(expiredStore, new RecordingNotificationService(expiredStore), expiredClock);
    await expiring.Coordinator.SaveSettingsAsync(new AutomationFocusSettings(1, 1), CancellationToken.None);
    await expiring.Coordinator.StartAsync(CancellationToken.None);
    await expiring.DisposeAsync();
    expiredClock.Advance(TimeSpan.FromMinutes(2));
    var noCatchUpNotifications = new RecordingNotificationService(expiredStore);
    await using var recovered = await CoordinatorHarness.CreateAsync(expiredStore, noCatchUpNotifications, expiredClock);
    var interrupted = await recovered.Coordinator.GetSnapshotAsync(CancellationToken.None);
    Check.Equal(AutomationFocusSessionState.Interrupted, interrupted.Session.State);
    Check.Equal(1, interrupted.History.Count);
    Check.True(interrupted.History[0].FinalState == AutomationFocusSessionState.Interrupted,
        $"Recovered history should be interrupted, got {interrupted.History[0].FinalState}.");
    Check.Equal(0, noCatchUpNotifications.Count);
    await recovered.Coordinator.EndAsync(CancellationToken.None);
    var dismissed = await recovered.Coordinator.GetSnapshotAsync(CancellationToken.None);
    Check.True(dismissed.History[0].FinalState == AutomationFocusSessionState.Interrupted,
        $"Dismissing an interrupted session should preserve its history, got {dismissed.History[0].FinalState}.");
}

static async Task WorkerFailureIsLoggedAndRetried()
{
    var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));
    var store = new FocusLibraryStore();
    var notifications = new RecordingNotificationService(store);
    var log = new RecordingApplicationLog();
    await using var lease = await CoordinatorHarness.CreateAsync(store, notifications, time, log);
    var coordinator = lease.Coordinator;
    await coordinator.SaveSettingsAsync(new AutomationFocusSettings(1, 1), CancellationToken.None);
    await coordinator.StartAsync(CancellationToken.None);
    await time.WaitForTimerAsync();

    store.FailSessionWrites = true;
    time.Advance(TimeSpan.FromMinutes(1));
    await Check.EventuallyAsync(() => log.HasEvent("FocusSessions.TickFailed"));
    var unchanged = await coordinator.GetSnapshotAsync(CancellationToken.None);
    Check.Equal(AutomationFocusPhase.Focus, unchanged.Session.Phase);
    Check.Equal(0, notifications.Count);

    store.FailSessionWrites = false;
    await time.WaitForTimerAsync();
    time.Advance(TimeSpan.FromSeconds(1));
    await Check.EventuallyAsync(async () =>
        (await coordinator.GetSnapshotAsync(CancellationToken.None)).Session.Phase == AutomationFocusPhase.Break);
    Check.Equal(1, notifications.Count);
}

static async Task DisposalStopsTheWorker()
{
    var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));
    var store = new FocusLibraryStore();
    var notifications = new RecordingNotificationService(store);
    await using var lease = await CoordinatorHarness.CreateAsync(store, notifications, time);
    var coordinator = lease.Coordinator;
    await coordinator.SaveSettingsAsync(new AutomationFocusSettings(1, 1), CancellationToken.None);
    var started = await coordinator.StartAsync(CancellationToken.None);
    await lease.DisposeAsync();
    time.Advance(TimeSpan.FromMinutes(2));
    await Task.Delay(20);

    Check.Equal(0, notifications.Count);
    var persisted = store.LastRecord("focus-sessions", "sessions", "current");
    Check.True(persisted is not null);
    Check.Equal("running", persisted!.Data.GetProperty("session").GetProperty("state").GetString());
    Check.Equal(started.PhaseEndsAtUtc!.Value, persisted.Data.GetProperty("session").GetProperty("phaseEndsAtUtc").GetDateTimeOffset());
}

static async Task WorkTimeRequiresDescriptionAndStoresTags()
{
    var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));
    var store = new FocusLibraryStore();
    await using var lease = await WorkTimeCoordinatorHarness.CreateAsync(store, time);
    var coordinator = lease.Coordinator;

    var started = await coordinator.StartAsync(CancellationToken.None);
    time.Advance(TimeSpan.FromMinutes(12));
    var stopped = await coordinator.StopAsync(CancellationToken.None);
    Check.Equal(started.Active!.Id, stopped.Pending!.Id);
    Check.Equal(720_000L, stopped.Pending.DurationMilliseconds);
    Check.Equal(0, stopped.History.Count);
    await Check.ThrowsAsync<InvalidDataException>(() => coordinator.SaveEntryAsync("  ", [], CancellationToken.None));

    var saved = await coordinator.SaveEntryAsync("Build release", [" feature ", "#deep", "feature"], CancellationToken.None);
    Check.Equal(null, saved.Pending);
    Check.Equal(1, saved.History.Count);
    Check.Equal("Build release", saved.History[0].Description);
    Check.True(saved.History[0].Tags.SequenceEqual(["feature", "#deep"]));
    Check.Equal(720_000L, saved.History[0].DurationMilliseconds);
    Check.True(store.LastRecord("focus-sessions", "work-time-history", started.Active!.Id) is not null);
}

static async Task WorkTimePendingCanResumeAndDiscard()
{
    var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));
    var store = new FocusLibraryStore();
    await using var lease = await WorkTimeCoordinatorHarness.CreateAsync(store, time);
    var coordinator = lease.Coordinator;

    await coordinator.StartAsync(CancellationToken.None);
    time.Advance(TimeSpan.FromMinutes(20));
    await coordinator.StopAsync(CancellationToken.None);
    time.Advance(TimeSpan.FromMinutes(30));
    var resumed = await coordinator.ResumePendingAsync(CancellationToken.None);
    Check.Equal(1_200_000L, resumed.Active!.ElapsedMilliseconds);
    time.Advance(TimeSpan.FromMinutes(7));
    var stoppedAgain = await coordinator.StopAsync(CancellationToken.None);
    Check.Equal(1_620_000L, stoppedAgain.Pending!.DurationMilliseconds);
    var discarded = await coordinator.DiscardPendingAsync(CancellationToken.None);
    Check.Equal(null, discarded.Pending);
    Check.Equal(null, discarded.Active);
    Check.Equal(0, discarded.History.Count);
}

static async Task WorkTimeEntriesCanBeEditedAndDeleted()
{
    var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));
    var store = new FocusLibraryStore();
    await using var lease = await WorkTimeCoordinatorHarness.CreateAsync(store, time);
    var coordinator = lease.Coordinator;

    var started = await coordinator.StartAsync(CancellationToken.None);
    time.Advance(TimeSpan.FromSeconds(75));
    var stopped = await coordinator.StopAsync(CancellationToken.None);
    var saved = await coordinator.SaveEntryAsync("Original description", ["old-tag"], CancellationToken.None);
    var original = saved.History.Single();
    Check.Equal(75_000L, original.DurationMilliseconds);

    var updated = await coordinator.UpdateEntryAsync(original.Id, "Corrected description", ["planning", "review"], CancellationToken.None);
    var edited = updated.History.Single();
    Check.Equal("Corrected description", edited.Description);
    Check.True(edited.Tags.SequenceEqual(["planning", "review"]));
    Check.Equal(original.StartedUtc, edited.StartedUtc);
    Check.Equal(original.StoppedUtc, edited.StoppedUtc);
    Check.Equal(original.DurationMilliseconds, edited.DurationMilliseconds);
    var persisted = store.LastRecord("focus-sessions", "work-time-history", original.Id);
    Check.Equal("Corrected description", persisted!.Data.GetProperty("description").GetString());

    var deleted = await coordinator.DeleteEntryAsync(original.Id, CancellationToken.None);
    Check.Equal(0, deleted.History.Count);
    Check.Equal(null, store.LastRecord("focus-sessions", "work-time-history", original.Id));
    await Check.ThrowsAsync<InvalidOperationException>(() => coordinator.DeleteEntryAsync(original.Id, CancellationToken.None));
    Check.Equal(started.Active!.Id, stopped.Pending!.Id);
}

static async Task ActiveWorkTimeSurvivesRestart()
{
    var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));
    var store = new FocusLibraryStore();
    await using (var first = await WorkTimeCoordinatorHarness.CreateAsync(store, time))
    {
        await first.Coordinator.StartAsync(CancellationToken.None);
        time.Advance(TimeSpan.FromMinutes(2));
    }

    await using var restarted = await WorkTimeCoordinatorHarness.CreateAsync(store, time);
    var restored = await restarted.Coordinator.GetSnapshotAsync(CancellationToken.None);
    Check.Equal(120_000L, restored.Active!.ElapsedMilliseconds);
    time.Advance(TimeSpan.FromMinutes(3));
    var stopped = await restarted.Coordinator.StopAsync(CancellationToken.None);
    Check.Equal(300_000L, stopped.Pending!.DurationMilliseconds);
}

static class CoordinatorHarness
{
    public static async Task<FocusCoordinatorLease> CreateAsync(
        IAutomationLibraryStore store, IAutomationNotificationService notifications, TimeProvider timeProvider,
        IApplicationLog? log = null)
    {
        var implementation = typeof(IAutomationFocusSessionCoordinator).Assembly
            .GetType("Automator.Application.Automation.AutomationFocusSessionCoordinator");
        Check.True(implementation is not null, "The host-owned focus coordinator has not been added yet.");
        var arguments = log is null
            ? new object?[] { store, notifications, timeProvider }
            : [store, notifications, timeProvider, log];
        var instance = Activator.CreateInstance(implementation!, arguments)
            ?? throw new InvalidOperationException("The focus coordinator could not be constructed.");
        var coordinator = instance as IAutomationFocusSessionCoordinator
            ?? throw new InvalidOperationException("The focus coordinator must implement its published contract.");
        var lifetime = instance as IAsyncDisposable
            ?? throw new InvalidOperationException("The focus coordinator must expose async shutdown.");
        try
        {
            var initialize = implementation!.GetMethod("InitializeAsync", [typeof(CancellationToken)]);
            Check.True(initialize is not null, "The coordinator must expose InitializeAsync(CancellationToken).");
            var initialization = initialize!.Invoke(instance, [CancellationToken.None]);
            Check.True(initialization is Task, "Focus coordinator initialization must return Task.");
            await (Task)initialization!;
            return new FocusCoordinatorLease(coordinator, lifetime);
        }
        catch
        {
            await lifetime.DisposeAsync();
            throw;
        }
    }
}

static class WorkTimeCoordinatorHarness
{
    public static async Task<WorkTimeCoordinatorLease> CreateAsync(IAutomationLibraryStore store, TimeProvider timeProvider)
    {
        var implementation = typeof(IAutomationWorkTimeCoordinator).Assembly
            .GetType("Automator.Application.Automation.AutomationWorkTimeCoordinator");
        Check.True(implementation is not null, "The host-owned work-time coordinator has not been added yet.");
        var instance = Activator.CreateInstance(implementation!, store, timeProvider)
            ?? throw new InvalidOperationException("The work-time coordinator could not be constructed.");
        var coordinator = instance as IAutomationWorkTimeCoordinator
            ?? throw new InvalidOperationException("The work-time coordinator must implement its published contract.");
        var lifetime = instance as IAsyncDisposable
            ?? throw new InvalidOperationException("The work-time coordinator must expose async shutdown.");
        try
        {
            var initialize = implementation!.GetMethod("InitializeAsync", [typeof(CancellationToken)]);
            Check.True(initialize is not null, "The work-time coordinator must expose InitializeAsync(CancellationToken).");
            var initialization = initialize!.Invoke(instance, [CancellationToken.None]);
            Check.True(initialization is Task, "Work-time coordinator initialization must return Task.");
            await (Task)initialization!;
            return new WorkTimeCoordinatorLease(coordinator, lifetime);
        }
        catch
        {
            await lifetime.DisposeAsync();
            throw;
        }
    }
}

sealed class WorkTimeCoordinatorLease(IAutomationWorkTimeCoordinator coordinator, IAsyncDisposable lifetime) : IAsyncDisposable
{
    public IAutomationWorkTimeCoordinator Coordinator { get; } = coordinator;
    public ValueTask DisposeAsync() => lifetime.DisposeAsync();
}

sealed class FocusCoordinatorLease(IAutomationFocusSessionCoordinator coordinator, IAsyncDisposable lifetime) : IAsyncDisposable
{
    public IAutomationFocusSessionCoordinator Coordinator { get; } = coordinator;
    public ValueTask DisposeAsync() => lifetime.DisposeAsync();
}

sealed class FocusLibraryStore : IAutomationLibraryStore
{
    private readonly object _sync = new();
    private readonly Dictionary<(string Module, string Collection, string Id), AutomationLibraryRecord> _records = [];
    public List<string> Events { get; } = [];
    public bool FailSessionWrites { get; set; }

    public Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(string moduleId, string collection, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            IReadOnlyList<AutomationLibraryRecord> result = _records.Values
                .Where(record => record.ModuleId == moduleId && record.Collection == collection)
                .OrderBy(record => record.UpdatedUtc).Select(Clone).ToArray();
            return Task.FromResult(result);
        }
    }

    public Task<AutomationLibraryRecord?> GetAsync(string moduleId, string collection, string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            return Task.FromResult(_records.TryGetValue((moduleId, collection, id), out var record) ? Clone(record) : null);
        }
    }

    public Task UpsertAsync(AutomationLibraryRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (FailSessionWrites && record.Collection == "sessions")
            throw new IOException("Simulated focus session store failure.");
        lock (_sync)
        {
            _records[(record.ModuleId, record.Collection, record.Id)] = Clone(record);
            Events.Add($"upsert:{record.Collection}:{record.Id}");
        }
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string moduleId, string collection, string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            var deleted = _records.Remove((moduleId, collection, id));
            if (deleted) Events.Add($"delete:{collection}:{id}");
            return Task.FromResult(deleted);
        }
    }

    public AutomationLibraryRecord? LastRecord(string moduleId, string collection, string id)
    {
        lock (_sync) return _records.TryGetValue((moduleId, collection, id), out var record) ? Clone(record) : null;
    }

    private static AutomationLibraryRecord Clone(AutomationLibraryRecord record) => record with { Data = record.Data.Clone() };
}

sealed class RecordingApplicationLog : IApplicationLog
{
    private readonly object _sync = new();
    private readonly List<(ApplicationLogLevel Level, string EventName)> _events = [];

    public bool HasEvent(string eventName)
    {
        lock (_sync) return _events.Any(item => item.EventName == eventName);
    }

    public void Write(ApplicationLogLevel level, string eventName, string message, Exception? exception = null,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        lock (_sync) _events.Add((level, eventName));
    }
}

sealed class RecordingNotificationService(FocusLibraryStore store) : IAutomationNotificationService
{
    private int _count;
    public int Count => Volatile.Read(ref _count);
    public bool ShouldThrow { get; set; }
    public string? PersistedPhaseAtNotification { get; private set; }

    public async Task ShowAsync(AutomationNotification notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var active = await store.GetAsync("focus-sessions", "sessions", "current", cancellationToken);
        PersistedPhaseAtNotification = active?.Data.GetProperty("session").GetProperty("phase").GetString();
        store.Events.Add("notification");
        Interlocked.Increment(ref _count);
        if (ShouldThrow) throw new InvalidOperationException("Toast unavailable.");
    }
}

sealed class ManualTimeProvider(DateTimeOffset initialTime) : TimeProvider
{
    private readonly object _sync = new();
    private readonly List<ManualTimer> _timers = [];
    private DateTimeOffset _utcNow = initialTime;
    private long _timestamp = initialTime.UtcTicks;

    public override DateTimeOffset GetUtcNow() { lock (_sync) return _utcNow; }
    public override long GetTimestamp() { lock (_sync) return _timestamp; }
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        lock (_sync) _timers.Add(timer);
        timer.Change(dueTime, period);
        return timer;
    }

    public async Task WaitForTimerAsync() => await Check.EventuallyAsync(() =>
    {
        lock (_sync) return _timers.Any(timer => timer.IsScheduled);
    });

    public void Advance(TimeSpan amount)
    {
        if (amount < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(amount));
        DateTimeOffset target;
        lock (_sync) target = _utcNow + amount;
        while (true)
        {
            ManualTimer? due;
            lock (_sync)
            {
                due = _timers.Where(timer => timer.IsDueBy(target)).OrderBy(timer => timer.DueUtc).FirstOrDefault();
                if (due is null)
                {
                    _timestamp += (target - _utcNow).Ticks;
                    _utcNow = target;
                    return;
                }
                _timestamp += (due.DueUtc - _utcNow).Ticks;
                _utcNow = due.DueUtc;
            }
            due.Fire();
        }
    }

    private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
    {
        private readonly object _sync = new();
        private bool _disposed;
        private DateTimeOffset? _dueUtc;
        private TimeSpan _period = Timeout.InfiniteTimeSpan;

        public DateTimeOffset DueUtc { get { lock (_sync) return _dueUtc ?? DateTimeOffset.MaxValue; } }
        public bool IsScheduled { get { lock (_sync) return !_disposed && _dueUtc is not null; } }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            lock (_sync)
            {
                if (_disposed) return false;
                var now = owner.GetUtcNow();
                _dueUtc = IsInfinite(dueTime) ? null : now + NormalizeDueTime(dueTime);
                _period = IsInfinite(period) ? Timeout.InfiniteTimeSpan : NormalizeDueTime(period);
                return true;
            }
        }

        public void Dispose()
        {
            lock (_sync) { _disposed = true; _dueUtc = null; }
            owner.Remove(this);
        }

        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }

        public bool IsDueBy(DateTimeOffset target) { lock (_sync) return !_disposed && _dueUtc is { } due && due <= target; }

        public void Fire()
        {
            lock (_sync)
            {
                if (_disposed || _dueUtc is null) return;
                _dueUtc = _period == Timeout.InfiniteTimeSpan ? null : owner.GetUtcNow() + _period;
            }
            callback(state);
        }

        private static bool IsInfinite(TimeSpan duration) => duration == Timeout.InfiniteTimeSpan;
        private static TimeSpan NormalizeDueTime(TimeSpan duration) => duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
    }

    private void Remove(ManualTimer timer) { lock (_sync) _timers.Remove(timer); }
}

static class Check
{
    public static void True(bool condition, string? message = null)
    {
        if (!condition) throw new InvalidOperationException(message ?? "Expected true.");
    }

    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
    }

    public static async Task EventuallyAsync(Func<Task<bool>> condition)
    {
        var until = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (DateTime.UtcNow < until)
        {
            if (await condition()) return;
            await Task.Delay(5);
        }
        throw new InvalidOperationException("Condition was not reached before timeout.");
    }

    public static async Task EventuallyAsync(Func<bool> condition)
    {
        var until = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (DateTime.UtcNow < until)
        {
            if (condition()) return;
            await Task.Delay(5);
        }
        throw new InvalidOperationException("Condition was not reached before timeout.");
    }

    public static async Task ThrowsAsync<TException>(Func<Task> action) where TException : Exception
    {
        try { await action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }
}
