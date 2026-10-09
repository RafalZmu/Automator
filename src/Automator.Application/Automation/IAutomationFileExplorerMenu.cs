namespace Automator.Application.Automation;

/// <summary>Host-owned reconciliation of Automator menu entries; callers cannot supply registry keys or commands.</summary>
public interface IAutomationFileExplorerMenu
{
    Task ReconcileAsync(IReadOnlyList<ExplorerActionDefinition> entries, string executablePath, CancellationToken cancellationToken);
}
