using System.Text.Json;
using Automator.Application.Automation;
using Automator.Core.Plugins;

var tests = new (string Name, Func<Task> Run)[] {
    ("schedule recurrence resolves intervals and local daylight-saving gaps", CalculatesRecurrences),
    ("scheduler coordinator persists runs and prevents duplicate concurrent executions", CoordinatorPersistsAndSerializesRuns),
    ("scheduler startup failure can be retried and invalid records cannot poison startup", StartupCanRetry),
    ("scheduler recovery never rewinds occurrence cursor or advances it for manual claims", RecoveryPreservesCursor),
    ("scheduler restart catches up once only for schedules with durable prior cursors", CatchupRunsOnce),
    ("scheduler edits rebaseline cursors and retain unchanged interval anchors", UpdatesRebaselineCursor),
    ("scheduler survives tick failure and keeps independent schedules progressing", WorkerSurvivesFailureAndSlowRuns),
    ("scheduler shutdown cancels and drains manual workflow execution", ShutdownCancelsManualRun),
    ("scheduler retains a persisted claim after cursor failure and never replays that occurrence", ClaimFailureDoesNotReplay),
    ("scheduler runs saved Script Runner profiles through the host profile executor", RunsSavedScriptProfile),
    ("scheduler reads legacy workflow-only schedule records", MigratesLegacyWorkflowSchedule),
    ("scheduler actions invoke only the host coordinator", ControlsHostCoordinator),
    ("scheduler rejects invalid and mixed recurrence fields before saving", RejectsInvalidRecurrence),
    ("scheduler snapshot contains safe history and tolerates absent optional catalogs", ReadsSafeSnapshot),
    ("scheduler propagates cancellation and hides host exception contents", HandlesFailureAndCancellation),
};
var failures = 0;
foreach (var (name, run) in tests) { try { await run(); Console.WriteLine($"PASS {name}"); } catch (Exception exception) { failures++; Console.WriteLine($"FAIL {name}: {exception.Message}"); } }
Console.WriteLine($"{tests.Length - failures}/{tests.Length} Scheduler specifications passed.");
return failures == 0 ? 0 : 1;

static JsonElement Json(string value) { using var doc = JsonDocument.Parse(value); return doc.RootElement.Clone(); }
static Task CalculatesRecurrences() {
    var interval = new AutomationScheduleDefinition("interval", "Interval", "workflow", true,
        new AutomationScheduleRecurrence(AutomationScheduleRecurrenceKind.Interval, 30, IntervalAnchorUtc: DateTimeOffset.Parse("2026-01-01T00:00:00Z")), false);
    var nextInterval = AutomationScheduleRecurrenceCalculator.NextOccurrenceUtc(interval, DateTimeOffset.Parse("2026-01-01T01:01:00Z"), TimeZoneInfo.Utc);
    Equal(DateTimeOffset.Parse("2026-01-01T01:30:00Z"), nextInterval);

    var daily = new AutomationScheduleDefinition("daily", "Daily", "workflow", true,
        new AutomationScheduleRecurrence(AutomationScheduleRecurrenceKind.Daily, LocalTime: new TimeOnly(2, 30)), false);
    var eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
    var after = DateTimeOffset.Parse("2025-03-09T06:45:00Z");
    var nextDaily = AutomationScheduleRecurrenceCalculator.NextOccurrenceUtc(daily, after, eastern);
    Equal(DateTimeOffset.Parse("2025-03-09T07:00:00Z"), nextDaily);
    var dailyWithSeconds = daily with { Recurrence = daily.Recurrence with { LocalTime = new TimeOnly(2, 30, 45) } };
    Equal(DateTimeOffset.Parse("2025-03-09T07:00:00Z"), AutomationScheduleRecurrenceCalculator.NextOccurrenceUtc(dailyWithSeconds, after, eastern));
    var repeated = daily with { Recurrence = daily.Recurrence with { LocalTime = new TimeOnly(1, 30) } };
    Equal(DateTimeOffset.Parse("2025-11-02T05:30:00Z"), AutomationScheduleRecurrenceCalculator.LatestOccurrenceUtc(repeated, DateTimeOffset.Parse("2025-11-02T06:45:00Z"), eastern));
    Equal(DateTimeOffset.Parse("2025-11-03T06:30:00Z"), AutomationScheduleRecurrenceCalculator.NextOccurrenceUtc(repeated, DateTimeOffset.Parse("2025-11-02T05:31:00Z"), eastern));
    return Task.CompletedTask;
}
static async Task CoordinatorPersistsAndSerializesRuns() {
    var store = new TestLibraryStore();
    await store.UpsertAsync(new AutomationLibraryRecord("workflows", "profiles", "workflow", 1,
        Json("""{"id":"workflow","name":"Weekly report","steps":[]}"""), DateTimeOffset.UtcNow), default);
    var time = new TestTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
    var executor = new BlockingWorkflowExecutor();
    await using var coordinator = new AutomationSchedulerCoordinator(store, executor, time, Automator.Application.Logging.NullApplicationLog.Instance);
    await coordinator.StartAsync(default);
    var schedule = SchedulerModule.NormalizeAndValidate(new AutomationScheduleDefinition("report", "Report", "workflow", true,
        new AutomationScheduleRecurrence(AutomationScheduleRecurrenceKind.Interval, 60), false));
    await coordinator.SaveAsync(schedule, default);
    var firstRun = coordinator.RunNowAsync("report", default);
    await executor.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
    await ThrowsAsync<InvalidOperationException>(() => coordinator.RunNowAsync("report", default));
    var claims = await store.ListAsync("scheduler", "occurrence-claims", default);
    Equal(1, claims.Count);
    executor.Release.TrySetResult();
    var history = await firstRun;
    Equal(AutomationStatus.Success, history.Status);
    Equal("completed", history.Category);
    var snapshot = await coordinator.GetSnapshotAsync(default);
    Equal(true, snapshot.Running);
    Equal("workflow", snapshot.Workflows!.Single().ProfileId);
    Equal(false, snapshot.States!.Single().Running);
    Equal(history.Id, snapshot.History.Single().Id);
}
static async Task RunsSavedScriptProfile() {
    var store = new TestLibraryStore();
    var handler = new SavedScriptHandler();
    var profiles = new AutomationSavedProfileExecutor([handler]);
    var time = new TestTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
    await using var coordinator = new AutomationSchedulerCoordinator(store, new CountingWorkflowExecutor(), time,
        savedProfiles: profiles);
    await coordinator.StartAsync(default);
    var snapshot = await coordinator.GetSnapshotAsync(default);
    Equal("script-job", snapshot.Scripts!.Single().ProfileId);
    var schedule = new AutomationScheduleDefinition("script-schedule", "Script schedule", "", true,
        new AutomationScheduleRecurrence(AutomationScheduleRecurrenceKind.Interval, 30), false,
        AutomationScheduleTargetKind.ScriptProfile, "script-job");
    await coordinator.SaveAsync(schedule, default);
    var run = await coordinator.RunNowAsync(schedule.Id, default);
    Equal(AutomationStatus.Success, run.Status);
    Equal(1, handler.Executions);
    Equal(AutomationExecutionOrigin.Scheduled, handler.LastMetadata!.Origin);
    Equal("script-job", (await coordinator.GetSnapshotAsync(default)).Schedules.Single().EffectiveProfileId);
}
static async Task MigratesLegacyWorkflowSchedule() {
    var store = new TestLibraryStore();
    await SeedWorkflow(store);
    await store.UpsertAsync(new AutomationLibraryRecord("scheduler", "profiles", "legacy", 1,
        Json("""{"id":"legacy","name":"Legacy","workflowId":"workflow","enabled":false,"runOnceAfterRestart":false,"recurrence":{"kind":"interval","intervalMinutes":60,"intervalAnchorUtc":"2026-01-01T00:00:00Z"}}"""), DateTimeOffset.UtcNow), default);
    await using var coordinator = new AutomationSchedulerCoordinator(store, new CountingWorkflowExecutor(),
        new TestTimeProvider(DateTimeOffset.Parse("2026-01-01T00:10:00Z")));
    await coordinator.StartAsync(default);
    var schedule = (await coordinator.GetSnapshotAsync(default)).Schedules.Single();
    Equal(AutomationScheduleTargetKind.Workflow, schedule.TargetKind);
    Equal("workflow", schedule.EffectiveProfileId);
}
static async Task SeedWorkflow(TestLibraryStore store, string id = "workflow") => await store.UpsertAsync(new AutomationLibraryRecord("workflows", "profiles", id, 1,
    Json("{\"id\":\"" + id + "\",\"name\":\"Report\",\"steps\":[]}"), DateTimeOffset.UtcNow), default);
static AutomationScheduleDefinition IntervalSchedule(string id, string workflowId = "workflow", bool enabled = true, bool catchup = false) =>
    new(id, id, workflowId, enabled, new AutomationScheduleRecurrence(AutomationScheduleRecurrenceKind.Interval, 60,
        IntervalAnchorUtc: DateTimeOffset.Parse("2026-01-01T00:00:00Z")), catchup);
static async Task SeedSchedule(TestLibraryStore store, AutomationScheduleDefinition schedule) =>
    await store.UpsertAsync(new AutomationLibraryRecord("scheduler", "profiles", schedule.Id, 1,
        JsonSerializer.SerializeToElement(schedule, new JsonSerializerOptions(JsonSerializerDefaults.Web)), DateTimeOffset.UtcNow), default);
static async Task SeedCursor(TestLibraryStore store, string id, DateTimeOffset occurrence) =>
    await store.UpsertAsync(new AutomationLibraryRecord("scheduler", "occurrence-cursors", id, 1,
        JsonSerializer.SerializeToElement(new { lastOccurrenceUtc = occurrence }), DateTimeOffset.UtcNow), default);
static async Task<DateTimeOffset?> Cursor(TestLibraryStore store, string id) {
    var record = await store.GetAsync("scheduler", "occurrence-cursors", id, default);
    return record?.Data.GetProperty("lastOccurrenceUtc").GetDateTimeOffset();
}
static async Task Eventually(Func<bool> condition) {
    var until = DateTime.UtcNow.AddSeconds(5);
    while (!condition()) { if (DateTime.UtcNow > until) throw new Exception("Timed out waiting for scheduler progress."); await Task.Delay(20); }
}
static async Task StartupCanRetry() {
    var store = new TestLibraryStore(); await SeedWorkflow(store);
    var time = new TestTimeProvider(DateTimeOffset.Parse("2026-01-01T03:00:00Z"));
    await using var coordinator = new AutomationSchedulerCoordinator(store, new CountingWorkflowExecutor(), time);
    using var canceled = new CancellationTokenSource(); canceled.Cancel();
    await ThrowsAsync<OperationCanceledException>(() => coordinator.StartAsync(canceled.Token));
    store.FailListCollectionOnce = "occurrence-claims";
    await ThrowsAsync<IOException>(() => coordinator.StartAsync(default));
    await ThrowsAsync<InvalidOperationException>(() => coordinator.GetSnapshotAsync(default));
    await store.UpsertAsync(new AutomationLibraryRecord("scheduler", "profiles", "invalid", 1,
        Json("""{"id":"invalid","name":"Broken","workflowId":"workflow","enabled":true,"recurrence":{"kind":"interval","intervalMinutes":0}}"""), time.GetUtcNow()), default);
    await coordinator.StartAsync(default);
    Equal(true, (await coordinator.GetSnapshotAsync(default)).Running);
    Equal(0, (await coordinator.GetSnapshotAsync(default)).Schedules.Count);
}
static async Task RecoveryPreservesCursor() {
    var store = new TestLibraryStore(); await SeedWorkflow(store);
    var time = new TestTimeProvider(DateTimeOffset.Parse("2026-01-01T03:00:00Z"));
    await SeedSchedule(store, IntervalSchedule("report", enabled: false));
    var latest = time.GetUtcNow().AddHours(-1); await SeedCursor(store, "report", latest);
    var older = latest.AddHours(-1); var id = $"report-{older.ToUniversalTime().Ticks:x16}";
    foreach (var (claimId, occurrence) in new[] { (id, older), ("manual-test", time.GetUtcNow().AddHours(10)) }) {
        await store.UpsertAsync(new AutomationLibraryRecord("scheduler", "occurrence-claims", claimId, 1,
            JsonSerializer.SerializeToElement(new { id = claimId, scheduleId = "report", occurrenceUtc = occurrence, claimedUtc = older }), older), default);
    }
    await using var coordinator = new AutomationSchedulerCoordinator(store, new CountingWorkflowExecutor(), time);
    await coordinator.StartAsync(default);
    Equal(latest, await Cursor(store, "report"));
    var snapshot = await coordinator.GetSnapshotAsync(default);
    Equal(2, snapshot.History.Count); Equal(true, snapshot.History.All(item => item.Category == "interrupted"));
    Equal(0, (await store.ListAsync("scheduler", "occurrence-claims", default)).Count);
}
static async Task CatchupRunsOnce() {
    var store = new TestLibraryStore(); await SeedWorkflow(store);
    var time = new TestTimeProvider(DateTimeOffset.Parse("2026-01-01T03:00:00Z"));
    foreach (var schedule in new[] { IntervalSchedule("catchup", catchup: true), IntervalSchedule("skip"), IntervalSchedule("imported", catchup: true) }) await SeedSchedule(store, schedule);
    await SeedCursor(store, "catchup", time.GetUtcNow().AddHours(-3)); await SeedCursor(store, "skip", time.GetUtcNow().AddHours(-3));
    var executor = new CountingWorkflowExecutor();
    await using (var coordinator = new AutomationSchedulerCoordinator(store, executor, time)) {
        await coordinator.StartAsync(default); await Eventually(() => executor.Count == 1);
        await Eventually(() => store.Count("scheduler", "run-history") == 1);
        Equal(time.GetUtcNow(), await Cursor(store, "skip")); Equal(time.GetUtcNow(), await Cursor(store, "imported"));
    }
    await using (var coordinator = new AutomationSchedulerCoordinator(store, executor, time)) {
        await coordinator.StartAsync(default); Equal(1, executor.Count);
        Equal(1, (await coordinator.GetSnapshotAsync(default)).History.Count);
    }
}
static async Task UpdatesRebaselineCursor() {
    var store = new TestLibraryStore(); await SeedWorkflow(store);
    var time = new TestTimeProvider(DateTimeOffset.Parse("2026-01-01T03:00:00Z"));
    await using var coordinator = new AutomationSchedulerCoordinator(store, new CountingWorkflowExecutor(), time);
    await coordinator.StartAsync(default); var original = IntervalSchedule("report"); await coordinator.SaveAsync(original, default);
    await SeedCursor(store, "report", time.GetUtcNow().AddYears(1));
    var edited = original with { Name = "Renamed", Recurrence = original.Recurrence with { IntervalAnchorUtc = null } };
    await coordinator.SaveAsync(edited, default);
    Equal(time.GetUtcNow(), await Cursor(store, "report"));
    Equal(original.Recurrence.IntervalAnchorUtc, (await coordinator.GetSnapshotAsync(default)).Schedules.Single().Recurrence.IntervalAnchorUtc);
    var future = original with { Recurrence = original.Recurrence with { IntervalAnchorUtc = time.GetUtcNow().AddHours(1) } };
    await coordinator.SaveAsync(future, default); Equal(null, await Cursor(store, "report"));
}
static async Task WorkerSurvivesFailureAndSlowRuns() {
    var store = new TestLibraryStore(); await SeedWorkflow(store); await SeedWorkflow(store, "fast");
    var time = new TestTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
    var executor = new SelectivelyBlockingExecutor();
    await using var coordinator = new AutomationSchedulerCoordinator(store, executor, time);
    await coordinator.StartAsync(default);
    await coordinator.SaveAsync(IntervalSchedule("slow"), default); await coordinator.SaveAsync(IntervalSchedule("fast", "fast"), default);
    store.FailListCollectionOnce = "profiles";
    time.Advance(TimeSpan.FromHours(1));
    await Eventually(() => executor.SlowStarted.Task.IsCompleted && executor.FastCount == 1);
    var snapshot = await coordinator.GetSnapshotAsync(default); Equal(true, snapshot.States!.Single(item => item.ScheduleId == "slow").Running);
    await ThrowsAsync<InvalidOperationException>(() => coordinator.RunNowAsync("slow", default));
    time.Advance(TimeSpan.FromHours(1)); await Eventually(() => executor.FastCount == 2);
    executor.Release.TrySetResult();
}
static async Task ShutdownCancelsManualRun() {
    var store = new TestLibraryStore(); await SeedWorkflow(store);
    var executor = new BlockingWorkflowExecutor(); var time = new TestTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
    var coordinator = new AutomationSchedulerCoordinator(store, executor, time); await coordinator.StartAsync(default);
    await coordinator.SaveAsync(IntervalSchedule("report"), default);
    var run = coordinator.RunNowAsync("report", default); await executor.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
    await coordinator.DisposeAsync(); await ThrowsAsync<OperationCanceledException>(() => run);
    Equal(0, (await store.ListAsync("scheduler", "occurrence-claims", default)).Count);
    Equal("canceled", (await store.ListAsync("scheduler", "run-history", default)).Single().Data.GetProperty("category").GetString());
}
static async Task ClaimFailureDoesNotReplay() {
    var store = new TestLibraryStore(); await SeedWorkflow(store);
    var time = new TestTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z")); var executor = new CountingWorkflowExecutor();
    var log = new RecordingSchedulerLog();
    await using (var coordinator = new AutomationSchedulerCoordinator(store, executor, time, log)) {
        await coordinator.StartAsync(default); await coordinator.SaveAsync(IntervalSchedule("report", catchup: true), default);
        store.FailUpsertCollectionOnce = "occurrence-cursors"; time.Advance(TimeSpan.FromHours(1));
        await Eventually(() => store.Count("scheduler", "occurrence-claims") == 1);
        await Eventually(() => store.GetAsync("scheduler", "occurrence-cursors", "report", default).GetAwaiter().GetResult()!.Data.GetProperty("lastOccurrenceUtc").GetDateTimeOffset() == time.GetUtcNow());
        Equal(0, executor.Count);
        time.Advance(TimeSpan.FromHours(1)); await Eventually(() => executor.Count == 1);
        await Eventually(() => store.Count("scheduler", "run-history") == 1);
    }
    await using (var recovered = new AutomationSchedulerCoordinator(store, executor, time, log)) {
        await recovered.StartAsync(default); Equal(1, executor.Count); Equal(time.GetUtcNow(), await Cursor(store, "report"));
        var snapshot = await recovered.GetSnapshotAsync(default); Equal(1, snapshot.History.Count(item => item.Category == "interrupted"));
    }
    Equal(false, log.Messages.Any(message => message.Contains("private-store-content")));
    Equal(true, log.Messages.Any(message => message.Contains("Scheduler.OccurrenceFailed")));
}
static async Task ThrowsAsync<TException>(Func<Task> action) where TException : Exception {
    try { await action(); } catch (TException) { return; }
    throw new Exception($"Expected {typeof(TException).Name}.");
}
static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}."); }
static AutomationServicesContext Context(FakeCoordinator coordinator) => new AutomationCapabilityRegistry(schedulerCoordinatorFactory: _ => coordinator)
    .CreateContext(new AutomationModuleDescriptor("scheduler", new SchedulerModule().Definition.Capabilities));

static async Task ControlsHostCoordinator() {
    var module = new SchedulerModule(); var host = new FakeCoordinator(); await using var context = Context(host);
    Equal(6, module.Definition.Slot); Equal(1, module.Definition.Capabilities.Count);
    var saved = await module.ExecuteAsync("saveSchedule", Json("""{"id":"report","name":" Report ","workflowId":"workflow","enabled":true,"runOnceAfterRestart":false,"recurrence":{"kind":"interval","intervalMinutes":30}}"""), Json("{}"), context, default);
    Equal(AutomationStatus.Success, saved.Status); Equal("Report", host.Saved!.Name);
    var enabled = await module.ExecuteAsync("setEnabled", Json("""{"scheduleId":"report","enabled":false}"""), Json("{}"), context, default);
    Equal(AutomationStatus.Success, enabled.Status); Equal(false, host.Enabled);
    var result = await module.ExecuteAsync("runNow", Json("""{"scheduleId":"report","workflowId":"injected","input":{"secret":"not-forwarded"}}"""), Json("{}"), context, default);
    Equal(AutomationStatus.Success, result.Status); Equal("report", host.Ran);
    var deleted = await module.ExecuteAsync("deleteSchedule", Json("""{"scheduleId":"report"}"""), Json("{}"), context, default);
    Equal(true, deleted.Data.GetProperty("deleted").GetBoolean());
}
static async Task RejectsInvalidRecurrence() {
    var module = new SchedulerModule(); var host = new FakeCoordinator(); await using var context = Context(host);
    foreach (var recurrence in new[] { """{"kind":"interval","intervalMinutes":0}""", """{"kind":"daily","localTime":"09:00:00","intervalMinutes":30}""", """{"kind":"weekly","localTime":"09:00:00","daysOfWeek":["monday","monday"]}""", """{"kind":123,"intervalMinutes":30}""" }) {
        var input = Json("{\"id\":\"report\",\"name\":\"Report\",\"workflowId\":\"workflow\",\"enabled\":true,\"runOnceAfterRestart\":false,\"recurrence\":" + recurrence + "}");
        var result = await module.ExecuteAsync("saveSchedule", input, Json("{}"), context, default); Equal(AutomationStatus.Error, result.Status);
    }
    Equal(null, host.Saved);
    var badBoolean = await module.ExecuteAsync("setEnabled", Json("""{"scheduleId":"report","enabled":"true"}"""), Json("{}"), context, default); Equal(AutomationStatus.Error, badBoolean.Status);
}
static async Task ReadsSafeSnapshot() {
    var host = new FakeCoordinator(); await using var context = Context(host);
    var result = await new SchedulerModule().ExecuteAsync("getSnapshot", Json("{}"), Json("{}"), context, default);
    Equal(AutomationStatus.Success, result.Status); Equal(0, result.Data.GetProperty("states").GetArrayLength()); Equal(0, result.Data.GetProperty("workflows").GetArrayLength());
    var history = result.Data.GetProperty("history")[0]; Equal("completed", history.GetProperty("category").GetString());
    Equal(false, history.TryGetProperty("output", out _)); Equal(false, history.TryGetProperty("stdout", out _));
}
static async Task HandlesFailureAndCancellation() {
    var host = new FakeCoordinator { Failure = new IOException("secret-body-do-not-display") }; await using var context = Context(host);
    var result = await new SchedulerModule().ExecuteAsync("runNow", Json("""{"scheduleId":"report"}"""), Json("{}"), context, default);
    Equal(AutomationStatus.Error, result.Status); Equal(false, result.Message.Contains("secret-body"));
    host.Failure = new OperationCanceledException();
    try { await new SchedulerModule().ExecuteAsync("runNow", Json("""{"scheduleId":"report"}"""), Json("{}"), context, default); throw new Exception("Cancellation swallowed."); } catch (OperationCanceledException) { }
}
sealed class FakeCoordinator : IAutomationSchedulerCoordinator {
    public AutomationScheduleDefinition? Saved; public bool Enabled; public string? Ran; public Exception? Failure;
    private static AutomationScheduleHistoryEntry Entry => new("run-1", "report", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, AutomationStatus.Success, "completed", 12);
    public Task<AutomationSchedulerSnapshot> GetSnapshotAsync(CancellationToken ct) => Task.FromResult(new AutomationSchedulerSnapshot([], [Entry], true));
    public Task SaveAsync(AutomationScheduleDefinition schedule, CancellationToken ct) { Saved = schedule; return Task.CompletedTask; }
    public Task<bool> DeleteAsync(string id, CancellationToken ct) => Task.FromResult(true);
    public Task SetEnabledAsync(string id, bool enabled, CancellationToken ct) { Enabled = enabled; return Task.CompletedTask; }
    public Task<AutomationScheduleHistoryEntry> RunNowAsync(string id, CancellationToken ct) { if (Failure is not null) throw Failure; Ran = id; return Task.FromResult(Entry); }
}
sealed class TestLibraryStore : IAutomationLibraryStore {
    private readonly Dictionary<(string Module, string Collection, string Id), AutomationLibraryRecord> _records = [];
    public string? FailListCollectionOnce;
    public string? FailUpsertCollectionOnce;
    public int Count(string module, string collection) { lock (_records) return _records.Values.Count(record => record.ModuleId == module && record.Collection == collection); }
    public Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(string module, string collection, CancellationToken ct) {
        lock (_records) {
            if (collection == FailListCollectionOnce) { FailListCollectionOnce = null; throw new IOException("private-store-content"); }
            return Task.FromResult<IReadOnlyList<AutomationLibraryRecord>>(_records.Values.Where(record => record.ModuleId == module && record.Collection == collection).ToArray());
        }
    }
    public Task<AutomationLibraryRecord?> GetAsync(string module, string collection, string id, CancellationToken ct) {
        lock (_records) { _records.TryGetValue((module, collection, id), out var record); return Task.FromResult(record); }
    }
    public Task UpsertAsync(AutomationLibraryRecord record, CancellationToken ct) {
        lock (_records) {
            if (record.Collection == FailUpsertCollectionOnce) { FailUpsertCollectionOnce = null; throw new IOException("private-store-content"); }
            _records[(record.ModuleId, record.Collection, record.Id)] = record with { Data = record.Data.Clone() }; return Task.CompletedTask;
        }
    }
    public Task<bool> DeleteAsync(string module, string collection, string id, CancellationToken ct) {
        lock (_records) return Task.FromResult(_records.Remove((module, collection, id)));
    }
}
sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider {
    private DateTimeOffset _now = now;
    public override DateTimeOffset GetUtcNow() => _now;
    public void Advance(TimeSpan duration) => _now += duration;
}
sealed class SavedScriptHandler : IAutomationSavedProfileHandler {
    public string ModuleId => "script-runner";
    public int Executions { get; private set; }
    public AutomationExecutionMetadata? LastMetadata { get; private set; }
    public Task<IReadOnlyList<AutomationSavedProfileSummary>> ListProfilesAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<AutomationSavedProfileSummary>>([new("script-job", "Nightly script")]);
    public Task<AutomationProfileExecutionOutput> ExecuteAsync(string profileId, JsonElement? input,
        AutomationExecutionMetadata metadata, CancellationToken ct) {
        if (profileId != "script-job") throw new InvalidOperationException("Unexpected saved script profile.");
        Executions++;
        LastMetadata = metadata;
        using var output = JsonDocument.Parse("{}");
        return Task.FromResult(new AutomationProfileExecutionOutput(output.RootElement.Clone(), new AutomationExecutionSummary(AutomationStatus.Success, "completed", 1)));
    }
}
sealed class BlockingWorkflowExecutor : IAutomationScheduledWorkflowExecutor {
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public async Task<AutomationExecutionSummary> RunScheduledAsync(string workflowId, string correlationId, CancellationToken ct) {
        Started.TrySetResult(); await Release.Task.WaitAsync(ct); return new AutomationExecutionSummary(AutomationStatus.Success, "completed", 15);
    }
}
sealed class CountingWorkflowExecutor : IAutomationScheduledWorkflowExecutor {
    private int _count; public int Count => Volatile.Read(ref _count);
    public Task<AutomationExecutionSummary> RunScheduledAsync(string id, string correlation, CancellationToken ct) {
        Interlocked.Increment(ref _count); return Task.FromResult(new AutomationExecutionSummary(AutomationStatus.Success, "completed", 1));
    }
}
sealed class SelectivelyBlockingExecutor : IAutomationScheduledWorkflowExecutor {
    private int _fastCount; public int FastCount => Volatile.Read(ref _fastCount);
    public TaskCompletionSource SlowStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public async Task<AutomationExecutionSummary> RunScheduledAsync(string id, string correlation, CancellationToken ct) {
        if (id == "fast") Interlocked.Increment(ref _fastCount);
        else { SlowStarted.TrySetResult(); await Release.Task.WaitAsync(ct); }
        return new AutomationExecutionSummary(AutomationStatus.Success, "completed", 1);
    }
}
sealed class RecordingSchedulerLog : Automator.Application.Logging.IApplicationLog {
    public System.Collections.Concurrent.ConcurrentBag<string> Messages { get; } = [];
    public void Write(Automator.Application.Logging.ApplicationLogLevel level, string eventName, string message, Exception? exception = null,
        IReadOnlyDictionary<string, object?>? properties = null) => Messages.Add(eventName + " " + message + " " + exception?.Message);
}
