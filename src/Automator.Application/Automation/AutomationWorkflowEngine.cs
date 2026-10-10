using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

/// <summary>
/// Host-owned workflow execution. It resolves only saved profiles registered by the host and keeps
/// step input/output in memory; durable history contains status/category/duration only.
/// </summary>
public sealed class AutomationWorkflowEngine(
    IAutomationLibraryStore library,
    AutomationSavedProfileExecutor profiles,
    IAutomationVariableProvider? variables = null) : IAutomationWorkflowRunner, IAutomationScheduledWorkflowExecutor
{
    public const string ModuleId = "workflows";
    public const string ProfileCollection = "profiles";
    public const string RunHistoryCollection = "run-history";
    public const int MaximumSteps = 64;
    private const int MaximumRunHistoryEntries = 100;
    private static readonly Regex StableId = new("^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex VariableKey = new("^[A-Za-z_][A-Za-z0-9_.-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly HashSet<string> SupportedProfileModules = new(StringComparer.Ordinal)
    {
        "script-runner", "api", "browser-automation", "playwright-task"
    };

    public async Task<IReadOnlyList<AutomationSavedProfileSummary>> ListSavedProfilesAsync(
        string moduleId,
        CancellationToken cancellationToken)
    {
        if (!SupportedProfileModules.Contains(moduleId))
            throw new InvalidDataException("That module type cannot be used in a workflow.");
        return await profiles.ListProfilesAsync(moduleId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AutomationWorkflowRunResult> RunAsync(
        string workflowId,
        JsonElement? initialInput,
        CancellationToken cancellationToken)
    {
        ValidateWorkflowId(workflowId);
        ValidateJsonSize(initialInput, AutomationSavedProfileExecutor.MaximumStructuredDataBytes, "Workflow input");
        var globals = variables is null ? null : await variables.GetAsync(cancellationToken).ConfigureAwait(false);
        var workflow = await LoadWorkflowAsync(workflowId, cancellationToken).ConfigureAwait(false);
        await ValidateApprovalAsync(workflow, cancellationToken).ConfigureAwait(false);
        return await RunProfileAsync(workflow, globals, initialInput, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AutomationWorkflowRunResult> RunTransientAsync(AutomationWorkflowProfile workflow, JsonElement? initialInput, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        var validation = WorkflowModule.ValidateProfile(workflow);
        if (validation is not null) throw new InvalidDataException(validation);
        foreach (var moduleId in workflow.Steps.Select(step => step.ModuleId).Distinct(StringComparer.Ordinal))
        {
            var available = await profiles.ListProfilesAsync(moduleId, cancellationToken).ConfigureAwait(false);
            var ids = available.Select(profile => profile.ProfileId).ToHashSet(StringComparer.Ordinal);
            if (workflow.Steps.Any(step => step.ModuleId == moduleId && !ids.Contains(step.ProfileId)))
                throw new InvalidDataException($"Workflow refers to a profile that is not currently saved under '{moduleId}'.");
        }
        ValidateJsonSize(initialInput, AutomationSavedProfileExecutor.MaximumStructuredDataBytes, "Workflow input");
        var globals = variables is null ? null : await variables.GetAsync(cancellationToken).ConfigureAwait(false);
        return await RunProfileAsync(workflow, globals, initialInput, cancellationToken).ConfigureAwait(false);
    }

    private async Task<AutomationWorkflowRunResult> RunProfileAsync(AutomationWorkflowProfile workflow, AutomationVariableSnapshot? globals, JsonElement? initialInput, CancellationToken cancellationToken)
    {
        var rootInput = BuildRootInput(workflow, globals, initialInput);
        var startedUtc = DateTimeOffset.UtcNow;
        var timer = Stopwatch.StartNew();
        var correlationId = Guid.NewGuid().ToString("N");
        var stepResults = new List<AutomationWorkflowStepResult>(workflow.Steps.Count);
        var stepOutputs = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        var fallbackInput = rootInput.Clone();
        var finalOutput = EmptyJsonObject();
        var summary = new AutomationExecutionSummary(AutomationStatus.Success, "completed", 0);

        AutomationWorkflowStep? activeStep = null;
        try
        {
            foreach (var step in workflow.Steps)
            {
                activeStep = step;
                cancellationToken.ThrowIfCancellationRequested();
                var stepTimer = Stopwatch.StartNew();
                try
                {
                    var stepInput = MapInput(step, fallbackInput, rootInput, stepOutputs);
                    var profileResult = await profiles.RunAsync(step.ModuleId, step.ProfileId, stepInput,
                        new AutomationExecutionMetadata(AutomationExecutionOrigin.Workflow, correlationId), cancellationToken).ConfigureAwait(false);
                    finalOutput = profileResult.Output.Clone();
                    stepOutputs.Add(step.Id, finalOutput.Clone());
                    stepResults.Add(new AutomationWorkflowStepResult(step.Id, step.ModuleId, step.ProfileId,
                        profileResult.Summary.Status, profileResult.Output.Clone(), profileResult.Summary));
                    fallbackInput = profileResult.Output.Clone();

                    if (profileResult.Summary.Status != AutomationStatus.Success)
                    {
                        summary = new AutomationExecutionSummary(profileResult.Summary.Status,
                            profileResult.Summary.Category, (long)timer.Elapsed.TotalMilliseconds);
                        break;
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException
                    or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or JsonException)
                {
                    var failure = new AutomationExecutionSummary(AutomationStatus.Error, "step-execution-failed",
                        (long)stepTimer.Elapsed.TotalMilliseconds);
                    stepResults.Add(new AutomationWorkflowStepResult(step.Id, step.ModuleId, step.ProfileId,
                        AutomationStatus.Error, EmptyJsonObject(), failure));
                    summary = new AutomationExecutionSummary(AutomationStatus.Error, failure.Category,
                        (long)timer.Elapsed.TotalMilliseconds);
                    break;
                }
                activeStep = null;
            }
        }
        catch (OperationCanceledException)
        {
            timer.Stop();
            if (activeStep is not null && stepResults.All(result => result.StepId != activeStep.Id))
            {
                var canceledStep = new AutomationExecutionSummary(AutomationStatus.Warning, "canceled", 0);
                stepResults.Add(new AutomationWorkflowStepResult(activeStep.Id, activeStep.ModuleId, activeStep.ProfileId,
                    AutomationStatus.Warning, EmptyJsonObject(), canceledStep));
            }
            summary = new AutomationExecutionSummary(AutomationStatus.Warning, "canceled", (long)timer.Elapsed.TotalMilliseconds);
            var canceledResult = new AutomationWorkflowRunResult(EmptyJsonObject(), Array.AsReadOnly(stepResults.ToArray()), summary);
            await PersistHistoryAsync(workflow, canceledResult, startedUtc, DateTimeOffset.UtcNow,
                AutomationExecutionOrigin.Manual, correlationId, CancellationToken.None).ConfigureAwait(false);
            throw;
        }

        timer.Stop();
        summary = summary with { DurationMilliseconds = (long)timer.Elapsed.TotalMilliseconds };
        var result = new AutomationWorkflowRunResult(finalOutput, Array.AsReadOnly(stepResults.ToArray()), summary);
        await PersistHistoryAsync(workflow, result, startedUtc, DateTimeOffset.UtcNow,
            AutomationExecutionOrigin.Manual, correlationId, CancellationToken.None).ConfigureAwait(false);
        return result;
    }

    public async Task<AutomationExecutionSummary> RunScheduledAsync(
        string workflowId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 128 || correlationId.Any(char.IsControl))
                throw new InvalidDataException("The workflow correlation id is invalid.");
            ValidateWorkflowId(workflowId);
            var globals = variables is null ? null : await variables.GetAsync(cancellationToken).ConfigureAwait(false);
            var workflow = await LoadWorkflowAsync(workflowId, cancellationToken).ConfigureAwait(false);
            await ValidateApprovalAsync(workflow, cancellationToken).ConfigureAwait(false);
            var rootInput = BuildRootInput(workflow, globals, null);
            var startedUtc = DateTimeOffset.UtcNow;
            var timer = Stopwatch.StartNew();
            var stepOutputs = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            var scheduledSteps = new List<AutomationWorkflowStepResult>(workflow.Steps.Count);
            JsonElement? fallbackInput = rootInput.Clone();
            var summary = new AutomationExecutionSummary(AutomationStatus.Success, "completed", 0);
            foreach (var step in workflow.Steps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var input = MapInput(step, fallbackInput, rootInput, stepOutputs);
                    var output = await profiles.RunAsync(step.ModuleId, step.ProfileId, input,
                        new AutomationExecutionMetadata(AutomationExecutionOrigin.Scheduled, correlationId), cancellationToken).ConfigureAwait(false);
                    stepOutputs.Add(step.Id, output.Output.Clone());
                    fallbackInput = output.Output.Clone();
                    scheduledSteps.Add(new AutomationWorkflowStepResult(step.Id, step.ModuleId, step.ProfileId,
                        output.Summary.Status, EmptyJsonObject(), output.Summary));
                    if (output.Summary.Status != AutomationStatus.Success)
                    {
                        summary = output.Summary with { DurationMilliseconds = (long)timer.Elapsed.TotalMilliseconds };
                        break;
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException
                    or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or JsonException)
                {
                    summary = new AutomationExecutionSummary(AutomationStatus.Error, "step-execution-failed",
                        (long)timer.Elapsed.TotalMilliseconds);
                    var failed = new AutomationExecutionSummary(AutomationStatus.Error, "step-execution-failed",
                        (long)timer.Elapsed.TotalMilliseconds);
                    scheduledSteps.Add(new AutomationWorkflowStepResult(step.Id, step.ModuleId, step.ProfileId,
                        AutomationStatus.Error, EmptyJsonObject(), failed));
                    break;
                }
            }

            timer.Stop();
            summary = summary with { DurationMilliseconds = (long)timer.Elapsed.TotalMilliseconds };
            var safeHistory = new AutomationWorkflowRunResult(EmptyJsonObject(), Array.AsReadOnly(scheduledSteps.ToArray()), summary);
            await PersistHistoryAsync(workflow, safeHistory, startedUtc, DateTimeOffset.UtcNow,
                AutomationExecutionOrigin.Scheduled, correlationId, cancellationToken).ConfigureAwait(false);
            return summary;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException
            or IOException or UnauthorizedAccessException or JsonException)
        {
            return new AutomationExecutionSummary(AutomationStatus.Error, "workflow-unavailable", 0);
        }
    }

    private async Task<AutomationWorkflowProfile> LoadWorkflowAsync(string id, CancellationToken cancellationToken)
    {
        var record = await library.GetAsync(ModuleId, ProfileCollection, id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("The saved workflow no longer exists.");
        if (record.SchemaVersion != 1) throw new InvalidDataException("The saved workflow version is not supported.");
        var workflow = JsonSerializer.Deserialize<AutomationWorkflowProfile>(record.Data.GetRawText(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("The saved workflow is invalid.");
        if (!string.Equals(workflow.Id, id, StringComparison.Ordinal))
            throw new InvalidDataException("The saved workflow id does not match its library key.");
        workflow = workflow with { Variables = workflow.Variables ?? EmptyVariables() };
        ValidateWorkflow(workflow);
        return workflow;
    }

    private static async Task ValidateApprovalAsync(AutomationWorkflowProfile workflow, CancellationToken cancellationToken)
    {
        if (workflow.CodexApproval is not { } approval) return;
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(workflow with { CodexApproval = null }));
        var revision = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
        if (!string.Equals(revision, approval.SourceHash, StringComparison.Ordinal) || approval.Scope is null
            || !string.Equals(ScriptRunnerExecution.HashReview(approval.Scope, approval.Inputs, approval.Effects), approval.ScopeHash, StringComparison.Ordinal))
            throw new InvalidDataException("This generated workflow changed after approval. Review and approve it before running.");
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task PersistHistoryAsync(
        AutomationWorkflowProfile workflow,
        AutomationWorkflowRunResult result,
        DateTimeOffset startedUtc,
        DateTimeOffset finishedUtc,
        AutomationExecutionOrigin origin,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var history = new
        {
            workflowId = workflow.Id,
            origin,
            correlationId,
            startedUtc,
            finishedUtc,
            summary = result.Summary,
            steps = result.Steps.Select(step => new
            {
                stepId = step.StepId,
                moduleId = step.ModuleId,
                profileId = step.ProfileId,
                status = step.Summary.Status,
                category = step.Summary.Category,
                durationMilliseconds = step.Summary.DurationMilliseconds
            }).ToArray()
        };
        await library.UpsertAsync(new AutomationLibraryRecord(ModuleId, RunHistoryCollection, correlationId, 1,
            JsonSerializer.SerializeToElement(history), finishedUtc), cancellationToken).ConfigureAwait(false);

        var existing = await library.ListAsync(ModuleId, RunHistoryCollection, cancellationToken).ConfigureAwait(false);
        foreach (var oldEntry in existing.OrderByDescending(entry => entry.UpdatedUtc).Skip(MaximumRunHistoryEntries))
            await library.DeleteAsync(ModuleId, RunHistoryCollection, oldEntry.Id, cancellationToken).ConfigureAwait(false);
    }

    private static JsonElement? MapInput(
        AutomationWorkflowStep step,
        JsonElement? fallback,
        JsonElement? initialInput,
        IReadOnlyDictionary<string, JsonElement> completedSteps)
    {
        if (step.Inputs.Count == 0) return fallback?.Clone();
        var destination = new JsonObject();
        foreach (var binding in step.Inputs)
        {
            JsonElement source;
            if (binding.LiteralPresent)
            {
                source = binding.Literal is { } literal ? literal.Clone() : JsonNull();
            }
            else if (binding.SourceStepId == "$input")
            {
                source = initialInput ?? throw new InvalidDataException("Workflow input is unavailable for a mapped value.");
                if (binding.SourceJsonPointer is { Length: > 0 } inputPointer)
                    source = ResolvePointer(source, inputPointer);
            }
            else
            {
                if (binding.SourceStepId is null || !completedSteps.TryGetValue(binding.SourceStepId, out source))
                    throw new InvalidDataException("A workflow step references output that is not available yet.");
                if (binding.SourceJsonPointer is { Length: > 0 } outputPointer)
                    source = ResolvePointer(source, outputPointer);
            }

            SetObjectPath(destination, ParsePointer(binding.TargetJsonPointer), JsonNode.Parse(source.GetRawText()));
        }
        var json = destination.ToJsonString();
        ValidateJsonTextSize(json, AutomationSavedProfileExecutor.MaximumStructuredDataBytes, "Mapped workflow input");
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static void ValidateWorkflow(AutomationWorkflowProfile workflow)
    {
        ValidateWorkflowId(workflow.Id);
        AutomationVariableService.ValidateReferences(workflow.VariableReferences);
        if (string.IsNullOrWhiteSpace(workflow.Name) || workflow.Name.Length > 128 || workflow.Name.Any(char.IsControl))
            throw new InvalidDataException("Workflow name is invalid.");
        var variables = workflow.Variables ?? EmptyVariables();
        if (variables.Count > 64) throw new InvalidDataException("A workflow can contain at most 64 variables.");
        foreach (var (key, value) in variables)
        {
            if (!VariableKey.IsMatch(key)) throw new InvalidDataException("A workflow variable key is invalid.");
            if (value.ValueKind == JsonValueKind.Undefined)
                throw new InvalidDataException($"Workflow variable '{key}' is invalid.");
            ValidateJsonTextSize(value.GetRawText(), 48 * 1024, $"Workflow variable '{key}'");
        }
        ValidateJsonSize(JsonSerializer.SerializeToElement(variables), 48 * 1024, "Workflow variables");
        if (workflow.Steps is null || workflow.Steps.Count is < 1 or > MaximumSteps)
            throw new InvalidDataException($"A workflow must contain between 1 and {MaximumSteps} steps.");

        var seenSteps = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in workflow.Steps)
        {
            if (step is null || !StableId.IsMatch(step.Id) || seenSteps.Contains(step.Id))
                throw new InvalidDataException("Workflow step ids must be unique valid ids.");
            if (!SupportedProfileModules.Contains(step.ModuleId) || !StableId.IsMatch(step.ProfileId))
                throw new InvalidDataException("A workflow step references an unsupported saved profile.");
            if (step.Inputs is null || step.Inputs.Count > 128)
                throw new InvalidDataException("A workflow step has too many input mappings.");
            var targets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in step.Inputs)
            {
                if (binding is null || !targets.Add(binding.TargetJsonPointer))
                    throw new InvalidDataException("Workflow input mappings contain duplicate destinations.");
                _ = ParsePointer(binding.TargetJsonPointer, isTarget: true);
                if (binding.LiteralPresent == (binding.SourceStepId is not null))
                    throw new InvalidDataException("Each workflow mapping must use either a literal or one source.");
                if (binding.LiteralPresent)
                {
                    if (binding.Literal is { ValueKind: JsonValueKind.Undefined })
                        throw new InvalidDataException("A workflow literal is invalid.");
                    continue;
                }
                if (binding.SourceStepId == "$input")
                {
                    if (binding.SourceJsonPointer is { Length: > 0 } inputPointer) _ = ParsePointer(inputPointer);
                    continue;
                }
                if (binding.SourceStepId is null || !seenSteps.Contains(binding.SourceStepId))
                    throw new InvalidDataException("Workflow mappings can reference only earlier steps or initial input.");
                if (binding.SourceJsonPointer is { Length: > 0 } outputPointer) _ = ParsePointer(outputPointer);
            }
            seenSteps.Add(step.Id);
        }
    }

    private static string[] ParsePointer(string pointer, bool isTarget = false)
    {
        if (string.IsNullOrEmpty(pointer))
        {
            if (isTarget) throw new InvalidDataException("Workflow destinations must name an object property.");
            return [];
        }
        if (!pointer.StartsWith("/", StringComparison.Ordinal))
            throw new InvalidDataException("Workflow JSON pointers must start with '/'.");
        var segments = pointer[1..].Split('/');
        for (var index = 0; index < segments.Length; index++)
        {
            if (segments[index].Length == 0) throw new InvalidDataException("Workflow JSON pointers cannot contain empty path segments.");
            var builder = new StringBuilder(segments[index].Length);
            for (var position = 0; position < segments[index].Length; position++)
            {
                var character = segments[index][position];
                if (character != '~')
                {
                    builder.Append(character);
                    continue;
                }
                if (++position >= segments[index].Length || segments[index][position] is not ('0' or '1'))
                    throw new InvalidDataException("Workflow JSON pointer escaping is invalid.");
                builder.Append(segments[index][position] == '0' ? '~' : '/');
            }
            segments[index] = builder.ToString();
        }
        return segments;
    }

    private static JsonElement ResolvePointer(JsonElement root, string pointer)
    {
        var current = root;
        foreach (var segment in ParsePointer(pointer))
        {
            if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(segment, out var property))
            {
                current = property;
                continue;
            }
            if (current.ValueKind == JsonValueKind.Array && int.TryParse(segment, out var index)
                && index >= 0 && index < current.GetArrayLength())
            {
                current = current[index];
                continue;
            }
            throw new InvalidDataException("A workflow input mapping source does not exist.");
        }
        return current.Clone();
    }

    private static void SetObjectPath(JsonObject root, IReadOnlyList<string> path, JsonNode? value)
    {
        var current = root;
        for (var index = 0; index < path.Count - 1; index++)
        {
            var segment = path[index];
            if (!current.ContainsKey(segment)) current[segment] = new JsonObject();
            if (current[segment] is not JsonObject nested)
                throw new InvalidDataException("Workflow input mappings overlap at an incompatible path.");
            current = nested;
        }
        var leaf = path[^1];
        if (current.ContainsKey(leaf)) throw new InvalidDataException("Workflow input mappings overlap at an incompatible path.");
        current[leaf] = value;
    }

    private static void ValidateWorkflowId(string id)
    {
        if (!StableId.IsMatch(id)) throw new InvalidDataException("Workflow id is invalid.");
    }

    private static void ValidateJsonSize(JsonElement? value, int limit, string label)
    {
        if (value is { } element)
        {
            if (element.ValueKind == JsonValueKind.Undefined)
                throw new InvalidDataException($"{label} is invalid.");
            ValidateJsonTextSize(element.GetRawText(), limit, label);
        }
    }

    private static void ValidateJsonTextSize(string value, int limit, string label)
    {
        if (Encoding.UTF8.GetByteCount(value) > limit) throw new InvalidDataException($"{label} is too large.");
    }

    private static JsonElement EmptyJsonObject()
    {
        using var document = JsonDocument.Parse("{}");
        return document.RootElement.Clone();
    }

    private static JsonElement JsonNull()
    {
        using var document = JsonDocument.Parse("null");
        return document.RootElement.Clone();
    }

    private static IReadOnlyDictionary<string, JsonElement> EmptyVariables() =>
        new Dictionary<string, JsonElement>(StringComparer.Ordinal);

    private static JsonElement BuildRootInput(AutomationWorkflowProfile workflow, AutomationVariableSnapshot? globals, JsonElement? legacyInput)
    {
        // Legacy callers may still send per-run JSON. Object properties override saved variables;
        // a legacy scalar or array replaces the root to preserve its old JSON-pointer semantics.
        if (legacyInput is { ValueKind: not JsonValueKind.Object } nonObjectInput) return nonObjectInput.Clone();

        var root = new JsonObject();
        if (globals is not null)
        {
            foreach (var (key, value) in globals.Values) root[key] = JsonNode.Parse(value.GetRawText());
            root["variables"] = JsonSerializer.SerializeToNode(globals.Values);
            foreach (var (key, name) in workflow.VariableReferences)
                if (globals.Values.TryGetValue(name, out var value)) root[key] = JsonNode.Parse(value.GetRawText());
                else throw new InvalidDataException("A migrated workflow variable is missing. Check Variables in Options.");
        }
        if (workflow.Variables is not null)
        {
            foreach (var (key, value) in workflow.Variables)
                root[key] = JsonNode.Parse(value.GetRawText());
        }
        if (legacyInput is { ValueKind: JsonValueKind.Object } objectInput)
        {
            foreach (var property in objectInput.EnumerateObject())
                root[property.Name] = JsonNode.Parse(property.Value.GetRawText());
        }

        var json = root.ToJsonString();
        ValidateJsonTextSize(json, AutomationSavedProfileExecutor.MaximumStructuredDataBytes, "Workflow root input");
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
