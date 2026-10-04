using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Automator.Application.Launcher;
using Automator.Core.Automation;
using Automator.Core.Configuration;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

/// <summary>Ordered saved-profile workflows for slot 5.</summary>
public sealed class WorkflowModule : ILauncherTabModuleProvider
{
    public const string IdValue = "workflows";
    public const int ContractVersionValue = 1;
    public const int SettingsVersionValue = 1;
    public const int LibraryRecordSchemaVersion = 1;
    public const string WorkflowCollection = "profiles";
    private const int MaximumSteps = 32;
    private const int MaximumBindingsPerStep = 64;
    private const int MaximumWorkflowInputBytes = 48 * 1024;
    private const int MaximumUiResultBytes = 480 * 1024;

    private static readonly Regex WorkflowIdPattern = new("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex StepIdPattern = new("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex ProfileIdPattern = new("^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex VariableKeyPattern = new("^[A-Za-z_][A-Za-z0-9_.-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly HashSet<string> SupportedProfileModules = new(StringComparer.Ordinal)
        { "script-runner", "api", "browser-automation" };
    private static readonly AutomationCapabilityRequirement LibraryCapability = new(AutomationCapabilityIds.LibraryStorage, 1);
    private static readonly AutomationCapabilityRequirement WorkflowCapability = new(AutomationCapabilityIds.WorkflowExecution, 1);
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public AutomationModuleDefinition Definition { get; } = new(
        5,
        IdValue,
        "Workflows",
        "workflow",
        "workflows",
        false,
        ContractVersionValue,
        SettingsVersionValue,
        [LibraryCapability, WorkflowCapability],
        [
            new("listWorkflows", 1, [LibraryCapability]),
            new("getWorkflow", 1, [LibraryCapability]),
            new("listSavedProfiles", 1, [WorkflowCapability]),
            new("saveWorkflow", 1, [LibraryCapability]),
            new("deleteWorkflow", 1, [LibraryCapability]),
            new("runWorkflow", 1, [LibraryCapability, WorkflowCapability]),
        ]);

    public int ContractVersion => Definition.ContractVersion;
    public string Id => Definition.Id;
    public string Title => Definition.Title;

    public LauncherModuleState CreateInitialState() =>
        new(Id, LauncherTabRegistry.Version, new Dictionary<string, string>(StringComparer.Ordinal) { ["status"] = "ready" });

    public JsonElement CreateDefaultSettings() => JsonSerializer.SerializeToElement(new { }, JsonOptions);

    public JsonElement MigrateSettings(int fromVersion, JsonElement value) =>
        throw new InvalidOperationException($"Module '{Id}' does not support settings migration from version {fromVersion}.");

    public async ValueTask<AutomationResult> ExecuteAsync(
        string actionId,
        JsonElement input,
        JsonElement moduleSettings,
        AutomationServicesContext services,
        CancellationToken cancellationToken)
    {
        try
        {
            return actionId switch
            {
                "listWorkflows" => await ListWorkflowsAsync(services, cancellationToken).ConfigureAwait(false),
                "getWorkflow" => await GetWorkflowAsync(input, services, cancellationToken).ConfigureAwait(false),
                "listSavedProfiles" => await ListSavedProfilesAsync(input, services, cancellationToken).ConfigureAwait(false),
                "saveWorkflow" => await SaveWorkflowAsync(input, services, cancellationToken).ConfigureAwait(false),
                "deleteWorkflow" => await DeleteWorkflowAsync(input, services, cancellationToken).ConfigureAwait(false),
                "runWorkflow" => await RunWorkflowAsync(input, services, cancellationToken).ConfigureAwait(false),
                _ => Error("Unknown workflow action.")
            };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException
            or IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            return Error(exception.Message);
        }
    }

    private static async Task<AutomationResult> ListWorkflowsAsync(
        AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var records = await services.Library!.ListAsync(WorkflowCollection, cancellationToken).ConfigureAwait(false);
        var workflows = new List<object>();
        foreach (var record in records)
        {
            if (record.SchemaVersion != LibraryRecordSchemaVersion) continue;
            var profile = DeserializeProfile(record.Data);
            if (profile is null || !string.Equals(profile.Id, record.Id, StringComparison.Ordinal) || Validate(profile) is not null) continue;
            workflows.Add(new { id = profile.Id, name = profile.Name, stepCount = profile.Steps.Count });
            if (workflows.Count == 256) break;
        }
        return Result(AutomationStatus.Success, $"{workflows.Count} saved workflow{(workflows.Count == 1 ? string.Empty : "s")}.",
            new { workflows });
    }

    private static async Task<AutomationResult> GetWorkflowAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var id = ReadWorkflowId(input);
        var record = await services.Library!.GetAsync(WorkflowCollection, id, cancellationToken).ConfigureAwait(false);
        if (record is null || record.SchemaVersion != LibraryRecordSchemaVersion)
            return Error("The selected workflow no longer exists or uses an unsupported schema.");
        var profile = DeserializeProfile(record.Data);
        var validation = profile is null || !string.Equals(profile.Id, id, StringComparison.Ordinal)
            ? "The saved workflow is invalid."
            : Validate(profile);
        return validation is not null
            ? Error(validation)
            : Result(AutomationStatus.Success, "Workflow loaded.", new { workflow = profile });
    }

    private static async Task<AutomationResult> SaveWorkflowAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var profile = DeserializeProfile(input)
            ?? throw new InvalidDataException("The workflow is empty or invalid.");
        profile = NormalizeProfile(profile);
        var validation = Validate(profile);
        if (validation is not null) throw new InvalidDataException(validation);

        await services.Library!.UpsertAsync(WorkflowCollection, profile.Id, LibraryRecordSchemaVersion,
            JsonSerializer.SerializeToElement(profile, JsonOptions), cancellationToken).ConfigureAwait(false);
        return Result(AutomationStatus.Success, $"Saved {profile.Name}.", new { workflow = profile });
    }

    private static async Task<AutomationResult> ListSavedProfilesAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        if (input.ValueKind != JsonValueKind.Object || !input.TryGetProperty("moduleId", out var value)
            || value.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("A saved-profile module ID is required.");
        var moduleId = value.GetString()!;
        if (!SupportedProfileModules.Contains(moduleId))
            throw new InvalidDataException("The selected module does not provide workflow profiles.");

        var profiles = await services.Workflows!.ListSavedProfilesAsync(moduleId, cancellationToken).ConfigureAwait(false);
        return Result(AutomationStatus.Success, $"{profiles.Count} saved profile{(profiles.Count == 1 ? string.Empty : "s")}.",
            new { moduleId, profiles = profiles.Select(profile => new { profileId = profile.ProfileId, name = profile.Name }).ToArray() });
    }

    private static async Task<AutomationResult> DeleteWorkflowAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var id = ReadWorkflowId(input);
        var deleted = await services.Library!.DeleteAsync(WorkflowCollection, id, cancellationToken).ConfigureAwait(false);
        return Result(deleted ? AutomationStatus.Success : AutomationStatus.Information,
            deleted ? "Workflow removed." : "Workflow was already removed.", new { id, deleted });
    }

    private static async Task<AutomationResult> RunWorkflowAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var workflowId = ReadWorkflowId(input);
        var initialInput = ReadInitialInput(input);
        var record = await services.Library!.GetAsync(WorkflowCollection, workflowId, cancellationToken).ConfigureAwait(false);
        if (record is null || record.SchemaVersion != LibraryRecordSchemaVersion)
            return Error("The selected workflow no longer exists or uses an unsupported schema.");

        var profile = DeserializeProfile(record.Data);
        var validation = profile is null || !string.Equals(profile.Id, workflowId, StringComparison.Ordinal)
            ? "The saved workflow is invalid."
            : Validate(profile);
        if (validation is not null) return Error(validation);

        // The host runner reloads the saved record and calls fixed profile handlers. Renderer input
        // supplies only optional JSON data; it cannot supply steps, actions, capabilities, or grants.
        var run = await services.Workflows!.RunAsync(workflowId, initialInput, cancellationToken).ConfigureAwait(false);
        var payload = CreateUiPayload(workflowId, run);
        var serialized = JsonSerializer.SerializeToElement(payload, JsonOptions);
        if (Encoding.UTF8.GetByteCount(serialized.GetRawText()) > MaximumUiResultBytes)
        {
            payload = CreateSummaryOnlyPayload(workflowId, run);
            serialized = JsonSerializer.SerializeToElement(payload, JsonOptions);
        }

        var status = run.Summary.Status;
        var message = status switch
        {
            AutomationStatus.Success => "Workflow completed.",
            AutomationStatus.Information => "Workflow finished.",
            AutomationStatus.Warning => "Workflow finished with warnings.",
            _ => $"Workflow stopped ({run.Summary.Category})."
        };
        if (Encoding.UTF8.GetByteCount(serialized.GetRawText()) > 512 * 1024)
        {
            serialized = JsonSerializer.SerializeToElement(new
            {
                workflowId,
                summary = run.Summary,
                outputOmitted = true,
                steps = run.Steps.Select(step => new
                {
                    stepId = step.StepId,
                    moduleId = step.ModuleId,
                    profileId = step.ProfileId,
                    status = step.Status,
                    summary = step.Summary,
                    outputOmitted = true,
                }).ToArray()
            }, JsonOptions);
        }

        var resultMessage = payload.OutputOmitted ? $"{message} Structured output was omitted because it exceeded the display limit." : message;
        return new AutomationResult(AutomationTabContract.CurrentVersion, status, resultMessage, serialized, []);
    }

    private static WorkflowUiPayload CreateUiPayload(string workflowId, AutomationWorkflowRunResult run) => new(
        workflowId,
        run.Output,
        run.Steps.Select(step => new WorkflowUiStep(
            step.StepId, step.ModuleId, step.ProfileId, step.Status, step.Output, step.Summary, false)).ToArray(),
        run.Summary,
        false);

    private static WorkflowUiPayload CreateSummaryOnlyPayload(string workflowId, AutomationWorkflowRunResult run) => new(
        workflowId,
        JsonSerializer.SerializeToElement<object?>(null, JsonOptions),
        run.Steps.Select(step => new WorkflowUiStep(
            step.StepId, step.ModuleId, step.ProfileId, step.Status,
            JsonSerializer.SerializeToElement<object?>(null, JsonOptions), step.Summary, true)).ToArray(),
        run.Summary,
        true);

    /// <summary>Validates a saved workflow before execution or library import.</summary>
    public static string? ValidateProfile(AutomationWorkflowProfile profile) => Validate(profile);

    private static string? Validate(AutomationWorkflowProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Id) || !WorkflowIdPattern.IsMatch(profile.Id))
            return "Workflow ID must be a lowercase key using letters, numbers, dots, dashes, or underscores.";
        if (string.IsNullOrWhiteSpace(profile.Name) || profile.Name.Length > 128)
            return "Workflow name is required and must be at most 128 characters.";
        try { AutomationVariableService.ValidateReferences(profile.VariableReferences); }
        catch (InvalidDataException) { return "Workflow variable references are invalid."; }
        var variables = profile.Variables ?? EmptyVariables();
        if (variables.Count > 64) return "A workflow can contain at most 64 variables.";
        foreach (var (key, value) in variables)
        {
            if (string.IsNullOrWhiteSpace(key) || !VariableKeyPattern.IsMatch(key))
                return $"Workflow variable key '{key}' must start with a letter or underscore and contain only letters, numbers, dots, dashes, or underscores.";
            if (value.ValueKind == JsonValueKind.Undefined)
                return $"Workflow variable '{key}' must contain a JSON value.";
            if (Encoding.UTF8.GetByteCount(value.GetRawText()) > MaximumWorkflowInputBytes)
                return $"Workflow variable '{key}' is too large.";
        }
        if (profile.Steps is null || profile.Steps.Count is < 1 or > MaximumSteps)
            return $"A workflow must contain between 1 and {MaximumSteps} steps.";

        var priorStepIds = new HashSet<string>(StringComparer.Ordinal);
        var stepIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in profile.Steps)
        {
            if (step is null || string.IsNullOrWhiteSpace(step.Id) || !StepIdPattern.IsMatch(step.Id))
                return "Every step needs a valid lowercase step ID.";
            if (!stepIds.Add(step.Id)) return $"Duplicate step ID '{step.Id}'.";
            if (!SupportedProfileModules.Contains(step.ModuleId))
                return $"Step '{step.Id}' uses an unsupported saved-profile module.";
            if (string.IsNullOrWhiteSpace(step.ProfileId) || !ProfileIdPattern.IsMatch(step.ProfileId))
                return $"Step '{step.Id}' needs a valid saved profile ID.";
            if (step.Inputs is null || step.Inputs.Count > MaximumBindingsPerStep)
                return $"Step '{step.Id}' has too many input mappings.";

            var destinationPointers = new List<string[]>();
            foreach (var binding in step.Inputs)
            {
                if (binding is null || !TryParseJsonPointer(binding.TargetJsonPointer, allowRoot: false, out var targetSegments))
                    return $"Step '{step.Id}' has an invalid destination JSON pointer.";

                var hasLiteral = binding.LiteralPresent;
                var hasSource = binding.SourceStepId is not null || binding.SourceJsonPointer is not null;
                if (hasLiteral == hasSource)
                    return "Each input mapping must contain exactly one literal or earlier-step source.";
                if (hasLiteral && Encoding.UTF8.GetByteCount(binding.Literal?.GetRawText() ?? "null") > MaximumWorkflowInputBytes)
                    return "An input mapping literal is too large.";
                if (hasSource)
                {
                    if (string.IsNullOrWhiteSpace(binding.SourceStepId)
                        || (binding.SourceStepId != "$input" && !priorStepIds.Contains(binding.SourceStepId)))
                        return "Input mappings can only read initial input or output from an earlier step.";
                    if (!TryParseJsonPointer(binding.SourceJsonPointer, allowRoot: true, out _))
                        return $"Step '{step.Id}' has an invalid source JSON pointer.";
                }

                if (destinationPointers.Any(prior => IsPrefix(prior, targetSegments) || IsPrefix(targetSegments, prior)))
                    return $"Step '{step.Id}' has conflicting destination JSON pointers.";
                destinationPointers.Add(targetSegments);
            }

            priorStepIds.Add(step.Id);
        }

        var profileData = JsonSerializer.SerializeToElement(profile, JsonOptions);
        return Encoding.UTF8.GetByteCount(profileData.GetRawText()) <= MaximumWorkflowInputBytes
            ? null
            : "A saved workflow exceeds the 48 KiB limit.";
    }

    private static bool TryParseJsonPointer(string? pointer, bool allowRoot, out string[] segments)
    {
        segments = [];
        if (pointer is null || pointer.Length > 4096) return false;
        if (pointer.Length == 0) return allowRoot;
        if (pointer[0] != '/') return false;

        var rawSegments = pointer[1..].Split('/');
        var result = new string[rawSegments.Length];
        for (var segmentIndex = 0; segmentIndex < rawSegments.Length; segmentIndex++)
        {
            var current = rawSegments[segmentIndex];
            if (current.Length == 0) return false;
            var builder = new StringBuilder(current.Length);
            for (var i = 0; i < current.Length; i++)
            {
                if (current[i] != '~')
                {
                    builder.Append(current[i]);
                    continue;
                }
                if (++i >= current.Length || current[i] is not ('0' or '1')) return false;
                builder.Append(current[i] == '0' ? '~' : '/');
            }
            result[segmentIndex] = builder.ToString();
        }

        segments = result;
        return true;
    }

    private static bool IsPrefix(IReadOnlyList<string> prefix, IReadOnlyList<string> candidate) =>
        prefix.Count <= candidate.Count && prefix.Select((segment, index) => segment == candidate[index]).All(matches => matches);

    private static AutomationWorkflowProfile? DeserializeProfile(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) return null;
        try
        {
            if (value.TryGetProperty("variables", out var rawVariables))
            {
                if (rawVariables.ValueKind is not (JsonValueKind.Object or JsonValueKind.Null)) return null;
                if (rawVariables.ValueKind == JsonValueKind.Object)
                {
                    var names = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var property in rawVariables.EnumerateObject())
                        if (!names.Add(property.Name)) return null;
                }
            }
            var profile = JsonSerializer.Deserialize<AutomationWorkflowProfile>(value.GetRawText(), JsonOptions);
            return profile is null ? null : NormalizeProfile(profile);
        }
        catch (JsonException) { return null; }
    }

    private static AutomationWorkflowProfile NormalizeProfile(AutomationWorkflowProfile profile) =>
        profile with { Variables = profile.Variables ?? EmptyVariables() };

    private static IReadOnlyDictionary<string, JsonElement> EmptyVariables() =>
        new Dictionary<string, JsonElement>(StringComparer.Ordinal);

    private static string ReadWorkflowId(JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object || !input.TryGetProperty("workflowId", out var value)
            || value.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("A workflow ID is required.");
        var id = value.GetString()!;
        if (!WorkflowIdPattern.IsMatch(id)) throw new InvalidDataException("The workflow ID is invalid.");
        return id;
    }

    private static JsonElement? ReadInitialInput(JsonElement input)
    {
        if (!input.TryGetProperty("initialInput", out var value)) return null;
        if (value.ValueKind == JsonValueKind.Undefined
            || Encoding.UTF8.GetByteCount(value.GetRawText()) > MaximumWorkflowInputBytes)
            throw new InvalidDataException("Workflow input is missing or exceeds the 48 KiB limit.");
        return value.Clone();
    }

    private static AutomationResult Error(string message) => Result(AutomationStatus.Error, message, new { });

    private static AutomationResult Result(AutomationStatus status, string message, object data) => new(
        AutomationTabContract.CurrentVersion,
        status,
        message,
        JsonSerializer.SerializeToElement(data, JsonOptions),
        []);

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private sealed record WorkflowUiPayload(
        string WorkflowId,
        JsonElement Output,
        IReadOnlyList<WorkflowUiStep> Steps,
        AutomationExecutionSummary Summary,
        bool OutputOmitted);

    private sealed record WorkflowUiStep(
        string StepId,
        string ModuleId,
        string ProfileId,
        AutomationStatus Status,
        JsonElement Output,
        AutomationExecutionSummary Summary,
        bool OutputOmitted);
}
