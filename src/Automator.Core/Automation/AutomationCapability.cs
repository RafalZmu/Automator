namespace Automator.Core.Automation;

/// <summary>A versioned service capability declared by a bundled automation module.</summary>
public sealed record AutomationCapabilityRequirement(string Id, int Version);

public static class AutomationCapabilityIds
{
    public const string KeyboardInput = "keyboard.input";
    public const string HttpRequest = "http.request";
    public const string LibraryStorage = "storage.library";
    public const string ProcessExecution = "process.execute";
    public const string SecretManagement = "secrets.manage";
    public const string ApiProfileHttp = "api.profileHttp";
    public const string BrowserSession = "browser.session";
    public const string WorkflowExecution = "workflow.execute";
    public const string SchedulerManagement = "scheduler.manage";
    public const string FocusManagement = "focus.manage";
    public const string WorkTimeManagement = "work-time.management";
    public const string FileExplorerMenu = "file-explorer.menu";
    public const string WebsiteLaunch = "website.launch";
}
