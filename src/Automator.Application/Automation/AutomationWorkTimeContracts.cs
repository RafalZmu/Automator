namespace Automator.Application.Automation;

public sealed record AutomationWorkTimeSegment(DateTimeOffset StartedUtc, DateTimeOffset? StoppedUtc);

public sealed record AutomationWorkTimeTimer(
    string Id,
    string Description,
    DateTimeOffset StartedUtc,
    string Status,
    long ElapsedMilliseconds,
    DateTimeOffset SampledUtc,
    IReadOnlyList<AutomationWorkTimeSegment> Segments);

public sealed record AutomationWorkTimeEntry(
    string Id,
    DateTimeOffset StartedUtc,
    DateTimeOffset StoppedUtc,
    long DurationMilliseconds,
    string Description,
    IReadOnlyList<string> Tags,
    IReadOnlyList<AutomationWorkTimeSegment> Segments);

public sealed record AutomationWorkTimeSnapshot(
    IReadOnlyList<AutomationWorkTimeTimer> Timers,
    IReadOnlyList<AutomationWorkTimeEntry> PendingEntries,
    IReadOnlyList<AutomationWorkTimeEntry> History);

/// <summary>Backend-owned named timers and durable work log for slot seven.</summary>
public interface IAutomationWorkTimeCoordinator
{
    Task<AutomationWorkTimeSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
    Task<AutomationWorkTimeSnapshot> StartAsync(string description, CancellationToken cancellationToken);
    Task<AutomationWorkTimeSnapshot> PauseAsync(string id, CancellationToken cancellationToken);
    Task<AutomationWorkTimeSnapshot> ResumeAsync(string id, CancellationToken cancellationToken);
    Task<AutomationWorkTimeSnapshot> EndAsync(string id, CancellationToken cancellationToken);
    Task<AutomationWorkTimeSnapshot> SaveEntryAsync(string id, string description, IReadOnlyList<string> tags, CancellationToken cancellationToken);
    Task<AutomationWorkTimeSnapshot> UpdateEntryAsync(string id, string description, IReadOnlyList<string> tags, CancellationToken cancellationToken);
    Task<AutomationWorkTimeSnapshot> DeleteEntryAsync(string id, CancellationToken cancellationToken);
    Task<AutomationWorkTimeSnapshot> DiscardTimerAsync(string id, CancellationToken cancellationToken);
    Task<AutomationWorkTimeSnapshot> DiscardPendingAsync(string id, CancellationToken cancellationToken);
}
