using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

public sealed record ExplorerActionDefinition(string Id, string ProfileId, string Label, IReadOnlyList<string> Extensions,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? FileParameterKey = null);

public sealed partial class ScriptRunnerModule
{
    public const string ExplorerActionCollection = "explorer-actions";
    public const string FilePathToken = "{{file.path}}";
    private static readonly Regex ExtensionPattern = new("^\\.[a-z0-9][a-z0-9_-]{0,31}$", RegexOptions.CultureInvariant);

    private static async Task<ExplorerActionDefinition[]> ReadExplorerActionsAsync(AutomationServicesContext services, CancellationToken token) =>
        (await services.Library!.ListAsync(ExplorerActionCollection, token)).Select(record =>
            JsonSerializer.Deserialize<ExplorerActionDefinition>(record.Data.GetRawText(), JsonOptions)
            ?? throw new InvalidDataException("The saved Explorer action is invalid.")).ToArray();

    private async Task<AutomationResult> ListExplorerActionsAsync(AutomationServicesContext services, CancellationToken token) =>
        Result(AutomationStatus.Success, "Explorer actions loaded.", new { actions = await ReadExplorerActionsAsync(services, token), registration = await ReconcileExplorerMenuAsync(services, token) });

    private async Task<AutomationResult> SaveExplorerActionAsync(JsonElement input, AutomationServicesContext services, CancellationToken token)
    {
        var action = JsonSerializer.Deserialize<ExplorerActionDefinition>(input.GetRawText(), JsonOptions)
            ?? throw new InvalidDataException("Explorer action is required.");
        ValidateExplorerAction(action);
        action = action with { Extensions = action.Extensions.Select(value => value.ToLowerInvariant()).Distinct(StringComparer.Ordinal).ToArray() };
        var profile = await GetExplorerProfileAsync(action, services, token);
        ValidateExplorerProfile(action, profile);
        await services.Library!.UpsertAsync(ExplorerActionCollection, action.Id, 1, JsonSerializer.SerializeToElement(action, JsonOptions), token);
        return Result(AutomationStatus.Success, "Explorer action saved.", new { action, registration = await ReconcileExplorerMenuAsync(services, token) });
    }

    private async Task<AutomationResult> DeleteExplorerActionAsync(JsonElement input, AutomationServicesContext services, CancellationToken token)
    {
        var id = ReadId(input);
        var deleted = await services.Library!.DeleteAsync(ExplorerActionCollection, id, token);
        return Result(AutomationStatus.Success, "Explorer action removed.", new { id, deleted, registration = await ReconcileExplorerMenuAsync(services, token) });
    }

    private sealed record ExplorerMenuRegistration(string State, string? Message = null);

    private async Task<ExplorerMenuRegistration> ReconcileExplorerMenuAsync(AutomationServicesContext services, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(_fileExplorerExecutable) || services.FileExplorerMenu is null)
            return new("disabled", "Explorer menu registration is available in the installed Windows build.");
        try
        {
            await services.FileExplorerMenu.ReconcileAsync(await ReadExplorerActionsAsync(services, token), _fileExplorerExecutable, token);
            return new("updated");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException
            or ArgumentException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            // The Library write has already succeeded. Surface a retryable registration error without losing the mapping.
            return new("error", "The mapping was saved, but the Explorer menu could not be updated. Reopen this section to retry.");
        }
    }

    private static async Task<ScriptRunnerProfile> GetExplorerProfileAsync(ExplorerActionDefinition action, AutomationServicesContext services, CancellationToken token)
    {
        var record = await services.Library!.GetAsync(ProfileCollection, action.ProfileId, token)
            ?? throw new InvalidDataException("The selected script profile no longer exists.");
        var profile = JsonSerializer.Deserialize<ScriptRunnerProfile>(record.Data.GetRawText(), JsonOptions)
            ?? throw new InvalidDataException("The saved script profile is invalid.");
        Validate(profile);
        return profile;
    }

    public static void ValidateExplorerAction(ExplorerActionDefinition action)
    {
        if (action.Id is null || !ProfileIdPattern.IsMatch(action.Id) || action.ProfileId is null || !ProfileIdPattern.IsMatch(action.ProfileId))
            throw new InvalidDataException("Explorer action and profile IDs must be valid keys.");
        if (string.IsNullOrWhiteSpace(action.Label) || action.Label.Length > 128 || action.Label.Any(char.IsControl))
            throw new InvalidDataException("Menu label is required and must be at most 128 characters.");
        if (action.Extensions is null || action.Extensions.Count is < 1 or > 32 || action.Extensions.Any(value => value is null || !ExtensionPattern.IsMatch(value.ToLowerInvariant())))
            throw new InvalidDataException("Use one or more file extensions such as .fdb.");
    }

    private void ValidateExplorerProfile(ExplorerActionDefinition action, ScriptRunnerProfile profile)
    {
        if (profile.TemplateOrigin is { } origin)
        {
            var template = ScriptRunnerTemplateCatalog.Get(origin.Id);
            if (template is null || origin.Version != template.Version || !_templateInstaller.MatchesInstalledAsset(profile))
                throw new InvalidDataException("The installed template is invalid. Remove and reinstall its profile.");
            if (!template.Parameters.Any(parameter => parameter.Key == action.FileParameterKey && parameter.Type is ScriptRunnerTemplateParameterType.File or ScriptRunnerTemplateParameterType.Directory))
                throw new InvalidDataException("Choose a declared file or directory template parameter.");
        }
        else if (action.FileParameterKey is not null || !profile.Arguments.Any(value => value.Contains(FilePathToken, StringComparison.Ordinal)))
            throw new InvalidDataException("Regular profiles require {{file.path}} in their saved arguments and no template parameter.");
    }

    private async Task<AutomationResult> RunExplorerActionAsync(JsonElement input, AutomationServicesContext services, CancellationToken token)
    {
        var id = ReadId(input);
        var action = (await ReadExplorerActionsAsync(services, token)).FirstOrDefault(item => item.Id == id)
            ?? throw new InvalidDataException("The selected Explorer action no longer exists.");
        ValidateExplorerAction(action);
        var profile = await GetExplorerProfileAsync(action, services, token);
        ValidateExplorerProfile(action, profile);
        if (!input.TryGetProperty("filePath", out var pathValue) || pathValue.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("A selected file path is required.");
        var path = pathValue.GetString()!;
        if (path.Length > 4096 || !Path.IsPathFullyQualified(path) || !File.Exists(path))
            throw new InvalidDataException("The selected file must be an existing absolute file path.");
        path = Path.GetFullPath(path);
        if (!action.Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException("The selected file extension does not match this Explorer action.");
        if (profile.TemplateOrigin is not null)
        {
            if (input.TryGetProperty("arguments", out _)) throw new InvalidDataException("Template actions accept typed inputs only.");
            if (!input.TryGetProperty("templateValues", out var values) || values.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("This template requires interactive input values before it can run.");
            var dictionary = values.EnumerateObject().ToDictionary(item => item.Name, item => item.Value.Clone());
            dictionary[action.FileParameterKey!] = JsonSerializer.SerializeToElement(path);
            return await RunProfileAsync(JsonSerializer.SerializeToElement(new { id = profile.Id, templateValues = dictionary }), services, token);
        }
        if (input.TryGetProperty("templateValues", out _)) throw new InvalidDataException("Regular profiles do not accept template values.");
        var arguments = input.TryGetProperty("arguments", out var edited)
            ? JsonSerializer.Deserialize<string[]>(edited.GetRawText(), JsonOptions) ?? throw new InvalidDataException("Arguments are invalid.") : profile.Arguments;
        // The form can submit an already-prefilled path. Keep that exact path opaque
        // while expanding profile variables, then insert it as literal file data.
        var prepared = arguments.Select(value => value?.Replace(path, FilePathToken, StringComparison.Ordinal)!).ToArray();
        Validate(profile with { Arguments = prepared });
        var result = await RunProfileAsync(JsonSerializer.SerializeToElement(new { id = profile.Id }), services, token, prepared, path);
        return result with { Actions = [] };
    }
}
