namespace Automator.Application.Automation;

public enum AutomationBrowserActionKind
{
    Navigate,
    Click,
    Fill,
    Select,
    WaitFor,
    ReadTitle,
    ReadText,
    ListLinks,
}

public enum AutomationBrowserLocatorKind
{
    Role,
    Label,
    Placeholder,
    Text,
    TestId,
}

/// <summary>Closed browser action vocabulary; arbitrary code evaluation is intentionally unavailable.</summary>
public sealed record AutomationBrowserAction(
    AutomationBrowserActionKind Kind,
    string? Url = null,
    AutomationBrowserLocatorKind? LocatorKind = null,
    string? Locator = null,
    string? Value = null,
    int TimeoutMilliseconds = 10_000);

public sealed record AutomationBrowserRuntimeStatus(
    bool Installed,
    bool Ready,
    string Message,
    int? ProgressPercent = null);

public sealed record AutomationBrowserActionResult(
    string CurrentUrl,
    string? Title = null,
    string? Text = null,
    IReadOnlyList<AutomationBrowserLink> Links = default!);

public sealed record AutomationBrowserLink(string Text, string Url);

public interface IAutomationBrowserService
{
    Task<AutomationBrowserRuntimeStatus> GetRuntimeStatusAsync(CancellationToken cancellationToken);
    Task<AutomationBrowserActionResult> ExecuteAsync(string profileId, AutomationBrowserAction action, CancellationToken cancellationToken);
    Task CloseSessionAsync(string profileId, CancellationToken cancellationToken);
}
