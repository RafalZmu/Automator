using System.Text.Json;

namespace Automator.Application.Automation;

/// <summary>
/// Owns the active work interval and pending metadata draft for the backend lifetime. The active
/// start is durable, so an interval continues across an Automator restart until explicitly stopped.
/// </summary>
public sealed class AutomationWorkTimeCoordinator : IAutomationWorkTimeCoordinator, IAsyncDisposable
{
    public const string ModuleId = FocusSessionsModule.IdValue;
    public const string StateCollection = "work-time";
    public const string HistoryCollection = "work-time-history";
    public const int MaximumHistoryEntries = 500;

    private const string CurrentId = "current";
    private const int RecordSchemaVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAutomationLibraryStore _libraryStore;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _stateGate = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly List<AutomationWorkTimeEntry> _history = [];
    private PersistedActive? _active;
    private AutomationWorkTimeEntry? _pending;
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
            if (current is not null)
            {
                if (current.SchemaVersion != RecordSchemaVersion)
                    throw new InvalidDataException("The saved work-time state uses an unsupported schema version.");
                var state = Deserialize<PersistedState>(current, "work-time state");
                _active = state.Active;
                _pending = state.Pending;
                ValidateState(_active, _pending);
            }

            var history = await _libraryStore.ListAsync(ModuleId, HistoryCollection, linked.Token).ConfigureAwait(false);
            _history.Clear();
            foreach (var record in history.OrderBy(item => item.UpdatedUtc))
            {
                var entry = Deserialize<AutomationWorkTimeEntry>(record, "work-time history entry");
                ValidateEntry(entry, requireDescription: true);
                _history.Add(entry);
            }
            await PruneHistoryAsync(linked.Token).ConfigureAwait(false);
            Volatile.Write(ref _initialized, 1);
        }
        finally { _stateGate.Release(); }
    }

    public async Task<AutomationWorkTimeSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        using var linked = CreateOperationToken(cancellationToken);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            var now = _timeProvider.GetUtcNow();
            var active = _active is null ? null : ToActiveSnapshot(_active, now);
            return new AutomationWorkTimeSnapshot(active, _pending, _history.ToArray());
        }
        finally { _stateGate.Release(); }
    }

    public async Task<AutomationWorkTimeSnapshot> StartAsync(CancellationToken cancellationToken)
    {
        using var linked = CreateOperationToken(cancellationToken);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            if (_active is not null) throw new InvalidOperationException("Stop the active work interval before starting another.");
            if (_pending is not null) throw new InvalidOperationException("Save, resume, or discard the pending work entry first.");

            var now = _timeProvider.GetUtcNow();
            var active = new PersistedActive(Guid.NewGuid().ToString("N"), now, 0, now);
            await PersistStateAsync(active, null, linked.Token).ConfigureAwait(false);
            _active = active;
            return Snapshot(now);
        }
        finally { _stateGate.Release(); }
    }

    public async Task<AutomationWorkTimeSnapshot> StopAsync(CancellationToken cancellationToken)
    {
        using var linked = CreateOperationToken(cancellationToken);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            if (_active is null) throw new InvalidOperationException("Start a work interval before stopping it.");

            var now = _timeProvider.GetUtcNow();
            var pending = new AutomationWorkTimeEntry(_active.Id, _active.StartedUtc, now,
                ElapsedMilliseconds(_active, now), string.Empty, []);
            await PersistStateAsync(null, pending, linked.Token).ConfigureAwait(false);
            _active = null;
            _pending = pending;
            return Snapshot(now);
        }
        finally { _stateGate.Release(); }
    }

    public async Task<AutomationWorkTimeSnapshot> SaveEntryAsync(
        string description,
        IReadOnlyList<string> tags,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tags);
        var cleanedDescription = description?.Trim() ?? string.Empty;
        if (cleanedDescription.Length == 0)
            throw new InvalidDataException("Add a description before saving the work entry.");
        if (cleanedDescription.Length > 500)
            throw new InvalidDataException("A work description can contain at most 500 characters.");
        var cleanedTags = NormalizeTags(tags);

        using var linked = CreateOperationToken(cancellationToken);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            if (_pending is null) throw new InvalidOperationException("Stop a work interval before saving its entry.");
            var entry = _pending with { Description = cleanedDescription, Tags = cleanedTags };
            ValidateEntry(entry, requireDescription: true);
            await WriteEntryAsync(entry, linked.Token).ConfigureAwait(false);
            await PersistStateAsync(null, null, linked.Token).ConfigureAwait(false);
            _history.RemoveAll(item => item.Id == entry.Id);
            _history.Add(entry);
            _history.Sort((left, right) => left.StoppedUtc.CompareTo(right.StoppedUtc));
            await PruneHistoryAsync(linked.Token).ConfigureAwait(false);
            _pending = null;
            return Snapshot(_timeProvider.GetUtcNow());
        }
        finally { _stateGate.Release(); }
    }

    public async Task<AutomationWorkTimeSnapshot> UpdateEntryAsync(
        string id,
        string description,
        IReadOnlyList<string> tags,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tags);
        var entryId = id?.Trim() ?? string.Empty;
        if (entryId.Length is 0 or > 128) throw new InvalidDataException("The work entry id is invalid.");
        var cleanedDescription = description?.Trim() ?? string.Empty;
        if (cleanedDescription.Length == 0)
            throw new InvalidDataException("Add a description before saving the work entry.");
        if (cleanedDescription.Length > 500)
            throw new InvalidDataException("A work description can contain at most 500 characters.");
        var cleanedTags = NormalizeTags(tags);

        using var linked = CreateOperationToken(cancellationToken);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            var index = _history.FindIndex(item => string.Equals(item.Id, entryId, StringComparison.Ordinal));
            if (index < 0) throw new InvalidOperationException("The saved work entry no longer exists.");
            var entry = _history[index] with { Description = cleanedDescription, Tags = cleanedTags };
            ValidateEntry(entry, requireDescription: true);
            await WriteEntryAsync(entry, linked.Token).ConfigureAwait(false);
            _history[index] = entry;
            return Snapshot(_timeProvider.GetUtcNow());
        }
        finally { _stateGate.Release(); }
    }

    public async Task<AutomationWorkTimeSnapshot> DeleteEntryAsync(string id, CancellationToken cancellationToken)
    {
        var entryId = id?.Trim() ?? string.Empty;
        if (entryId.Length is 0 or > 128) throw new InvalidDataException("The work entry id is invalid.");

        using var linked = CreateOperationToken(cancellationToken);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            var index = _history.FindIndex(item => string.Equals(item.Id, entryId, StringComparison.Ordinal));
            if (index < 0) throw new InvalidOperationException("The saved work entry no longer exists.");
            if (!await _libraryStore.DeleteAsync(ModuleId, HistoryCollection, entryId, linked.Token).ConfigureAwait(false))
                throw new InvalidOperationException("The saved work entry could not be found in the library.");
            _history.RemoveAt(index);
            return Snapshot(_timeProvider.GetUtcNow());
        }
        finally { _stateGate.Release(); }
    }

    public async Task<AutomationWorkTimeSnapshot> ResumePendingAsync(CancellationToken cancellationToken)
    {
        using var linked = CreateOperationToken(cancellationToken);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            if (_active is not null) throw new InvalidOperationException("Stop the active work interval before resuming a pending entry.");
            if (_pending is null) throw new InvalidOperationException("There is no pending work entry to resume.");
            var now = _timeProvider.GetUtcNow();
            var active = new PersistedActive(_pending.Id, _pending.StartedUtc, _pending.DurationMilliseconds, now);
            await PersistStateAsync(active, null, linked.Token).ConfigureAwait(false);
            _active = active;
            _pending = null;
            return Snapshot(now);
        }
        finally { _stateGate.Release(); }
    }

    public async Task<AutomationWorkTimeSnapshot> DiscardPendingAsync(CancellationToken cancellationToken)
    {
        using var linked = CreateOperationToken(cancellationToken);
        await _stateGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            if (_pending is null) throw new InvalidOperationException("There is no pending work entry to discard.");
            await PersistStateAsync(null, null, linked.Token).ConfigureAwait(false);
            _pending = null;
            return Snapshot(_timeProvider.GetUtcNow());
        }
        finally { _stateGate.Release(); }
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

    private AutomationWorkTimeSnapshot Snapshot(DateTimeOffset now) =>
        new(_active is null ? null : ToActiveSnapshot(_active, now), _pending, _history.ToArray());

    private AutomationWorkTimeActive ToActiveSnapshot(PersistedActive active, DateTimeOffset now) =>
        new(active.Id, active.StartedUtc, ElapsedMilliseconds(active, now), now);

    private static long ElapsedMilliseconds(PersistedActive active, DateTimeOffset now)
    {
        var running = Math.Max(0, (long)(now - active.RunningSinceUtc).TotalMilliseconds);
        return checked(active.AccumulatedMilliseconds + running);
    }

    private Task PersistStateAsync(PersistedActive? active, AutomationWorkTimeEntry? pending, CancellationToken cancellationToken) =>
        _libraryStore.UpsertAsync(new AutomationLibraryRecord(ModuleId, StateCollection, CurrentId, RecordSchemaVersion,
            JsonSerializer.SerializeToElement(new PersistedState(active, pending), JsonOptions), _timeProvider.GetUtcNow()), cancellationToken);

    private Task WriteEntryAsync(AutomationWorkTimeEntry entry, CancellationToken cancellationToken) =>
        _libraryStore.UpsertAsync(new AutomationLibraryRecord(ModuleId, HistoryCollection, entry.Id, RecordSchemaVersion,
            JsonSerializer.SerializeToElement(entry, JsonOptions), _timeProvider.GetUtcNow()), cancellationToken);

    private async Task PruneHistoryAsync(CancellationToken cancellationToken)
    {
        if (_history.Count <= MaximumHistoryEntries) return;
        var excess = _history.Count - MaximumHistoryEntries;
        var removed = _history.Take(excess).ToArray();
        foreach (var entry in removed)
            await _libraryStore.DeleteAsync(ModuleId, HistoryCollection, entry.Id, cancellationToken).ConfigureAwait(false);
        _history.RemoveRange(0, excess);
    }

    private static string[] NormalizeTags(IReadOnlyList<string> tags)
    {
        var result = new List<string>(Math.Min(tags.Count, 30));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in tags)
        {
            if (raw is null) continue;
            var tag = raw.Trim();
            if (tag.Length == 0) continue;
            if (tag.Length > 40) throw new InvalidDataException("A work tag can contain at most 40 characters.");
            if (!seen.Add(tag)) continue;
            result.Add(tag);
            if (result.Count > 30) throw new InvalidDataException("A work entry can contain at most 30 tags.");
        }
        return result.ToArray();
    }

    private static void ValidateState(PersistedActive? active, AutomationWorkTimeEntry? pending)
    {
        if (active is not null)
        {
            if (string.IsNullOrWhiteSpace(active.Id) || active.StartedUtc == default || active.RunningSinceUtc == default
                || active.RunningSinceUtc < active.StartedUtc || active.AccumulatedMilliseconds < 0)
                throw new InvalidDataException("The saved active work interval is invalid.");
        }
        if (active is not null && pending is not null)
            throw new InvalidDataException("The saved work-time state cannot have both an active and pending entry.");
        if (pending is not null) ValidateEntry(pending, requireDescription: false);
    }

    private static void ValidateEntry(AutomationWorkTimeEntry entry, bool requireDescription)
    {
        if (string.IsNullOrWhiteSpace(entry.Id) || entry.StartedUtc == default || entry.StoppedUtc < entry.StartedUtc
            || entry.DurationMilliseconds < 0 || entry.Tags is null || entry.Tags.Count > 30
            || entry.Tags.Any(tag => string.IsNullOrWhiteSpace(tag) || tag.Length > 40))
            throw new InvalidDataException("The saved work-time entry is invalid.");
        if (requireDescription && string.IsNullOrWhiteSpace(entry.Description))
            throw new InvalidDataException("A saved work-time entry must have a description.");
        if (entry.Description is null || entry.Description.Length > 500)
            throw new InvalidDataException("The saved work-time description is invalid.");
    }

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
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (Volatile.Read(ref _initialized) == 0)
            throw new InvalidOperationException("The work-time coordinator has not been initialized.");
        return CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
    }

    private sealed record PersistedActive(string Id, DateTimeOffset StartedUtc, long AccumulatedMilliseconds, DateTimeOffset RunningSinceUtc);
    private sealed record PersistedState(PersistedActive? Active, AutomationWorkTimeEntry? Pending);
}
