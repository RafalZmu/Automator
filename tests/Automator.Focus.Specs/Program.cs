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
    ("paused work time resumes accumulated time and discards by ID", WorkTimePendingCanResumeAndDiscard),
    ("saved work-time entries can be edited and deleted without losing interval timing", WorkTimeEntriesCanBeEditedAndDeleted),
    ("active work time persists across backend restart", ActiveWorkTimeSurvivesRestart),
    ("multiple named timers exclude pauses and retain independent drafts", WorkTimeMultipleTimers),
    ("only one timer runs and ending a paused timer preserves another runner", WorkTimeOnlyOneRunner),
    ("timers and independent drafts survive restart and targeted discard", WorkTimeDraftsSurviveRestart),
    ("legacy active pending and history records migrate without losing elapsed time", WorkTimeLegacyMigration),
    ("legacy pending migration preserves elapsed time without counting unknown paused gaps", WorkTimeLegacyPendingSegments),
    ("resumed legacy timer preserves elapsed without fabricating paused segments", WorkTimeLegacyResumedSegments),
    ("work time state writes are atomic and cancellation leaves state intact", WorkTimeFailedWritesPreserveState),
    ("work time history is bounded and corrupt versions are rejected", WorkTimeHistoryIsBounded),
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
    var c = lease.Coordinator;
    await Check.ThrowsAsync<InvalidDataException>(() => c.StartAsync("  ", CancellationToken.None));
    await Check.ThrowsAsync<InvalidDataException>(() => c.StartAsync(new string('x', 501), CancellationToken.None));
    var id = (await c.StartAsync(" Build release ", CancellationToken.None)).Timers.Single().Id;
    time.Advance(TimeSpan.FromMinutes(12));
    var ended = await c.EndAsync(id, CancellationToken.None);
    Check.Equal(id, ended.PendingEntries.Single().Id);
    Check.Equal("Build release", ended.PendingEntries.Single().Description);
    Check.Equal(720_000L, ended.PendingEntries.Single().DurationMilliseconds);
    await Check.ThrowsAsync<InvalidDataException>(() => c.SaveEntryAsync(id, "  ", [], CancellationToken.None));
    await Check.ThrowsAsync<InvalidDataException>(() => c.SaveEntryAsync(id, "x", [new string('x', 41)], CancellationToken.None));
    await Check.ThrowsAsync<InvalidDataException>(() => c.SaveEntryAsync(id, "x", Enumerable.Range(0,31).Select(i => i.ToString()).ToArray(), CancellationToken.None));
    var saved = await c.SaveEntryAsync(id, "Build release", [" feature ", "#deep", "feature"], CancellationToken.None);
    Check.Equal(0, saved.PendingEntries.Count);
    Check.True(saved.History.Single().Tags.SequenceEqual(["feature", "#deep"]));
    Check.Equal(720_000L, saved.History.Single().DurationMilliseconds);
    Check.Equal(2, store.LastRecord("focus-sessions", "work-time-history", id)!.SchemaVersion);
}

static async Task WorkTimePendingCanResumeAndDiscard()
{
    var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));
    var store = new FocusLibraryStore();
    await using var lease = await WorkTimeCoordinatorHarness.CreateAsync(store, time);
    var c = lease.Coordinator;
    var id = (await c.StartAsync("Task", CancellationToken.None)).Timers.Single().Id;
    time.Advance(TimeSpan.FromMinutes(20));
    await c.PauseAsync(id, CancellationToken.None);
    time.Advance(TimeSpan.FromMinutes(30));
    var resumed = await c.ResumeAsync(id, CancellationToken.None);
    Check.Equal(1_200_000L, resumed.Timers.Single().ElapsedMilliseconds);
    time.Advance(TimeSpan.FromMinutes(7));
    var paused = await c.PauseAsync(id, CancellationToken.None);
    Check.Equal(1_620_000L, paused.Timers.Single().ElapsedMilliseconds);
    var discarded = await c.DiscardTimerAsync(id, CancellationToken.None);
    Check.Equal(0, discarded.Timers.Count);
    Check.Equal(0, discarded.History.Count);
}

static async Task WorkTimeEntriesCanBeEditedAndDeleted()
{
    var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));
    var store = new FocusLibraryStore();
    await using var lease = await WorkTimeCoordinatorHarness.CreateAsync(store, time);
    var c = lease.Coordinator;
    var id = (await c.StartAsync("Original description", CancellationToken.None)).Timers.Single().Id;
    time.Advance(TimeSpan.FromSeconds(75));
    await c.EndAsync(id, CancellationToken.None);
    var original = (await c.SaveEntryAsync(id, "Original description", ["old-tag"], CancellationToken.None)).History.Single();
    var edited = (await c.UpdateEntryAsync(id, "Corrected description", ["planning", "review"], CancellationToken.None)).History.Single();
    Check.Equal("Corrected description", edited.Description);
    Check.True(edited.Tags.SequenceEqual(["planning", "review"]));
    Check.Equal(original.StartedUtc, edited.StartedUtc);
    Check.Equal(original.StoppedUtc, edited.StoppedUtc);
    Check.Equal(75_000L, edited.DurationMilliseconds);
    Check.True(edited.Segments.SequenceEqual(original.Segments));
    var deleted = await c.DeleteEntryAsync(id, CancellationToken.None);
    Check.Equal(0, deleted.History.Count);
    Check.Equal(null, store.LastRecord("focus-sessions", "work-time-history", id));
    await Check.ThrowsAsync<InvalidOperationException>(() => c.DeleteEntryAsync(id, CancellationToken.None));
}

static async Task ActiveWorkTimeSurvivesRestart()
{
    var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));
    var store = new FocusLibraryStore();
    string id;
    await using (var first = await WorkTimeCoordinatorHarness.CreateAsync(store, time))
    {
        id = (await first.Coordinator.StartAsync("Task", CancellationToken.None)).Timers.Single().Id;
        time.Advance(TimeSpan.FromMinutes(2));
    }
    await using var restarted = await WorkTimeCoordinatorHarness.CreateAsync(store, time);
    Check.Equal(120_000L, (await restarted.Coordinator.GetSnapshotAsync(CancellationToken.None)).Timers.Single().ElapsedMilliseconds);
    time.Advance(TimeSpan.FromMinutes(3));
    Check.Equal(300_000L, (await restarted.Coordinator.EndAsync(id, CancellationToken.None)).PendingEntries.Single().DurationMilliseconds);
}

static async Task WorkTimeOnlyOneRunner()
{
    var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 3, 8, 0, 0, TimeSpan.Zero));
    var store = new FocusLibraryStore();
    await using var lease = await WorkTimeCoordinatorHarness.CreateAsync(store, time);
    var c = lease.Coordinator;
    var a = (await c.StartAsync("A", CancellationToken.None)).Timers.Single().Id;
    await Check.ThrowsAsync<InvalidOperationException>(() => c.StartAsync("B", CancellationToken.None));
    await Check.ThrowsAsync<InvalidOperationException>(() => c.DiscardTimerAsync(a, CancellationToken.None));
    time.Advance(TimeSpan.FromMinutes(5));
    await c.PauseAsync(a, CancellationToken.None);
    var b = (await c.StartAsync("B", CancellationToken.None)).Timers.Last().Id;
    await Check.ThrowsAsync<InvalidOperationException>(() => c.ResumeAsync(a, CancellationToken.None));
    await Check.ThrowsAsync<InvalidOperationException>(() => c.ResumeAsync(b, CancellationToken.None));
    time.Advance(TimeSpan.FromMinutes(10));
    var endedA = await c.EndAsync(a, CancellationToken.None);
    Check.Equal(b, endedA.Timers.Single().Id);
    Check.Equal("running", endedA.Timers.Single().Status);
    Check.Equal(600_000L, endedA.Timers.Single().ElapsedMilliseconds);
    Check.Equal(300_000L, endedA.PendingEntries.Single().DurationMilliseconds);
    Check.Equal(time.GetUtcNow().AddMinutes(-10), endedA.PendingEntries.Single().StoppedUtc);
    await Check.ThrowsAsync<InvalidOperationException>(() => c.PauseAsync(a, CancellationToken.None));
    await Check.ThrowsAsync<InvalidOperationException>(() => c.DiscardTimerAsync("missing", CancellationToken.None));
    await c.SaveEntryAsync(a, "A", [], CancellationToken.None);
    Check.Equal(b, (await c.GetSnapshotAsync(CancellationToken.None)).Timers.Single().Id);
}

static async Task WorkTimeDraftsSurviveRestart()
{
    var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 3, 8, 0, 0, TimeSpan.Zero));
    var store = new FocusLibraryStore();
    string a, b, paused, running;
    await using (var lease = await WorkTimeCoordinatorHarness.CreateAsync(store, time))
    {
        var c = lease.Coordinator;
        a = (await c.StartAsync("A", CancellationToken.None)).Timers.Single().Id;
        time.Advance(TimeSpan.FromSeconds(3));
        await c.EndAsync(a, CancellationToken.None);
        b = (await c.StartAsync("B", CancellationToken.None)).Timers.Single().Id;
        time.Advance(TimeSpan.FromSeconds(5));
        await c.EndAsync(b, CancellationToken.None);
        paused = (await c.StartAsync("Paused", CancellationToken.None)).Timers.Single().Id;
        time.Advance(TimeSpan.FromSeconds(2));
        await c.PauseAsync(paused, CancellationToken.None);
        running = (await c.StartAsync("Running", CancellationToken.None)).Timers.Last().Id;
    }
    time.Advance(TimeSpan.FromMinutes(20));
    await using var restarted = await WorkTimeCoordinatorHarness.CreateAsync(store, time);
    var restored = await restarted.Coordinator.GetSnapshotAsync(CancellationToken.None);
    Check.Equal(2, restored.PendingEntries.Count);
    Check.Equal(2_000L, restored.Timers.First(t => t.Id == paused).ElapsedMilliseconds);
    Check.Equal(1_200_000L, restored.Timers.First(t => t.Id == running).ElapsedMilliseconds);
    var discardedTimer = await restarted.Coordinator.DiscardTimerAsync(paused, CancellationToken.None);
    Check.Equal(running, discardedTimer.Timers.Single().Id);
    Check.Equal(2, discardedTimer.PendingEntries.Count);
    var discardedDraft = await restarted.Coordinator.DiscardPendingAsync(a, CancellationToken.None);
    Check.Equal(b, discardedDraft.PendingEntries.Single().Id);
    Check.Equal(running, discardedDraft.Timers.Single().Id);
    await restarted.Coordinator.SaveEntryAsync(b, "Renamed B", [], CancellationToken.None);
    await Check.ThrowsAsync<InvalidOperationException>(() => restarted.Coordinator.DiscardPendingAsync(b, CancellationToken.None));
}

static async Task WorkTimeLegacyMigration()
{
    var origin = new DateTimeOffset(2026, 10, 3, 8, 0, 0, TimeSpan.Zero);
    var time = new ManualTimeProvider(origin.AddMinutes(30));
    async Task Seed(FocusLibraryStore store, string collection, string id, object data, int version = 1) =>
        await store.UpsertAsync(new AutomationLibraryRecord("focus-sessions", collection, id, version,
            JsonSerializer.SerializeToElement(data), origin), CancellationToken.None);
    var activeStore = new FocusLibraryStore();
    await Seed(activeStore, "work-time", "current", new { active = new { id = "legacy-active", startedUtc = origin,
        accumulatedMilliseconds = 120_000L, runningSinceUtc = origin.AddMinutes(20) }, pending = (object?)null });
    await Seed(activeStore, "work-time-history", "legacy-history", new { id = "legacy-history", startedUtc = origin,
        stoppedUtc = origin.AddMinutes(10), durationMilliseconds = 420_000L, description = "Saved", tags = new[] { "legacy" } });
    await using (var lease = await WorkTimeCoordinatorHarness.CreateAsync(activeStore, time))
    {
        var snapshot = await lease.Coordinator.GetSnapshotAsync(CancellationToken.None);
        var timer = snapshot.Timers.Single();
        Check.Equal("legacy-active", timer.Id);
        Check.Equal("Untitled timer", timer.Description);
        Check.Equal("running", timer.Status);
        Check.Equal(720_000L, timer.ElapsedMilliseconds);
        Check.Equal(420_000L, snapshot.History.Single().DurationMilliseconds);
        Check.Equal(origin, snapshot.History.Single().Segments.Single().StartedUtc);
        Check.Equal(origin.AddMinutes(10), snapshot.History.Single().Segments.Single().StoppedUtc);
        await lease.Coordinator.EndAsync(timer.Id, CancellationToken.None);
        var saved = await lease.Coordinator.SaveEntryAsync(timer.Id, "Renamed legacy", [], CancellationToken.None);
        Check.Equal("Renamed legacy", saved.History.Last().Description);
    }
    var pendingStore = new FocusLibraryStore();
    await Seed(pendingStore, "work-time", "current", new { active = (object?)null, pending = new { id = "legacy-pending",
        startedUtc = origin, stoppedUtc = origin.AddMinutes(10), durationMilliseconds = 123_456L, description = "", tags = Array.Empty<string>() } });
    await using var pendingLease = await WorkTimeCoordinatorHarness.CreateAsync(pendingStore, time);
    var paused = (await pendingLease.Coordinator.GetSnapshotAsync(CancellationToken.None)).Timers.Single();
    Check.Equal("paused", paused.Status);
    Check.Equal("Untitled timer", paused.Description);
    Check.Equal(123_456L, paused.ElapsedMilliseconds);
    Check.Equal(2, pendingStore.LastRecord("focus-sessions", "work-time", "current")!.SchemaVersion);
    await pendingLease.Coordinator.ResumeAsync(paused.Id, CancellationToken.None);
    time.Advance(TimeSpan.FromSeconds(2));
    Check.Equal(125_456L, (await pendingLease.Coordinator.EndAsync(paused.Id, CancellationToken.None)).PendingEntries.Single().DurationMilliseconds);
}

static async Task WorkTimeLegacyResumedSegments()
{
    var originalStart = new DateTimeOffset(2026, 10, 3, 9, 0, 0, TimeSpan.Zero);
    var resumedAt = originalStart.AddHours(6);
    var time = new ManualTimeProvider(resumedAt);
    var store = new FocusLibraryStore();
    await store.UpsertAsync(new AutomationLibraryRecord("focus-sessions", "work-time", "current", 1,
        JsonSerializer.SerializeToElement(new { active = new { id = "resumed-legacy", startedUtc = originalStart,
            accumulatedMilliseconds = 3_600_000L, runningSinceUtc = resumedAt }, pending = (object?)null }), resumedAt), CancellationToken.None);
    await using var lease = await WorkTimeCoordinatorHarness.CreateAsync(store, time);
    var c = lease.Coordinator;
    var migrated = (await c.GetSnapshotAsync(CancellationToken.None)).Timers.Single();
    Check.Equal(originalStart, migrated.StartedUtc);
    Check.Equal(3_600_000L, migrated.ElapsedMilliseconds);
    Check.Equal(1, migrated.Segments.Count);
    Check.Equal(resumedAt, migrated.Segments.Single().StartedUtc);
    Check.Equal(null, migrated.Segments.Single().StoppedUtc);
    time.Advance(TimeSpan.FromMinutes(30));
    var ended = (await c.EndAsync(migrated.Id, CancellationToken.None)).PendingEntries.Single();
    Check.Equal(5_400_000L, ended.DurationMilliseconds);
    Check.Equal(1, ended.Segments.Count);
    Check.Equal(resumedAt, ended.Segments.Single().StartedUtc);
    Check.Equal(resumedAt.AddMinutes(30), ended.Segments.Single().StoppedUtc);
    var saved = (await c.SaveEntryAsync(migrated.Id, "Recovered work", [], CancellationToken.None)).History.Single();
    Check.Equal(5_400_000L, saved.DurationMilliseconds);
    Check.Equal(1, saved.Segments.Count);
    Check.Equal(resumedAt, saved.Segments.Single().StartedUtc);
    Check.Equal(resumedAt.AddMinutes(30), saved.Segments.Single().StoppedUtc);
    Check.Equal(1_800_000L, (long)(saved.Segments.Single().StoppedUtc!.Value - saved.Segments.Single().StartedUtc).TotalMilliseconds);
}

static async Task WorkTimeLegacyPendingSegments()
{
    var started = new DateTimeOffset(2026, 10, 3, 8, 0, 0, TimeSpan.Zero);
    var stopped = started.AddHours(10);
    var time = new ManualTimeProvider(stopped);
    var store = new FocusLibraryStore();
    await store.UpsertAsync(new AutomationLibraryRecord("focus-sessions", "work-time", "current", 1,
        JsonSerializer.SerializeToElement(new { active = (object?)null, pending = new { id = "legacy-pending-gap",
            startedUtc = started, stoppedUtc = stopped, durationMilliseconds = 123_456L, description = "", tags = Array.Empty<string>() } }), stopped), CancellationToken.None);

    await using var lease = await WorkTimeCoordinatorHarness.CreateAsync(store, time);
    var coordinator = lease.Coordinator;
    var paused = (await coordinator.GetSnapshotAsync(CancellationToken.None)).Timers.Single();
    Check.Equal("paused", paused.Status);
    Check.Equal(123_456L, paused.ElapsedMilliseconds);
    Check.Equal(1, paused.Segments.Count);
    Check.Equal(stopped, paused.Segments.Single().StartedUtc);
    Check.Equal(stopped, paused.Segments.Single().StoppedUtc);

    await coordinator.ResumeAsync(paused.Id, CancellationToken.None);
    time.Advance(TimeSpan.FromSeconds(2));
    var draft = (await coordinator.EndAsync(paused.Id, CancellationToken.None)).PendingEntries.Single();
    Check.Equal(125_456L, draft.DurationMilliseconds);
    Check.Equal(2, draft.Segments.Count);
    Check.Equal(stopped, draft.Segments[0].StartedUtc);
    Check.Equal(stopped, draft.Segments[0].StoppedUtc);
    Check.Equal(stopped, draft.Segments[1].StartedUtc);
    Check.Equal(stopped.AddSeconds(2), draft.Segments[1].StoppedUtc);
}

static async Task WorkTimeFailedWritesPreserveState()
{
    var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 3, 8, 0, 0, TimeSpan.Zero));
    var store = new FocusLibraryStore();
    await using var lease = await WorkTimeCoordinatorHarness.CreateAsync(store, time);
    var c = lease.Coordinator;
    var id = (await c.StartAsync("A", CancellationToken.None)).Timers.Single().Id;
    store.FailWorkTimeWrites = true;
    await Check.ThrowsAsync<IOException>(() => c.PauseAsync(id, CancellationToken.None));
    Check.Equal("running", (await c.GetSnapshotAsync(CancellationToken.None)).Timers.Single().Status);
    store.FailWorkTimeWrites = false;
    using var canceled = new CancellationTokenSource();
    canceled.Cancel();
    await Check.ThrowsAsync<OperationCanceledException>(() => c.EndAsync(id, canceled.Token));
    Check.Equal(id, (await c.GetSnapshotAsync(CancellationToken.None)).Timers.Single().Id);
    await c.EndAsync(id, CancellationToken.None);
    store.FailWorkTimeWrites = true;
    await Check.ThrowsAsync<IOException>(() => c.SaveEntryAsync(id, "Saved before state failure", [], CancellationToken.None));
    store.FailWorkTimeWrites = false;
    await using var recovered = await WorkTimeCoordinatorHarness.CreateAsync(store, time);
    var repaired = await recovered.Coordinator.GetSnapshotAsync(CancellationToken.None);
    Check.Equal(id, repaired.History.Single().Id);
    Check.Equal(0, repaired.PendingEntries.Count);
}

static async Task WorkTimeHistoryIsBounded()
{
    var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 3, 8, 0, 0, TimeSpan.Zero));
    var store = new FocusLibraryStore();
    for (var i = 0; i < 501; i++)
    {
        var start = time.GetUtcNow().AddSeconds(i);
        await store.UpsertAsync(new AutomationLibraryRecord("focus-sessions", "work-time-history", $"entry-{i}", 1,
            JsonSerializer.SerializeToElement(new { id = $"entry-{i}", startedUtc = start, stoppedUtc = start.AddSeconds(1),
                durationMilliseconds = 1_000L, description = "Legacy", tags = Array.Empty<string>() }), start), CancellationToken.None);
    }
    await using var lease = await WorkTimeCoordinatorHarness.CreateAsync(store, time);
    Check.Equal(500, (await lease.Coordinator.GetSnapshotAsync(CancellationToken.None)).History.Count);
    Check.Equal(null, store.LastRecord("focus-sessions", "work-time-history", "entry-0"));
    var corrupt = new FocusLibraryStore();
    await corrupt.UpsertAsync(new AutomationLibraryRecord("focus-sessions", "work-time", "current", 99,
        JsonSerializer.SerializeToElement(new { }), time.GetUtcNow()), CancellationToken.None);
    await Check.ThrowsAsync<InvalidDataException>(async () => { await using var invalid = await WorkTimeCoordinatorHarness.CreateAsync(corrupt, time); });
}

static async Task WorkTimeMultipleTimers()
{
    var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 3, 8, 0, 0, TimeSpan.Zero));
    var store = new FocusLibraryStore();
    await using var lease = await WorkTimeCoordinatorHarness.CreateAsync(store, time);
    var capabilities = new AutomationCapabilityRegistry(workTimeCoordinatorFactory: _ => lease.Coordinator);
    var module = new FocusSessionsModule();
    await using var context = capabilities.CreateContext(new AutomationModuleDescriptor(module.Id, module.Definition.Capabilities));
    async Task<JsonElement> Act(string action, object input)
    {
        var result = await module.ExecuteAsync(action, JsonSerializer.SerializeToElement(input), module.CreateDefaultSettings(), context, CancellationToken.None);
        Check.Equal(Automator.Core.Plugins.AutomationStatus.Success, result.Status);
        return result.Data;
    }
    var start = await Act("start", new { description = "Task A" });
    Check.True(start.TryGetProperty("timers", out _), "The multi-timer snapshot must expose timers.");
    var a = start.GetProperty("timers")[0].GetProperty("id").GetString()!;
    Check.Equal("Task A", start.GetProperty("timers")[0].GetProperty("description").GetString());
    time.Advance(TimeSpan.FromMinutes(10));
    await Act("pause", new { id = a });
    var bStart = await Act("start", new { description = "Task B" });
    var b = bStart.GetProperty("timers")[1].GetProperty("id").GetString()!;
    time.Advance(TimeSpan.FromMinutes(20));
    await Act("end", new { id = b });
    await Act("saveEntry", new { id = b, description = "Task B", tags = new[] { "work" } });
    await Act("resume", new { id = a });
    time.Advance(TimeSpan.FromMinutes(5));
    var ended = await Act("end", new { id = a });
    var draft = ended.GetProperty("pendingEntries")[0];
    Check.Equal(900_000L, draft.GetProperty("durationMilliseconds").GetInt64());
    Check.Equal(2, draft.GetProperty("segments").GetArrayLength());
    Check.Equal(time.GetUtcNow().AddMinutes(-25), draft.GetProperty("segments")[0].GetProperty("stoppedUtc").GetDateTimeOffset());
    Check.Equal(time.GetUtcNow().AddMinutes(-5), draft.GetProperty("segments")[1].GetProperty("startedUtc").GetDateTimeOffset());
    var saved = await Act("saveEntry", new { id = a, description = "Task A", tags = Array.Empty<string>() });
    Check.Equal(2, saved.GetProperty("history").GetArrayLength());
    Check.Equal(0, saved.GetProperty("timers").GetArrayLength());
    Check.Equal(0, saved.GetProperty("pendingEntries").GetArrayLength());
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
    public bool FailWorkTimeWrites { get; set; }

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
        if (FailWorkTimeWrites && record.Collection == "work-time")
            throw new IOException("Simulated work time state store failure.");
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
