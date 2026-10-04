namespace Automator.Application.Automation;

public enum AutomationFocusPhase
{
    Focus,
    Break,
}

public enum AutomationFocusSessionState
{
    Idle,
    Running,
    Paused,
    Interrupted,
}

public sealed record AutomationFocusSettings(int FocusMinutes, int BreakMinutes);

public sealed record AutomationFocusSessionSnapshot(
    AutomationFocusSessionState State,
    string? SessionId,
    AutomationFocusPhase? Phase,
    DateTimeOffset? PhaseEndsAtUtc,
    long? RemainingMilliseconds,
    int CompletedFocusPhases,
    AutomationFocusSettings Settings);

public sealed record AutomationFocusSessionHistoryEntry(
    string Id,
    DateTimeOffset StartedUtc,
    DateTimeOffset FinishedUtc,
    int CompletedFocusPhases,
    AutomationFocusSessionState FinalState,
    long DurationMilliseconds);

public sealed record AutomationFocusSnapshot(
    AutomationFocusSessionSnapshot Session,
    IReadOnlyList<AutomationFocusSessionHistoryEntry> History,
    AutomationFocusSettings? Settings = null);

public sealed record AutomationNotification(string Title, string Body);

/// <summary>Host-owned, typed OS notification boundary shared by modules.</summary>
public interface IAutomationNotificationService
{
    Task ShowAsync(AutomationNotification notification, CancellationToken cancellationToken);
}

/// <summary>Backend-lifetime focus timer; tab deactivation does not dispose its state or worker.</summary>
public interface IAutomationFocusSessionCoordinator
{
    Task<AutomationFocusSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
    Task<AutomationFocusSessionSnapshot> SaveSettingsAsync(AutomationFocusSettings settings, CancellationToken cancellationToken);
    Task<AutomationFocusSessionSnapshot> StartAsync(CancellationToken cancellationToken);
    Task<AutomationFocusSessionSnapshot> PauseAsync(CancellationToken cancellationToken);
    Task<AutomationFocusSessionSnapshot> ResumeAsync(CancellationToken cancellationToken);
    Task<AutomationFocusSessionSnapshot> SkipAsync(CancellationToken cancellationToken);
    Task<AutomationFocusSessionSnapshot> EndAsync(CancellationToken cancellationToken);
}
