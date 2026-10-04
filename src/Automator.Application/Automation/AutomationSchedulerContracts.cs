using Automator.Core.Plugins;

namespace Automator.Application.Automation;

public enum AutomationScheduleRecurrenceKind
{
    Interval,
    Daily,
    Weekly,
}

public enum AutomationScheduleTargetKind
{
    Workflow,
    ScriptProfile,
}

public sealed record AutomationScheduleRecurrence(
    AutomationScheduleRecurrenceKind Kind,
    int? IntervalMinutes = null,
    TimeOnly? LocalTime = null,
    IReadOnlyList<DayOfWeek>? DaysOfWeek = null,
    DateTimeOffset? IntervalAnchorUtc = null);

public sealed record AutomationScheduleDefinition(
    string Id,
    string Name,
    string WorkflowId,
    bool Enabled,
    AutomationScheduleRecurrence Recurrence,
    bool RunOnceAfterRestart,
    AutomationScheduleTargetKind TargetKind = AutomationScheduleTargetKind.Workflow,
    string? ProfileId = null)
{
    /// <summary>Old schedule records contained only WorkflowId; new records use ProfileId and TargetKind.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string EffectiveProfileId => ProfileId ?? WorkflowId;
}

public sealed record AutomationScheduleHistoryEntry(
    string Id,
    string ScheduleId,
    DateTimeOffset StartedUtc,
    DateTimeOffset? FinishedUtc,
    AutomationStatus Status,
    string Category,
    long DurationMilliseconds);

public sealed record AutomationScheduleState(
    string ScheduleId,
    DateTimeOffset? NextRunAtUtc,
    bool Running);

public sealed record AutomationSchedulerSnapshot(
    IReadOnlyList<AutomationScheduleDefinition> Schedules,
    IReadOnlyList<AutomationScheduleHistoryEntry> History,
    bool Running,
    IReadOnlyList<AutomationScheduleState>? States = null,
    IReadOnlyList<AutomationSavedProfileSummary>? Workflows = null,
    IReadOnlyList<AutomationSavedProfileSummary>? Scripts = null);

public interface IAutomationSchedulerCoordinator
{
    Task<AutomationSchedulerSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
    Task SaveAsync(AutomationScheduleDefinition schedule, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string scheduleId, CancellationToken cancellationToken);
    Task SetEnabledAsync(string scheduleId, bool enabled, CancellationToken cancellationToken);
    Task<AutomationScheduleHistoryEntry> RunNowAsync(string scheduleId, CancellationToken cancellationToken);
}
