namespace Automator.Application.Automation;

public sealed record AutomationWorkTimeActive(
    string Id,
    DateTimeOffset StartedUtc,
    long ElapsedMilliseconds,
    DateTimeOffset SampledUtc);

public sealed record AutomationWorkTimeEntry(
    string Id,
    DateTimeOffset StartedUtc,
    DateTimeOffset StoppedUtc,
    long DurationMilliseconds,
    string Description,
    IReadOnlyList<string> Tags);

public sealed record AutomationWorkTimeSnapshot(
    AutomationWorkTimeActive? Active,
    AutomationWorkTimeEntry? Pending,
    IReadOnlyList<AutomationWorkTimeEntry> History);

/// <summary>Backend-owned work timer and durable work log for slot seven.</summary>
public interface IAutomationWorkTimeCoordinator
{
    Task<AutomationWorkTimeSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
    Task<AutomationWorkTimeSnapshot> StartAsync(CancellationToken cancellationToken);
    Task<AutomationWorkTimeSnapshot> StopAsync(CancellationToken cancellationToken);
    Task<AutomationWorkTimeSnapshot> SaveEntryAsync(string description, IReadOnlyList<string> tags, CancellationToken cancellationToken);
    Task<AutomationWorkTimeSnapshot> UpdateEntryAsync(string id, string description, IReadOnlyList<string> tags, CancellationToken cancellationToken);
    Task<AutomationWorkTimeSnapshot> DeleteEntryAsync(string id, CancellationToken cancellationToken);
    Task<AutomationWorkTimeSnapshot> ResumePendingAsync(CancellationToken cancellationToken);
    Task<AutomationWorkTimeSnapshot> DiscardPendingAsync(CancellationToken cancellationToken);
}
