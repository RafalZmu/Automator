using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Automator.Core.Automation;
using Automator.Core.Configuration;
using Automator.Core.Plugins;
using Automator.Application.Launcher;

namespace Automator.Application.Automation;

public enum ScriptRunnerInterpreter
{
    Python,
    Bash,
    Powershell
}

public enum ScriptRunnerOutputMode
{
    Text,
    Json
}

public sealed record ScriptRunnerProfile(
    string Id,
    string Name,
    ScriptRunnerInterpreter Interpreter,
    string InterpreterPath,
    string ScriptPath,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    ScriptRunnerOutputMode OutputMode,
    int TimeoutSeconds,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ScriptRunnerTemplateOrigin? TemplateOrigin = null);

public sealed record ScriptRunnerTemplateOrigin(string Id, int Version);

/// <summary>Reusable Python, Bash, and PowerShell profiles with bounded process results.</summary>
public sealed partial class ScriptRunnerModule : ILauncherTabModuleProvider
{
    private readonly string? _fileExplorerExecutable;
    private readonly IAutomationVariableProvider? _variables;
    private readonly ScriptRunnerTemplateInstaller _templateInstaller;
    public ScriptRunnerModule(IAutomationVariableProvider? variables = null, ScriptRunnerTemplateInstaller? templateInstaller = null, string? fileExplorerExecutable = null)
    {
        _fileExplorerExecutable = fileExplorerExecutable;
        _variables = variables;
        _templateInstaller = templateInstaller ?? new();
    }
    public const string IdValue = "script-runner";
    public const int ContractVersionValue = 1;
    public const int SettingsVersionValue = 1;
    public const string ProfileCollection = "profiles";
    private static readonly Regex ProfileIdPattern = new("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    internal static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private static readonly AutomationCapabilityRequirement LibraryCapability = new(AutomationCapabilityIds.LibraryStorage, 1);
    private static readonly AutomationCapabilityRequirement ProcessCapability = new(AutomationCapabilityIds.ProcessExecution, 1);

    private static readonly AutomationCapabilityRequirement ExplorerMenuCapability = new(AutomationCapabilityIds.FileExplorerMenu, 1);

    public AutomationModuleDefinition Definition { get; } = new(
        2,
        IdValue,
        "Script Runner",
        "terminal",
        "script-runner",
        false,
        ContractVersionValue,
        SettingsVersionValue,
        [LibraryCapability, ProcessCapability, ExplorerMenuCapability],
        [
            new("listProfiles", 1, [LibraryCapability]),
            new("listTemplates", 1, [LibraryCapability]),
            new("installTemplate", 1, [LibraryCapability]),
            new("saveProfile", 1, [LibraryCapability]),
            new("deleteProfile", 1, [LibraryCapability, ExplorerMenuCapability]),
            new("runProfile", 1, [LibraryCapability, ProcessCapability]),
            new("listExplorerActions", 1, [LibraryCapability, ExplorerMenuCapability]),
            new("saveExplorerAction", 1, [LibraryCapability, ExplorerMenuCapability]),
            new("deleteExplorerAction", 1, [LibraryCapability, ExplorerMenuCapability]),
            new("runExplorerAction", 1, [LibraryCapability, ProcessCapability]),
        ]);

    public int ContractVersion => Definition.ContractVersion;
    public string Id => Definition.Id;
    public string Title => Definition.Title;

    public LauncherModuleState CreateInitialState() =>
        new(Id, LauncherTabRegistry.Version, new Dictionary<string, string>(StringComparer.Ordinal) { ["status"] = "ready" });

    public JsonElement CreateDefaultSettings() => JsonSerializer.SerializeToElement(new
    {
        interpreterDefaults = new { python = string.Empty, bash = string.Empty, powershell = string.Empty }
    }, JsonOptions);

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
                "listExplorerActions" => await ListExplorerActionsAsync(services, cancellationToken),
                "saveExplorerAction" => await SaveExplorerActionAsync(input, services, cancellationToken),
                "deleteExplorerAction" => await DeleteExplorerActionAsync(input, services, cancellationToken),
                "runExplorerAction" => await RunExplorerActionAsync(input, services, cancellationToken),
                "listProfiles" => await ListProfilesAsync(services, cancellationToken).ConfigureAwait(false),
                "listTemplates" => ListTemplates(),
                "installTemplate" => await InstallTemplateAsync(input, moduleSettings, services, cancellationToken).ConfigureAwait(false),
                "saveProfile" => await SaveProfileAsync(input, services, cancellationToken).ConfigureAwait(false),
                "deleteProfile" => await DeleteProfileAsync(input, services, cancellationToken).ConfigureAwait(false),
                "runProfile" => await RunProfileAsync(input, services, cancellationToken).ConfigureAwait(false),
                _ => Error($"Unknown Script Runner action '{actionId}'.")
            };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException or IOException
            or UnauthorizedAccessException or JsonException or ArgumentException or System.ComponentModel.Win32Exception)
        {
            return Error(exception.Message);
        }
    }

    private static async Task<AutomationResult> ListProfilesAsync(AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var profiles = await services.Library!.ListAsync(ProfileCollection, cancellationToken).ConfigureAwait(false);
        return Result(AutomationStatus.Success, $"{profiles.Count} saved script profile{(profiles.Count == 1 ? string.Empty : "s")}.",
            new { profiles = profiles.Select(profile => profile.Data).ToArray() });
    }

    private static AutomationResult ListTemplates() => Result(AutomationStatus.Success,
        $"{ScriptRunnerTemplateCatalog.All.Count} script template{(ScriptRunnerTemplateCatalog.All.Count == 1 ? string.Empty : "s")} available.",
        new { templates = ScriptRunnerTemplateCatalog.All });

    private async Task<AutomationResult> InstallTemplateAsync(JsonElement input, JsonElement settings,
        AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var (profile, installed) = await _templateInstaller.InstallAsync(ReadId(input), settings, services.Library!, cancellationToken).ConfigureAwait(false);
        return Result(installed ? AutomationStatus.Success : AutomationStatus.Information,
            installed ? $"Installed {profile.Name}." : "This template is already installed.", new { profile, installed });
    }

    private async Task<AutomationResult> SaveProfileAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var profile = JsonSerializer.Deserialize<ScriptRunnerProfile>(input.GetRawText(), JsonOptions)
            ?? throw new InvalidDataException("The script profile is empty or invalid.");
        Validate(profile);
        if (profile.TemplateOrigin is not null && !_templateInstaller.MatchesInstalledAsset(profile))
            throw new InvalidDataException("Template origin does not match a registered installed script asset.");

        await services.Library!.UpsertAsync(ProfileCollection, profile.Id, SettingsVersionValue,
            JsonSerializer.SerializeToElement(profile, JsonOptions), cancellationToken).ConfigureAwait(false);
        return Result(AutomationStatus.Success, $"Saved {profile.Name}.", new { profile });
    }

    private async Task<AutomationResult> DeleteProfileAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var id = ReadId(input);
        var deleted = await services.Library!.DeleteAsync(ProfileCollection, id, cancellationToken).ConfigureAwait(false);
        foreach (var action in await ReadExplorerActionsAsync(services, cancellationToken))
            if (action.ProfileId == id) await services.Library.DeleteAsync(ExplorerActionCollection, action.Id, cancellationToken);
        return Result(deleted ? AutomationStatus.Success : AutomationStatus.Information,
            deleted ? "Script profile removed." : "Script profile was already removed.", new { id, deleted, registration = await ReconcileExplorerMenuAsync(services, cancellationToken) });
    }

    private async Task<AutomationResult> RunProfileAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken, IReadOnlyList<string>? explorerArguments = null, string? explorerFilePath = null)
    {
        var id = ReadId(input);
        var record = await services.Library!.GetAsync(ProfileCollection, id, cancellationToken).ConfigureAwait(false);
        if (record is null) return Error("The selected script profile no longer exists.");
        var profile = JsonSerializer.Deserialize<ScriptRunnerProfile>(record.Data.GetRawText(), JsonOptions)
            ?? throw new InvalidDataException("The saved script profile is invalid.");
        Validate(profile);

        var hasTemplateValues = input.TryGetProperty("templateValues", out var templateValues);
        ScriptRunnerTemplateDescriptor? template = null;
        string[]? transientArguments = null;
        if (profile.TemplateOrigin is { } origin)
        {
            template = ScriptRunnerTemplateCatalog.Get(origin.Id);
            if (template is null || template.Version != origin.Version || !_templateInstaller.MatchesInstalledAsset(profile))
                return Error("The installed template origin, path, or script content is invalid. Remove and reinstall the profile to repair it.");
            if (!hasTemplateValues) return Error("This template requires interactive input values before it can run.");
            transientArguments = ScriptRunnerTemplateCatalog.MapArguments(template, templateValues).ToArray();
        }
        else if (hasTemplateValues)
        {
            return Error("Transient template inputs are only accepted for a registered template profile.");
        }

        if (_variables is not null)
        {
            var globals = await _variables.GetAsync(cancellationToken).ConfigureAwait(false);
            profile = profile with { Arguments = (explorerArguments ?? profile.Arguments).Select(argument => AutomationVariableInterpolation.Expand(argument, globals.Values)).ToArray() };
            explorerArguments = null;
            Validate(profile);
        }
        var runProfile = profile with { Arguments = transientArguments ?? explorerArguments ?? profile.Arguments };
        if (explorerFilePath is not null)
            runProfile = runProfile with { Arguments = runProfile.Arguments.Select(argument => argument.Replace(FilePathToken, explorerFilePath, StringComparison.Ordinal)).ToArray() };
        Validate(runProfile);
        var arguments = runProfile.Interpreter == ScriptRunnerInterpreter.Powershell
            ? new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-File", runProfile.ScriptPath }.Concat(runProfile.Arguments).ToArray()
            : new[] { runProfile.ScriptPath }.Concat(runProfile.Arguments).ToArray();
        AutomationProcessResult processResult;
        try
        {
            processResult = await services.Processes!.ExecuteAsync(new AutomationProcessRequest(
                runProfile.InterpreterPath, arguments, runProfile.WorkingDirectory,
                TimeSpan.FromSeconds(runProfile.TimeoutSeconds)), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (template is not null)
        {
            return Error(RedactSensitive(exception.Message, template, transientArguments!));
        }

        var stdout = template is null ? processResult.StandardOutput : RedactSensitive(processResult.StandardOutput, template, transientArguments!);
        var stderr = template is null ? processResult.StandardError : RedactSensitive(processResult.StandardError, template, transientArguments!);

        JsonElement? structuredOutput = null;
        var parseFailure = false;
        if (profile.OutputMode == ScriptRunnerOutputMode.Json && !string.IsNullOrWhiteSpace(stdout))
        {
            try
            {
                using var document = JsonDocument.Parse(stdout);
                structuredOutput = document.RootElement.Clone();
            }
            catch (JsonException) { parseFailure = true; }
        }

        var status = processResult.TimedOut ? AutomationStatus.Warning
            : processResult.ExitCode == 0 && !parseFailure ? AutomationStatus.Success
            : processResult.ExitCode == 0 ? AutomationStatus.Warning : AutomationStatus.Error;
        var message = processResult.TimedOut ? $"{profile.Name} exceeded its {profile.TimeoutSeconds}-second timeout."
            : processResult.ExitCode != 0 ? $"{profile.Name} exited with code {processResult.ExitCode}."
            : parseFailure ? "The script completed, but its output was not valid JSON."
            : $"{profile.Name} completed.";
        return Result(status, message, new
        {
            profileId = profile.Id,
            exitCode = processResult.ExitCode,
            timedOut = processResult.TimedOut,
            stdout,
            stderr,
            stdoutTruncated = processResult.StandardOutputTruncated,
            stderrTruncated = processResult.StandardErrorTruncated,
            durationMilliseconds = processResult.DurationMilliseconds,
            structuredOutput,
            parseFailure,
        }, template is null ? [new AutomationAction("runAgain", "Run again", 1, JsonSerializer.SerializeToElement(new { id = profile.Id }))] : []);
    }

    private static string RedactSensitive(string value, ScriptRunnerTemplateDescriptor template, IReadOnlyList<string> arguments)
    {
        foreach (var parameter in template.Parameters.Where(parameter => parameter.Sensitive))
        {
            var secret = arguments[parameter.ArgumentIndex];
            if (secret.Length > 0) value = value.Replace(secret, "[redacted]", StringComparison.Ordinal);
        }
        return value;
    }

    private static string ReadId(JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object || !input.TryGetProperty("id", out var value) || value.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("A script profile id is required.");
        var id = value.GetString()!;
        if (!ProfileIdPattern.IsMatch(id)) throw new InvalidDataException("The script profile id is invalid.");
        return id;
    }

    /// <summary>Validates a stored profile for module execution and library import.</summary>
    public static void Validate(ScriptRunnerProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Id) || !ProfileIdPattern.IsMatch(profile.Id))
            throw new InvalidDataException("Profile id must be a short lowercase key using letters, numbers, dots, dashes, or underscores.");
        if (string.IsNullOrWhiteSpace(profile.Name) || profile.Name.Length > 128)
            throw new InvalidDataException("Profile name is required and must be at most 128 characters.");
        if (!Enum.IsDefined(profile.Interpreter) || !Enum.IsDefined(profile.OutputMode))
            throw new InvalidDataException("Script interpreter or output mode is unsupported.");
        if (!Path.IsPathFullyQualified(profile.InterpreterPath) || profile.InterpreterPath.Length > 4096)
            throw new InvalidDataException("Interpreter path must be an absolute path.");
        if (!Path.IsPathFullyQualified(profile.ScriptPath) || profile.ScriptPath.Length > 4096)
            throw new InvalidDataException("Script path must be an absolute path.");
        if (!Path.IsPathFullyQualified(profile.WorkingDirectory) || profile.WorkingDirectory.Length > 4096)
            throw new InvalidDataException("Working directory must be an absolute path.");
        var expectedExtension = profile.Interpreter switch
        {
            ScriptRunnerInterpreter.Python => ".py",
            ScriptRunnerInterpreter.Bash => ".sh",
            ScriptRunnerInterpreter.Powershell => ".ps1",
            _ => throw new InvalidDataException("Script interpreter is unsupported.")
        };
        if (!string.Equals(Path.GetExtension(profile.ScriptPath), expectedExtension, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"{profile.Interpreter} profiles must use a {expectedExtension} script.");
        if (profile.Arguments is null || profile.Arguments.Count > AutomationProcessLimits.MaximumArgumentCount - 1
            || profile.Arguments.Any(argument => argument is null || argument.Length > AutomationProcessLimits.MaximumArgumentLength))
            throw new InvalidDataException("The profile argument list is invalid or too large.");
        if (profile.TimeoutSeconds is < 1 or > 3600)
            throw new InvalidDataException("Profile timeout must be between 1 and 3600 seconds.");
        if (profile.TemplateOrigin is { } origin &&
            (string.IsNullOrWhiteSpace(origin.Id) || !ProfileIdPattern.IsMatch(origin.Id) || origin.Version < 1))
            throw new InvalidDataException("Template origin is invalid.");
    }

    private static AutomationResult Error(string message) => Result(AutomationStatus.Error, message, new { });

    private static AutomationResult Result(AutomationStatus status, string message, object data,
        IReadOnlyList<AutomationAction>? actions = null) => new(
            AutomationTabContract.CurrentVersion,
            status,
            message,
            JsonSerializer.SerializeToElement(data, JsonOptions),
            actions ?? []);

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
