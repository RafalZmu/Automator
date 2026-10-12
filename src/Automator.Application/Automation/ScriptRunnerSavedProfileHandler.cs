using System.Text.Json;
using Automator.Core.Automation;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

/// <summary>Host-only Script Runner adapter used by workflow and scheduler execution.</summary>
public sealed class ScriptRunnerSavedProfileHandler(
    IAutomationLibraryStore library,
    IAutomationProcessService processes,
    IAutomationVariableProvider? variables = null) : IAutomationSavedProfileHandler
{
    public string ModuleId => ScriptRunnerModule.IdValue;

    public async Task<IReadOnlyList<AutomationSavedProfileSummary>> ListProfilesAsync(CancellationToken cancellationToken)
    {
        var records = await library.ListAsync(ModuleId, ScriptRunnerModule.ProfileCollection, cancellationToken).ConfigureAwait(false);
        return Array.AsReadOnly(records.Select(record =>
        {
            var profile = ReadProfile(record.Data);
            return new AutomationSavedProfileSummary(profile.Id, profile.Name);
        }).ToArray());
    }

    public async Task<AutomationProfileExecutionOutput> ExecuteAsync(
        string profileId,
        JsonElement? input,
        AutomationExecutionMetadata metadata,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        var startedAt = System.Diagnostics.Stopwatch.StartNew();
        var record = await library.GetAsync(ModuleId, ScriptRunnerModule.ProfileCollection, profileId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("The selected script profile no longer exists.");
        var profile = ReadProfile(record.Data);
        if (profile.TemplateOrigin is { } origin)
        {
            var template = ScriptRunnerTemplateCatalog.Get(origin.Id);
            if (template is null || template.Version != origin.Version)
                throw new InvalidDataException("The installed template version is no longer registered. Repair it in Script Runner.");
            throw new InvalidDataException("This template requires interactive input values. Run it from Script Runner.");
        }
        var globals = variables is null ? null : await variables.GetAsync(cancellationToken).ConfigureAwait(false);
        if (globals is not null) profile = profile with { Arguments = profile.Arguments.Select(argument => AutomationVariableInterpolation.Expand(argument, globals.Values)).ToArray() };
        var execution = await ScriptRunnerExecution.RunAsync(profile, processes, input, metadata.Origin,
            metadata.CorrelationId, cancellationToken).ConfigureAwait(false);
        return new AutomationProfileExecutionOutput(execution.Output, execution.Summary with { DurationMilliseconds = (long)startedAt.Elapsed.TotalMilliseconds });
    }

    private static ScriptRunnerProfile ReadProfile(JsonElement data)
    {
        var profile = JsonSerializer.Deserialize<ScriptRunnerProfile>(data.GetRawText(), ScriptRunnerModule.JsonOptions)
            ?? throw new InvalidDataException("The saved script profile is invalid.");
        ScriptRunnerModule.Validate(profile);
        return profile;
    }

}
