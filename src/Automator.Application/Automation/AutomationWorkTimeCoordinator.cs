using System.Text.Json;

namespace Automator.Application.Automation;

/// <summary>Persists named timers, running segments, independent drafts and saved history.</summary>
public sealed class AutomationWorkTimeCoordinator : IAutomationWorkTimeCoordinator, IAsyncDisposable
{
    public const string ModuleId = FocusSessionsModule.IdValue;
    public const string StateCollection = "work-time";
    public const string HistoryCollection = "work-time-history";
    public const int MaximumHistoryEntries = 500;
    private const string CurrentId = "current";
    private const int RecordSchemaVersion = 2;
    private const string UntitledTimer = "Untitled timer";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IAutomationLibraryStore _libraryStore;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _stateGate = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly List<AutomationWorkTimeEntry> _history = [];
    private PersistedState _state = new([], []);
    private int _initialized;
    private int _disposed;

    public AutomationWorkTimeCoordinator(IAutomationLibraryStore libraryStore, TimeProvider? timeProvider = null)
    {
        _libraryStore = libraryStore ?? throw new ArgumentNullException(nameof(libraryStore));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            if (_initialized != 0) return;
            var current = await _libraryStore.GetAsync(ModuleId, StateCollection, CurrentId, linked.Token).ConfigureAwait(false);
            var state = current is null ? new PersistedState([], []) : current.SchemaVersion == 1
                ? MigrateState(Deserialize<LegacyState>(current, "work-time state"))
                : Deserialize<PersistedState>(current, "work-time state");
            ValidateState(state);
            var history = await _libraryStore.ListAsync(ModuleId, HistoryCollection, linked.Token).ConfigureAwait(false);
            _history.Clear();
            foreach (var record in history)
            {
                var entry = record.SchemaVersion == 1
                    ? MigrateEntry(Deserialize<LegacyEntry>(record, "work-time history entry"))
                    : Deserialize<AutomationWorkTimeEntry>(record, "work-time history entry");
                ValidateEntry(entry);
                _history.Add(entry);
            }
            _history.Sort((a, b) => a.StoppedUtc.CompareTo(b.StoppedUtc));
            // A history write may have succeeded immediately before shutdown interrupted draft removal.
            var savedIds = _history.Select(entry => entry.Id).ToHashSet(StringComparer.Ordinal);
            var repaired = state with { PendingEntries = state.PendingEntries.Where(entry => !savedIds.Contains(entry.Id)).ToArray() };
            if (current?.SchemaVersion == 1 || repaired.PendingEntries.Count != state.PendingEntries.Count)
                await PersistStateAsync(repaired, linked.Token).ConfigureAwait(false);
            _state = repaired;
            await PruneHistoryAsync(linked.Token).ConfigureAwait(false);
            Volatile.Write(ref _initialized, 1);
        }
        finally { _stateGate.Release(); }
    }

    public Task<AutomationWorkTimeSnapshot> GetSnapshotAsync(CancellationToken cancellationToken) =>
        OperateAsync((_, _) => Task.CompletedTask, cancellationToken);

    public Task<AutomationWorkTimeSnapshot> StartAsync(string description, CancellationToken cancellationToken)
    {
        var label = CleanDescription(description);
        return OperateAsync(async (now, token) =>
        {
            RequireNoRunner();
            var timer = new AutomationWorkTimeTimer(Guid.NewGuid().ToString("N"), label, now, "running", 0, now,
                [new AutomationWorkTimeSegment(now, null)]);
            await CommitStateAsync(_state with { Timers = [.. _state.Timers, timer] }, token).ConfigureAwait(false);
        }, cancellationToken);
    }

    public Task<AutomationWorkTimeSnapshot> PauseAsync(string id, CancellationToken cancellationToken)
    {
        var timerId = CleanId(id);
        return OperateAsync(async (now, token) =>
        {
            var timer = FindTimer(timerId);
            if (timer.Status != "running") throw new InvalidOperationException("The work timer is already paused.");
            await ReplaceTimerAsync(CloseTimer(timer, now), token).ConfigureAwait(false);
        }, cancellationToken);
    }

    public Task<AutomationWorkTimeSnapshot> ResumeAsync(string id, CancellationToken cancellationToken)
    {
        var timerId = CleanId(id);
        return OperateAsync(async (now, token) =>
        {
            var timer = FindTimer(timerId);
            if (timer.Status != "paused") throw new InvalidOperationException("Only a paused work timer can be resumed.");
            RequireNoRunner();
            var start = timer.Segments.Last().StoppedUtc is { } previous && now < previous ? previous : now;
            await ReplaceTimerAsync(timer with { Status = "running", SampledUtc = now,
                Segments = [.. timer.Segments, new AutomationWorkTimeSegment(start, null)] }, token).ConfigureAwait(false);
        }, cancellationToken);
    }

    public Task<AutomationWorkTimeSnapshot> EndAsync(string id, CancellationToken cancellationToken)
    {
        var timerId = CleanId(id);
        return OperateAsync(async (now, token) =>
        {
            var timer = FindTimer(timerId);
            if (timer.Status == "running") timer = CloseTimer(timer, now);
            var entry = new AutomationWorkTimeEntry(timer.Id, timer.StartedUtc, timer.Segments.Last().StoppedUtc!.Value,
                timer.ElapsedMilliseconds, timer.Description, [], timer.Segments);
            await CommitStateAsync(new PersistedState(_state.Timers.Where(t => t.Id != timerId).ToArray(),
                [.. _state.PendingEntries, entry]), token).ConfigureAwait(false);
        }, cancellationToken);
    }

    public Task<AutomationWorkTimeSnapshot> SaveEntryAsync(string id, string description, IReadOnlyList<string> tags, CancellationToken cancellationToken)
    {
        var entryId = CleanId(id);
        var label = CleanDescription(description);
        var cleanedTags = NormalizeTags(tags);
        return OperateAsync(async (_, token) =>
        {
            var draft = _state.PendingEntries.FirstOrDefault(entry => entry.Id == entryId)
                ?? throw new InvalidOperationException("The pending work entry no longer exists.");
            var entry = draft with { Description = label, Tags = cleanedTags };
            await WriteEntryAsync(entry, token).ConfigureAwait(false);
            await CommitStateAsync(_state with { PendingEntries = _state.PendingEntries.Where(e => e.Id != entryId).ToArray() }, token).ConfigureAwait(false);
            _history.RemoveAll(item => item.Id == entry.Id);
            _history.Add(entry);
            _history.Sort((a, b) => a.StoppedUtc.CompareTo(b.StoppedUtc));
            await PruneHistoryAsync(token).ConfigureAwait(false);
        }, cancellationToken);
    }

    public Task<AutomationWorkTimeSnapshot> UpdateEntryAsync(string id, string description, IReadOnlyList<string> tags, CancellationToken cancellationToken)
    {
        var entryId = CleanId(id);
        var label = CleanDescription(description);
        var cleanedTags = NormalizeTags(tags);
        return OperateAsync(async (_, token) =>
        {
            var index = _history.FindIndex(entry => entry.Id == entryId);
            if (index < 0) throw new InvalidOperationException("The saved work entry no longer exists.");
            var entry = _history[index] with { Description = label, Tags = cleanedTags };
            await WriteEntryAsync(entry, token).ConfigureAwait(false);
            _history[index] = entry;
        }, cancellationToken);
    }

    public Task<AutomationWorkTimeSnapshot> DeleteEntryAsync(string id, CancellationToken cancellationToken)
    {
        var entryId = CleanId(id);
        return OperateAsync(async (_, token) =>
        {
            var index = _history.FindIndex(entry => entry.Id == entryId);
            if (index < 0) throw new InvalidOperationException("The saved work entry no longer exists.");
            if (!await _libraryStore.DeleteAsync(ModuleId, HistoryCollection, entryId, token).ConfigureAwait(false))
                throw new InvalidOperationException("The saved work entry could not be found in the library.");
            _history.RemoveAt(index);
        }, cancellationToken);
    }

    public Task<AutomationWorkTimeSnapshot> DiscardTimerAsync(string id, CancellationToken cancellationToken)
    {
        var timerId = CleanId(id);
        return OperateAsync(async (_, token) =>
        {
            if (FindTimer(timerId).Status != "paused") throw new InvalidOperationException("Pause the work timer before discarding it.");
            await CommitStateAsync(_state with { Timers = _state.Timers.Where(t => t.Id != timerId).ToArray() }, token).ConfigureAwait(false);
        }, cancellationToken);
    }

    public Task<AutomationWorkTimeSnapshot> DiscardPendingAsync(string id, CancellationToken cancellationToken)
    {
        var entryId = CleanId(id);
        return OperateAsync(async (_, token) =>
        {
            if (!_state.PendingEntries.Any(e => e.Id == entryId)) throw new InvalidOperationException("The pending work entry no longer exists.");
            await CommitStateAsync(_state with { PendingEntries = _state.PendingEntries.Where(e => e.Id != entryId).ToArray() }, token).ConfigureAwait(false);
        }, cancellationToken);
    }

    private async Task<AutomationWorkTimeSnapshot> OperateAsync(Func<DateTimeOffset, CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (Volatile.Read(ref _initialized) == 0) throw new InvalidOperationException("The work-time coordinator has not been initialized.");
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            var now = _timeProvider.GetUtcNow();
            await action(now, linked.Token).ConfigureAwait(false);
            return new AutomationWorkTimeSnapshot(_state.Timers.Select(t => t with {
                ElapsedMilliseconds = ElapsedMilliseconds(t, now), SampledUtc = now, Segments = t.Segments.ToArray() }).ToArray(),
                _state.PendingEntries.Select(CopyEntry).ToArray(), _history.Select(CopyEntry).ToArray());
        }
        finally { _stateGate.Release(); }
    }

    private static AutomationWorkTimeEntry CopyEntry(AutomationWorkTimeEntry entry) =>
        entry with { Tags = entry.Tags.ToArray(), Segments = entry.Segments.ToArray() };

    private AutomationWorkTimeTimer FindTimer(string id) => _state.Timers.FirstOrDefault(timer => timer.Id == id)
        ?? throw new InvalidOperationException("The work timer no longer exists.");

    private void RequireNoRunner()
    {
        if (_state.Timers.Any(timer => timer.Status == "running"))
            throw new InvalidOperationException("Pause or end the running timer before starting or resuming another.");
    }

    private Task ReplaceTimerAsync(AutomationWorkTimeTimer timer, CancellationToken token) =>
        CommitStateAsync(_state with { Timers = _state.Timers.Select(t => t.Id == timer.Id ? timer : t).ToArray() }, token);

    private async Task CommitStateAsync(PersistedState state, CancellationToken token)
    {
        ValidateState(state);
        await PersistStateAsync(state, token).ConfigureAwait(false);
        _state = state;
    }

    private static long ElapsedMilliseconds(AutomationWorkTimeTimer timer, DateTimeOffset now) =>
        checked(timer.ElapsedMilliseconds + (timer.Status == "running"
            ? Math.Max(0, (long)(now - timer.Segments.Last().StartedUtc).TotalMilliseconds) : 0));

    private static AutomationWorkTimeTimer CloseTimer(AutomationWorkTimeTimer timer, DateTimeOffset now)
    {
        var last = timer.Segments.Last();
        var stop = now < last.StartedUtc ? last.StartedUtc : now;
        return timer with { Status = "paused", ElapsedMilliseconds = ElapsedMilliseconds(timer, now), SampledUtc = now,
            Segments = [.. timer.Segments.Take(timer.Segments.Count - 1), last with { StoppedUtc = stop }] };
    }

    private Task PersistStateAsync(PersistedState state, CancellationToken token) =>
        _libraryStore.UpsertAsync(new AutomationLibraryRecord(ModuleId, StateCollection, CurrentId, RecordSchemaVersion,
            JsonSerializer.SerializeToElement(state, JsonOptions), _timeProvider.GetUtcNow()), token);

    private Task WriteEntryAsync(AutomationWorkTimeEntry entry, CancellationToken token) =>
        _libraryStore.UpsertAsync(new AutomationLibraryRecord(ModuleId, HistoryCollection, entry.Id, RecordSchemaVersion,
            JsonSerializer.SerializeToElement(entry, JsonOptions), _timeProvider.GetUtcNow()), token);

    private async Task PruneHistoryAsync(CancellationToken token)
    {
        while (_history.Count > MaximumHistoryEntries)
        {
            await _libraryStore.DeleteAsync(ModuleId, HistoryCollection, _history[0].Id, token).ConfigureAwait(false);
            _history.RemoveAt(0);
        }
    }

    private static string CleanId(string id)
    {
        var result = id?.Trim() ?? string.Empty;
        if (result.Length is 0 or > 128) throw new InvalidDataException("The work entry id is invalid.");
        return result;
    }

    private static string CleanDescription(string description)
    {
        var result = description?.Trim() ?? string.Empty;
        if (result.Length == 0) throw new InvalidDataException("A work description is required.");
        if (result.Length > 500) throw new InvalidDataException("A work description can contain at most 500 characters.");
        return result;
    }

    private static string[] NormalizeTags(IReadOnlyList<string> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in tags)
        {
            var tag = raw?.Trim() ?? string.Empty;
            if (tag.Length == 0) continue;
            if (tag.Length > 40) throw new InvalidDataException("A work tag can contain at most 40 characters.");
            if (!seen.Add(tag)) continue;
            result.Add(tag);
            if (result.Count > 30) throw new InvalidDataException("A work entry can contain at most 30 tags.");
        }
        return result.ToArray();
    }

    private static void ValidateState(PersistedState state)
    {
        if (state.Timers is null || state.PendingEntries is null || state.Timers.Count(t => t.Status == "running") > 1)
            throw new InvalidDataException("The saved work-time state is invalid.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var timer in state.Timers)
        {
            CleanId(timer.Id);
            CleanDescription(timer.Description);
            if (!ids.Add(timer.Id) || timer.Status is not ("running" or "paused") || timer.StartedUtc == default
                || timer.SampledUtc == default || timer.ElapsedMilliseconds < 0)
                throw new InvalidDataException("The saved work timer is invalid.");
            ValidateSegments(timer.Segments, timer.StartedUtc, timer.Status == "running");
        }
        foreach (var entry in state.PendingEntries)
        {
            if (!ids.Add(entry.Id)) throw new InvalidDataException("The saved work-time ids are duplicated.");
            ValidateEntry(entry);
        }
    }

    private static void ValidateEntry(AutomationWorkTimeEntry entry)
    {
        CleanId(entry.Id);
        CleanDescription(entry.Description);
        if (entry.StartedUtc == default || entry.StoppedUtc < entry.StartedUtc || entry.DurationMilliseconds < 0
            || entry.Tags is null || entry.Tags.Count > 30 || entry.Tags.Any(tag => string.IsNullOrWhiteSpace(tag) || tag.Length > 40))
            throw new InvalidDataException("The saved work-time entry is invalid.");
        ValidateSegments(entry.Segments, entry.StartedUtc, false);
        if (entry.Segments.Last().StoppedUtc != entry.StoppedUtc)
            throw new InvalidDataException("The saved work entry end does not match its final segment.");
    }

    private static void ValidateSegments(IReadOnlyList<AutomationWorkTimeSegment> segments, DateTimeOffset start, bool running)
    {
        if (segments is null || segments.Count == 0) throw new InvalidDataException("The saved running segments are missing.");
        var previousStop = start;
        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            if (segment is null || segment.StartedUtc == default || segment.StartedUtc < previousStop
                || segment.StoppedUtc < segment.StartedUtc || (segment.StoppedUtc is null) != (running && i == segments.Count - 1))
                throw new InvalidDataException("The saved running segments are invalid.");
            previousStop = segment.StoppedUtc ?? segment.StartedUtc;
        }
    }

    private static PersistedState MigrateState(LegacyState legacy)
    {
        if (legacy.Active is not null && legacy.Pending is not null)
            throw new InvalidDataException("The legacy work-time state has both active and pending entries.");
        if (legacy.Active is { } active)
        {
            if (active.StartedUtc == default || active.RunningSinceUtc < active.StartedUtc || active.AccumulatedMilliseconds < 0)
                throw new InvalidDataException("The legacy active work timer is invalid.");
            var segments = new List<AutomationWorkTimeSegment>();
            if (active.RunningSinceUtc > active.StartedUtc && active.AccumulatedMilliseconds > 0)
                segments.Add(new(active.StartedUtc, active.RunningSinceUtc));
            segments.Add(new(active.RunningSinceUtc, null));
            return new([new(active.Id, UntitledTimer, active.StartedUtc, "running", active.AccumulatedMilliseconds,
                active.RunningSinceUtc, segments.ToArray())], []);
        }
        if (legacy.Pending is { } pending)
        {
            var entry = MigrateEntry(pending);
            ValidateEntry(entry);
            return new([new(entry.Id, entry.Description, entry.StartedUtc, "paused", entry.DurationMilliseconds,
                entry.StoppedUtc, entry.Segments)], []);
        }
        return new([], []);
    }

    private static AutomationWorkTimeEntry MigrateEntry(LegacyEntry entry) => new(entry.Id, entry.StartedUtc, entry.StoppedUtc,
        entry.DurationMilliseconds, string.IsNullOrWhiteSpace(entry.Description) ? UntitledTimer : entry.Description,
        entry.Tags, [new(entry.StartedUtc, entry.StoppedUtc)]);

    private static T Deserialize<T>(AutomationLibraryRecord record, string description)
    {
        if (record.SchemaVersion is not (1 or RecordSchemaVersion))
            throw new InvalidDataException($"The saved {description} uses an unsupported schema version.");
        try
        {
            return record.Data.Deserialize<T>(JsonOptions) ?? throw new InvalidDataException($"The saved {description} is empty.");
        }
        catch (JsonException exception) { throw new InvalidDataException($"The saved {description} is invalid.", exception); }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _lifetime.Cancel();
        await _stateGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        _stateGate.Release();
        _lifetime.Dispose();
        _stateGate.Dispose();
    }

    private sealed record PersistedState(IReadOnlyList<AutomationWorkTimeTimer> Timers, IReadOnlyList<AutomationWorkTimeEntry> PendingEntries);
    private sealed record LegacyActive(string Id, DateTimeOffset StartedUtc, long AccumulatedMilliseconds, DateTimeOffset RunningSinceUtc);
    private sealed record LegacyEntry(string Id, DateTimeOffset StartedUtc, DateTimeOffset StoppedUtc, long DurationMilliseconds, string Description, IReadOnlyList<string> Tags);
    private sealed record LegacyState(LegacyActive? Active, LegacyEntry? Pending);
}
