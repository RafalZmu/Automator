using System.Text.Json;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

public sealed record AutomationWorkflowProfile
{
    public AutomationWorkflowProfile(
        string id,
        string name,
        IReadOnlyList<AutomationWorkflowStep> steps,
        IReadOnlyDictionary<string, JsonElement>? variables = null)
    {
        Id = id;
        Name = name;
        Steps = steps;
        Variables = variables ?? new Dictionary<string, JsonElement>(StringComparer.Ordinal);
    }

    public string Id { get; init; }
    public string Name { get; init; }
    public IReadOnlyList<AutomationWorkflowStep> Steps { get; init; }
    public IReadOnlyDictionary<string, JsonElement> Variables { get; init; }
    public IReadOnlyDictionary<string, string> VariableReferences { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
    public CodexTaskApproval? CodexApproval { get; init; }
}

public sealed record AutomationWorkflowStep(
    string Id,
    string ModuleId,
    string ProfileId,
    IReadOnlyList<AutomationWorkflowInputBinding> Inputs);

public sealed record AutomationWorkflowInputBinding(
    string TargetJsonPointer,
    JsonElement? Literal,
    string? SourceStepId,
    string? SourceJsonPointer,
    bool LiteralPresent = false);

public sealed record AutomationWorkflowStepResult(
    string StepId,
    string ModuleId,
    string ProfileId,
    AutomationStatus Status,
    JsonElement Output,
    AutomationExecutionSummary Summary);

public sealed record AutomationWorkflowRunResult(
    JsonElement Output,
    IReadOnlyList<AutomationWorkflowStepResult> Steps,
    AutomationExecutionSummary Summary);

/// <summary>Scoped UI service for explicit user-run workflows.</summary>
public interface IAutomationWorkflowRunner
{
    Task<IReadOnlyList<AutomationSavedProfileSummary>> ListSavedProfilesAsync(string moduleId, CancellationToken cancellationToken);
    Task<AutomationWorkflowRunResult> RunAsync(string workflowId, JsonElement? initialInput, CancellationToken cancellationToken);
}

/// <summary>Host-only scheduler path; implementations return summaries and never raw step output.</summary>
public interface IAutomationScheduledWorkflowExecutor
{
    Task<AutomationExecutionSummary> RunScheduledAsync(string workflowId, string correlationId, CancellationToken cancellationToken);
}
