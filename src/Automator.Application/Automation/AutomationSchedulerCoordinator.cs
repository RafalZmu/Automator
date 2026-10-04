using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Automator.Application.Logging;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

/// <summary>
/// Backend-lifetime scheduler. Occurrences are claimed before a workflow starts; after a crash they
/// are recorded as interrupted and never replayed. This prevents automatic retries of a claimed
/// occurrence, but cannot promise exactly-once external side effects inside a workflow.
/// </summary>
public sealed class AutomationSchedulerCoordinator : IAutomationSchedulerCoordinator, IAsyncDisposable
{
    public const string ModuleId = "scheduler";
    public const string ScheduleCollection = "profiles";
    public const string HistoryCollection = "run-history";
    private const string ClaimCollection = "occurrence-claims";
    private const string CursorCollection = "occurrence-cursors";
    private const int MaximumHistoryEntries = 100;
    private static readonly Regex IdPattern = new("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private readonly IAutomationLibraryStore _library;
    private readonly IAutomationScheduledWorkflowExecutor _workflows;
    private readonly AutomationSavedProfileExecutor? _savedProfiles;
    private readonly TimeProvider _clock;
    private readonly IApplicationLog _log;
    private readonly Func<Task>? _historyChanged;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly ConcurrentDictionary<string, byte> _running = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _stateGate = new(1, 1);
    private readonly List<Task> _catchupTasks = [];
    private readonly ConcurrentDictionary<long, Task> _activeRuns = new();
    private readonly ConcurrentDictionary<long, Task> _occurrenceTasks = new();
    private long _runSequence;
    private Task? _worker;
    private DateTimeOffset _startedUtc;
    private int _started;
    private int _disposed;

    public AutomationSchedulerCoordinator(
        IAutomationLibraryStore library,
        IAutomationScheduledWorkflowExecutor workflows,
        TimeProvider? clock = null,
        IApplicationLog? log = null,
        AutomationSavedProfileExecutor? savedProfiles = null,
        Func<Task>? historyChanged = null)
    {
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _workflows = workflows ?? throw new ArgumentNullException(nameof(workflows));
        _savedProfiles = savedProfiles;
        _clock = clock ?? TimeProvider.System;
        _log = log ?? NullApplicationLog.Instance;
        _historyChanged = historyChanged;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            if (_started == 1) return;
            _startedUtc = _clock.GetUtcNow();
            var catchups = new List<(AutomationScheduleDefinition Schedule, DateTimeOffset Due)>();
            await RecoverClaimsAsync(linked.Token).ConfigureAwait(false);
            var schedules = await ReadSchedulesAsync(linked.Token).ConfigureAwait(false);
            foreach (var schedule in schedules.Where(item => item.Enabled))
            {
                var due = AutomationScheduleRecurrenceCalculator.LatestOccurrenceUtc(schedule, _startedUtc);
                if (due is null) continue;
                var cursor = await ReadCursorAsync(schedule.Id, linked.Token).ConfigureAwait(false);
                if (cursor >= due) continue;
                if (schedule.RunOnceAfterRestart && cursor is not null)
                    catchups.Add((schedule, due.Value));
                else
                {
                    // A new/imported schedule without a cursor has no durable last-observed
                    // occurrence. Establish a baseline rather than inventing a missed run.
                    await WriteCursorAsync(schedule.Id, due.Value, linked.Token).ConfigureAwait(false);
                    _log.Write(ApplicationLogLevel.Information, "Scheduler.MissedOccurrenceSkipped",
                        "A missed schedule occurrence was skipped during startup.",
                        properties: new Dictionary<string, object?> { ["scheduleId"] = schedule.Id });
                }
            }
            linked.Token.ThrowIfCancellationRequested();
            Volatile.Write(ref _started, 1);
            foreach (var (schedule, due) in catchups)
            {
                var task = SafelyProcessOccurrenceAsync(schedule, due, _lifetime.Token);
                lock (_catchupTasks) _catchupTasks.Add(task);
            }
            _worker = Task.Run(() => WorkerLoopAsync(_lifetime.Token), CancellationToken.None);
        }
        catch { Volatile.Write(ref _started, 0); throw; }
        finally { _stateGate.Release(); }
    }

    public async Task<AutomationSchedulerSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        EnsureStarted();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        cancellationToken = linked.Token;
        var schedules = await ReadSchedulesAsync(cancellationToken).ConfigureAwait(false);
        var histories = await ReadHistoryAsync(cancellationToken).ConfigureAwait(false);
        var workflows = await ReadWorkflowCatalogAsync(cancellationToken).ConfigureAwait(false);
        var scripts = _savedProfiles is null
            ? Array.Empty<AutomationSavedProfileSummary>()
            : await _savedProfiles.ListProfilesAsync("script-runner", cancellationToken).ConfigureAwait(false);
        var now = _clock.GetUtcNow();
        var states = schedules.Select(schedule => new AutomationScheduleState(schedule.Id,
            schedule.Enabled ? AutomationScheduleRecurrenceCalculator.NextOccurrenceUtc(schedule, now) : null,
            _running.ContainsKey(schedule.Id))).ToArray();
        return new AutomationSchedulerSnapshot(schedules, histories, !IsDisposed,
            Array.AsReadOnly(states), workflows, scripts);
    }

    public async Task SaveAsync(AutomationScheduleDefinition schedule, CancellationToken cancellationToken)
    {
        EnsureStarted();
        schedule = SchedulerModule.NormalizeAndValidate(schedule);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        cancellationToken = linked.Token;
        await _stateGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var catalog = schedule.TargetKind switch
            {
                AutomationScheduleTargetKind.Workflow => await ReadWorkflowCatalogAsync(cancellationToken).ConfigureAwait(false),
                AutomationScheduleTargetKind.ScriptProfile when _savedProfiles is not null => await _savedProfiles.ListProfilesAsync("script-runner", cancellationToken).ConfigureAwait(false),
                AutomationScheduleTargetKind.ScriptProfile => Array.Empty<AutomationSavedProfileSummary>(),
                _ => Array.Empty<AutomationSavedProfileSummary>(),
            };
            if (!catalog.Any(profile => profile.ProfileId == schedule.EffectiveProfileId))
                throw new InvalidDataException(schedule.TargetKind == AutomationScheduleTargetKind.Workflow
                    ? "The selected saved workflow does not exist."
                    : "The selected saved script profile does not exist.");

            if (_running.ContainsKey(schedule.Id)) throw new InvalidOperationException("A running schedule cannot be edited.");
            var existing = await ReadScheduleAsync(schedule.Id, cancellationToken).ConfigureAwait(false);
            if (schedule.Recurrence.Kind == AutomationScheduleRecurrenceKind.Interval
                && schedule.Recurrence.IntervalAnchorUtc is null)
                schedule = schedule with { Recurrence = schedule.Recurrence with {
                    IntervalAnchorUtc = existing?.Recurrence.Kind == AutomationScheduleRecurrenceKind.Interval
                        && existing.Recurrence.IntervalMinutes == schedule.Recurrence.IntervalMinutes
                        ? existing.Recurrence.IntervalAnchorUtc ?? _clock.GetUtcNow() : _clock.GetUtcNow() } };
            await WriteRecordAsync(ScheduleCollection, schedule.Id, schedule, cancellationToken).ConfigureAwait(false);
            await RebaselineCursorAsync(schedule, cancellationToken).ConfigureAwait(false);
        }
        finally { _stateGate.Release(); }
    }

    public async Task<bool> DeleteAsync(string scheduleId, CancellationToken cancellationToken)
    {
        ValidateId(scheduleId);
        EnsureStarted();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        cancellationToken = linked.Token;
        await _stateGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_running.ContainsKey(scheduleId)) throw new InvalidOperationException("A running schedule cannot be removed.");
            var deleted = await _library.DeleteAsync(ModuleId, ScheduleCollection, scheduleId, cancellationToken).ConfigureAwait(false);
            await _library.DeleteAsync(ModuleId, CursorCollection, scheduleId, cancellationToken).ConfigureAwait(false);
            return deleted;
        }
        finally { _stateGate.Release(); }
    }

    public async Task SetEnabledAsync(string scheduleId, bool enabled, CancellationToken cancellationToken)
    {
        ValidateId(scheduleId);
        EnsureStarted();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        cancellationToken = linked.Token;
        await _stateGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var schedule = await ReadScheduleAsync(scheduleId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidDataException("The saved schedule does not exist.");
            schedule = schedule with { Enabled = enabled };
            await WriteRecordAsync(ScheduleCollection, schedule.Id, schedule, cancellationToken).ConfigureAwait(false);
            if (enabled)
                await RebaselineCursorAsync(schedule, cancellationToken).ConfigureAwait(false);
        }
        finally { _stateGate.Release(); }
    }

    public async Task<AutomationScheduleHistoryEntry> RunNowAsync(string scheduleId, CancellationToken cancellationToken)
    {
        ValidateId(scheduleId);
        EnsureStarted();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        cancellationToken = linked.Token;
        var schedule = await ReadScheduleAsync(scheduleId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("The saved schedule does not exist.");
        return await RunScheduleAsync(schedule, _clock.GetUtcNow(), isOccurrence: false, cancellationToken).ConfigureAwait(false);
    }

    private async Task WorkerLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1), _clock);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                try { await ProcessDueSchedulesAsync(cancellationToken).ConfigureAwait(false); }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch { _log.Write(ApplicationLogLevel.Warning, "Scheduler.TickFailed", "A scheduler tick failed; scheduling will retry on the next tick."); }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception)
        {
            _log.Write(ApplicationLogLevel.Error, "Scheduler.WorkerFailed", "The scheduler worker stopped unexpectedly.");
        }
    }

    private async Task ProcessDueSchedulesAsync(CancellationToken cancellationToken)
    {
        var schedules = await ReadSchedulesAsync(cancellationToken).ConfigureAwait(false);
        var now = _clock.GetUtcNow();
        foreach (var schedule in schedules.Where(item => item.Enabled))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var due = AutomationScheduleRecurrenceCalculator.LatestOccurrenceUtc(schedule, now);
                if (due is null) continue;
                var cursor = await ReadCursorAsync(schedule.Id, cancellationToken).ConfigureAwait(false);
                if (cursor >= due || _running.ContainsKey(schedule.Id)) continue;
                QueueOccurrence(schedule, due.Value, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch { _log.Write(ApplicationLogLevel.Warning, "Scheduler.ScheduleTickFailed", "A schedule could not be processed; other schedules will continue.",
                properties: new Dictionary<string, object?> { ["scheduleId"] = schedule.Id }); }
        }
    }

    private void QueueOccurrence(AutomationScheduleDefinition schedule, DateTimeOffset occurrenceUtc, CancellationToken cancellationToken)
    {
        var sequence = Interlocked.Increment(ref _runSequence);
        var task = SafelyProcessOccurrenceAsync(schedule, occurrenceUtc, cancellationToken);
        _occurrenceTasks[sequence] = task;
        _ = task.ContinueWith(_ => { _occurrenceTasks.TryRemove(sequence, out var removed); },
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private async Task SafelyProcessOccurrenceAsync(AutomationScheduleDefinition schedule, DateTimeOffset occurrenceUtc, CancellationToken cancellationToken)
    {
        try { await ProcessOccurrenceAsync(schedule, occurrenceUtc, cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch { _log.Write(ApplicationLogLevel.Warning, "Scheduler.OccurrenceFailed", "A schedule occurrence could not be completed.",
            properties: new Dictionary<string, object?> { ["scheduleId"] = schedule.Id }); }
    }

    private async Task ProcessOccurrenceAsync(AutomationScheduleDefinition schedule, DateTimeOffset occurrenceUtc, CancellationToken cancellationToken)
    {
        if (!schedule.Enabled) return;
        var cursor = await ReadCursorAsync(schedule.Id, cancellationToken).ConfigureAwait(false);
        if (cursor >= occurrenceUtc) return;
        await RunScheduleAsync(schedule, occurrenceUtc, isOccurrence: true, cancellationToken).ConfigureAwait(false);
    }

    private async Task<AutomationScheduleHistoryEntry> RunScheduleAsync(
        AutomationScheduleDefinition schedule,
        DateTimeOffset occurrenceUtc,
        bool isOccurrence,
        CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        var sequence = Interlocked.Increment(ref _runSequence);
        var task = RunScheduleCoreAsync(schedule, occurrenceUtc, isOccurrence, linked.Token);
        _activeRuns[sequence] = task;
        try { return await task.ConfigureAwait(false); }
        finally { _activeRuns.TryRemove(sequence, out _); }
    }

    private async Task<AutomationScheduleHistoryEntry> RunScheduleCoreAsync(
        AutomationScheduleDefinition schedule, DateTimeOffset occurrenceUtc, bool isOccurrence, CancellationToken cancellationToken)
    {
        var id = isOccurrence ? OccurrenceId(schedule.Id, occurrenceUtc) : $"manual-{Guid.NewGuid():N}";
        var started = _clock.GetUtcNow();
        await _stateGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var latest = await ReadScheduleAsync(schedule.Id, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidDataException("The saved schedule does not exist.");
            schedule = latest;
            if (isOccurrence)
            {
                var currentDue = AutomationScheduleRecurrenceCalculator.LatestOccurrenceUtc(schedule, _clock.GetUtcNow());
                var cursor = await ReadCursorAsync(schedule.Id, cancellationToken).ConfigureAwait(false);
                if (!schedule.Enabled || currentDue != occurrenceUtc || cursor >= occurrenceUtc || _running.ContainsKey(schedule.Id))
                    return SkippedOccurrence(id, schedule.Id);
                var existingClaim = await _library.GetAsync(ModuleId, ClaimCollection, id, cancellationToken).ConfigureAwait(false);
                var history = await _library.GetAsync(ModuleId, HistoryCollection, id, cancellationToken).ConfigureAwait(false);
                if (existingClaim is not null || history is not null)
                {
                    await AdvanceCursorAsync(schedule.Id, occurrenceUtc, cancellationToken).ConfigureAwait(false);
                    return SkippedOccurrence(id, schedule.Id);
                }
            }
            if (!_running.TryAdd(schedule.Id, 0)) throw new InvalidOperationException("This schedule is already running.");
            try
            {
                var claim = new ScheduleClaim(id, schedule.Id, occurrenceUtc.ToUniversalTime(), started);
                await WriteRecordAsync(ClaimCollection, id, claim, cancellationToken).ConfigureAwait(false);
                if (isOccurrence) await AdvanceCursorAsync(schedule.Id, occurrenceUtc, cancellationToken).ConfigureAwait(false);
            }
            catch { _running.TryRemove(schedule.Id, out _); throw; }
        }
        finally { _stateGate.Release(); }

        try
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            AutomationExecutionSummary summary;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                summary = schedule.TargetKind switch
                {
                    AutomationScheduleTargetKind.Workflow => await _workflows.RunScheduledAsync(schedule.EffectiveProfileId, id, cancellationToken).ConfigureAwait(false),
                    AutomationScheduleTargetKind.ScriptProfile when _savedProfiles is not null =>
                        (await _savedProfiles.RunAsync("script-runner", schedule.EffectiveProfileId, null,
                            new AutomationExecutionMetadata(AutomationExecutionOrigin.Scheduled, id), cancellationToken).ConfigureAwait(false)).Summary,
                    _ => throw new InvalidOperationException("The saved script profile executor is unavailable."),
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                summary = new AutomationExecutionSummary(AutomationStatus.Warning, "canceled", (long)timer.Elapsed.TotalMilliseconds);
                var canceled = BuildHistory(id, schedule.Id, started, _clock.GetUtcNow(), summary);
                await CompleteRunAsync(canceled, CancellationToken.None).ConfigureAwait(false);
                throw;
            }
            catch (Exception)
            {
                summary = new AutomationExecutionSummary(AutomationStatus.Error, "target-unavailable", (long)timer.Elapsed.TotalMilliseconds);
            }

            timer.Stop();
            var history = BuildHistory(id, schedule.Id, started, _clock.GetUtcNow(), summary);
            await CompleteRunAsync(history, CancellationToken.None).ConfigureAwait(false);
            _log.Write(summary.Status == AutomationStatus.Success ? ApplicationLogLevel.Information : ApplicationLogLevel.Warning,
                "Scheduler.ScheduleRunCompleted", "A saved automation schedule run completed.",
                properties: new Dictionary<string, object?> { ["scheduleId"] = schedule.Id, ["category"] = history.Category, ["durationMs"] = history.DurationMilliseconds });
            return history;
        }
        catch
        {
            // A failed persistence/execution path leaves a claim for startup recovery instead of replaying side effects.
            throw;
        }
        finally { _running.TryRemove(schedule.Id, out _); }
    }

    private async Task CompleteRunAsync(AutomationScheduleHistoryEntry history, CancellationToken cancellationToken)
    {
        await WriteRecordAsync(HistoryCollection, history.Id, history, cancellationToken).ConfigureAwait(false);
        await _library.DeleteAsync(ModuleId, ClaimCollection, history.Id, cancellationToken).ConfigureAwait(false);
        await PruneHistoryAsync(cancellationToken).ConfigureAwait(false);
        await NotifyHistoryChangedAsync().ConfigureAwait(false);
    }

    private async Task NotifyHistoryChangedAsync()
    {
        if (_historyChanged is null) return;
        try { await _historyChanged().ConfigureAwait(false); }
        catch { _log.Write(ApplicationLogLevel.Warning, "Scheduler.HistoryNotificationFailed", "Schedule history was saved, but the renderer could not be notified."); }
    }

    private async Task RecoverClaimsAsync(CancellationToken cancellationToken)
    {
        var claims = await _library.ListAsync(ModuleId, ClaimCollection, cancellationToken).ConfigureAwait(false);
        var recoveredHistory = false;
        foreach (var record in claims)
        {
            if (!TryDeserialize<ScheduleClaim>(record, out var claim) || claim is null || claim.ScheduleId is null
                || !IdPattern.IsMatch(claim.ScheduleId) || claim.Id != record.Id
                || (claim.Id != OccurrenceId(claim.ScheduleId, claim.OccurrenceUtc) && !claim.Id.StartsWith("manual-", StringComparison.Ordinal)))
            {
                await _library.DeleteAsync(ModuleId, ClaimCollection, record.Id, cancellationToken).ConfigureAwait(false);
                continue;
            }

            var previous = await _library.GetAsync(ModuleId, HistoryCollection, claim.Id, cancellationToken).ConfigureAwait(false);
            if (previous is null)
            {
                var interrupted = new AutomationScheduleHistoryEntry(claim.Id, claim.ScheduleId, claim.ClaimedUtc,
                    _startedUtc, AutomationStatus.Warning, "interrupted", 0);
                await WriteRecordAsync(HistoryCollection, claim.Id, interrupted, cancellationToken).ConfigureAwait(false);
                recoveredHistory = true;
            }
            if (!claim.Id.StartsWith("manual-", StringComparison.Ordinal))
                await AdvanceCursorAsync(claim.ScheduleId, claim.OccurrenceUtc, cancellationToken).ConfigureAwait(false);
            await _library.DeleteAsync(ModuleId, ClaimCollection, claim.Id, cancellationToken).ConfigureAwait(false);
        }
        await PruneHistoryAsync(cancellationToken).ConfigureAwait(false);
        if (recoveredHistory) await NotifyHistoryChangedAsync().ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<AutomationScheduleDefinition>> ReadSchedulesAsync(CancellationToken cancellationToken)
    {
        var records = await _library.ListAsync(ModuleId, ScheduleCollection, cancellationToken).ConfigureAwait(false);
        return Array.AsReadOnly(records.Select(ReadValidSchedule)
            .Where(schedule => schedule is not null).Cast<AutomationScheduleDefinition>()
            .OrderBy(schedule => schedule.Name, StringComparer.CurrentCultureIgnoreCase).Take(256).ToArray());
    }

    private async Task<AutomationScheduleDefinition?> ReadScheduleAsync(string id, CancellationToken cancellationToken)
    {
        var record = await _library.GetAsync(ModuleId, ScheduleCollection, id, cancellationToken).ConfigureAwait(false);
        return record is null ? null : ReadValidSchedule(record);
    }

    private static AutomationScheduleDefinition? ReadValidSchedule(AutomationLibraryRecord record)
    {
        if (!TryDeserialize<AutomationScheduleDefinition>(record, out var schedule) || schedule is null || schedule.Id != record.Id) return null;
        try { return SchedulerModule.NormalizeAndValidate(schedule); }
        catch (InvalidDataException) { return null; }
    }

    private async Task<IReadOnlyList<AutomationScheduleHistoryEntry>> ReadHistoryAsync(CancellationToken cancellationToken)
    {
        var records = await _library.ListAsync(ModuleId, HistoryCollection, cancellationToken).ConfigureAwait(false);
        return Array.AsReadOnly(records.Select(record => TryDeserialize<AutomationScheduleHistoryEntry>(record, out var history) ? history : null)
            .Where(history => history is not null).Cast<AutomationScheduleHistoryEntry>()
            .OrderByDescending(history => history.StartedUtc).Take(MaximumHistoryEntries).ToArray());
    }

    private async Task<IReadOnlyList<AutomationSavedProfileSummary>> ReadWorkflowCatalogAsync(CancellationToken cancellationToken)
    {
        var records = await _library.ListAsync("workflows", "profiles", cancellationToken).ConfigureAwait(false);
        var result = new List<AutomationSavedProfileSummary>();
        foreach (var record in records.Take(1024))
        {
            if (record.SchemaVersion != 1 || record.Data.ValueKind != JsonValueKind.Object
                || !record.Data.TryGetProperty("id", out var idValue) || idValue.ValueKind != JsonValueKind.String
                || !record.Data.TryGetProperty("name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String) continue;
            var id = idValue.GetString();
            var name = nameValue.GetString();
            if (id is null || name is null || !IdPattern.IsMatch(id) || string.IsNullOrWhiteSpace(name)
                || name.Length > 128 || name.Any(char.IsControl) || result.Any(item => item.ProfileId == id)) continue;
            result.Add(new AutomationSavedProfileSummary(id, name));
        }
        return Array.AsReadOnly(result.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase).Take(256).ToArray());
    }

    private async Task<DateTimeOffset?> ReadCursorAsync(string scheduleId, CancellationToken cancellationToken)
    {
        var record = await _library.GetAsync(ModuleId, CursorCollection, scheduleId, cancellationToken).ConfigureAwait(false);
        if (record is null || record.SchemaVersion != 1 || record.Data.ValueKind != JsonValueKind.Object
            || !record.Data.TryGetProperty("lastOccurrenceUtc", out var value) || value.ValueKind != JsonValueKind.String
            || !value.TryGetDateTimeOffset(out var instant)) return null;
        return instant.ToUniversalTime();
    }

    private Task WriteCursorAsync(string scheduleId, DateTimeOffset occurrenceUtc, CancellationToken cancellationToken) =>
        WriteRecordAsync(CursorCollection, scheduleId, new { lastOccurrenceUtc = occurrenceUtc.ToUniversalTime() }, cancellationToken);

    private async Task AdvanceCursorAsync(string scheduleId, DateTimeOffset occurrenceUtc, CancellationToken cancellationToken)
    {
        var previous = await ReadCursorAsync(scheduleId, cancellationToken).ConfigureAwait(false);
        if (previous is null || previous < occurrenceUtc)
            await WriteCursorAsync(scheduleId, occurrenceUtc, cancellationToken).ConfigureAwait(false);
    }

    private async Task RebaselineCursorAsync(AutomationScheduleDefinition schedule, CancellationToken cancellationToken)
    {
        var due = AutomationScheduleRecurrenceCalculator.LatestOccurrenceUtc(schedule, _clock.GetUtcNow());
        if (due is { } occurrence) await WriteCursorAsync(schedule.Id, occurrence, cancellationToken).ConfigureAwait(false);
        else await _library.DeleteAsync(ModuleId, CursorCollection, schedule.Id, cancellationToken).ConfigureAwait(false);
    }

    private async Task PruneHistoryAsync(CancellationToken cancellationToken)
    {
        var records = await _library.ListAsync(ModuleId, HistoryCollection, cancellationToken).ConfigureAwait(false);
        foreach (var record in records.OrderByDescending(item => item.UpdatedUtc).Skip(MaximumHistoryEntries))
            await _library.DeleteAsync(ModuleId, HistoryCollection, record.Id, cancellationToken).ConfigureAwait(false);
    }

    private Task WriteRecordAsync<T>(string collection, string id, T value, CancellationToken cancellationToken) =>
        _library.UpsertAsync(new AutomationLibraryRecord(ModuleId, collection, id, 1,
            JsonSerializer.SerializeToElement(value, JsonOptions), _clock.GetUtcNow()), cancellationToken);

    private static bool TryDeserialize<T>(AutomationLibraryRecord record, out T? value)
    {
        try
        {
            value = record.SchemaVersion == 1 ? record.Data.Deserialize<T>(JsonOptions) : default;
            return value is not null;
        }
        catch (JsonException) { value = default; return false; }
    }

    private AutomationScheduleHistoryEntry SkippedOccurrence(string id, string scheduleId) =>
        new(id, scheduleId, _clock.GetUtcNow(), _clock.GetUtcNow(), AutomationStatus.Information, "skipped", 0);

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        // Integer values remain readable for the first implementation's existing schema-1 records.
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private static AutomationScheduleHistoryEntry BuildHistory(
        string id, string scheduleId, DateTimeOffset started, DateTimeOffset finished, AutomationExecutionSummary summary)
    {
        var safe = summary.Status is AutomationStatus.Success or AutomationStatus.Information or AutomationStatus.Warning or AutomationStatus.Error
            ? summary.Status : AutomationStatus.Error;
        var category = summary.Category is "completed" or "step-execution-failed" or "workflow-unavailable" or "target-unavailable"
            or "timed-out" or "nonzero-exit" or "invalid-json" or "canceled"
            ? summary.Category : safe == AutomationStatus.Success ? "completed" : "target-unavailable";
        return new AutomationScheduleHistoryEntry(id, scheduleId, started.ToUniversalTime(), finished.ToUniversalTime(), safe,
            category, Math.Clamp(summary.DurationMilliseconds, 0, 86_400_000));
    }

    private static string OccurrenceId(string scheduleId, DateTimeOffset occurrence) =>
        $"{scheduleId}-{occurrence.ToUniversalTime().Ticks:x16}";

    private static void ValidateId(string id)
    {
        if (!IdPattern.IsMatch(id)) throw new InvalidDataException("The schedule key is invalid.");
    }

    private void EnsureStarted()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (Volatile.Read(ref _started) == 0) throw new InvalidOperationException("The scheduler coordinator has not started.");
    }

    private bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _lifetime.Cancel();
        // Synchronize with a pending startup or control mutation before disposing the gate.
        await _stateGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        _stateGate.Release();
        if (_worker is not null)
        {
            try { await _worker.ConfigureAwait(false); } catch (OperationCanceledException) { }
        }
        Task[] catchups;
        lock (_catchupTasks) catchups = [.. _catchupTasks];
        try { await Task.WhenAll(catchups).ConfigureAwait(false); } catch (OperationCanceledException) { }
        try { await Task.WhenAll(_occurrenceTasks.Values.ToArray()).ConfigureAwait(false); } catch (OperationCanceledException) { }
        var active = _activeRuns.Values.ToArray();
        try { await Task.WhenAll(active).ConfigureAwait(false); } catch { /* Claims preserve interrupted storage failures for recovery. */ }
        _lifetime.Dispose();
        _stateGate.Dispose();
    }

    private sealed record ScheduleClaim(string Id, string ScheduleId, DateTimeOffset OccurrenceUtc, DateTimeOffset ClaimedUtc);
}
