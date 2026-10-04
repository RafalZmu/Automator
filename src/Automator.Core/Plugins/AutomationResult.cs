namespace Automator.Core.Plugins;

using System.Text.Json;

public enum AutomationStatus
{
    Success,
    Information,
    Warning,
    Error
}

/// <summary>A safe, serializable follow-up action offered by a module result.</summary>
public sealed record AutomationAction(string Id, string Label, int Version, JsonElement Payload);

public sealed record AutomationResult(
    int ContractVersion,
    AutomationStatus Status,
    string Message,
    JsonElement Data,
    IReadOnlyList<AutomationAction> Actions);

public static class AutomationTabContract
{
    public const int CurrentVersion = 1;
}

public interface IAutomationTab
{
    int ContractVersion { get; }
    string Id { get; }
    string Title { get; }
}
