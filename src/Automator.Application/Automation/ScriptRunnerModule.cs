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
    int TimeoutSeconds);

/// <summary>Reusable Python, Bash, and PowerShell profiles with bounded process results.</summary>
public sealed class ScriptRunnerModule : ILauncherTabModuleProvider
{
    private readonly IAutomationVariableProvider? _variables;
    public ScriptRunnerModule(IAutomationVariableProvider? variables = null) => _variables = variables;
    public const string IdValue = "script-runner";
    public const int ContractVersionValue = 1;
    public const int SettingsVersionValue = 1;
    public const string ProfileCollection = "profiles";
    private static readonly Regex ProfileIdPattern = new("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    internal static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private static readonly AutomationCapabilityRequirement LibraryCapability = new(AutomationCapabilityIds.LibraryStorage, 1);
    private static readonly AutomationCapabilityRequirement ProcessCapability = new(AutomationCapabilityIds.ProcessExecution, 1);

    public AutomationModuleDefinition Definition { get; } = new(
        2,
        IdValue,
        "Script Runner",
        "terminal",
        "script-runner",
        false,
        ContractVersionValue,
        SettingsVersionValue,
        [LibraryCapability, ProcessCapability],
        [
            new("listProfiles", 1, [LibraryCapability]),
            new("saveProfile", 1, [LibraryCapability]),
            new("deleteProfile", 1, [LibraryCapability]),
            new("runProfile", 1, [LibraryCapability, ProcessCapability]),
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
                "listProfiles" => await ListProfilesAsync(services, cancellationToken).ConfigureAwait(false),
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

    private static async Task<AutomationResult> SaveProfileAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var profile = JsonSerializer.Deserialize<ScriptRunnerProfile>(input.GetRawText(), JsonOptions)
            ?? throw new InvalidDataException("The script profile is empty or invalid.");
        Validate(profile);
        await services.Library!.UpsertAsync(ProfileCollection, profile.Id, SettingsVersionValue,
            JsonSerializer.SerializeToElement(profile, JsonOptions), cancellationToken).ConfigureAwait(false);
        return Result(AutomationStatus.Success, $"Saved {profile.Name}.", new { profile });
    }

    private static async Task<AutomationResult> DeleteProfileAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var id = ReadId(input);
        var deleted = await services.Library!.DeleteAsync(ProfileCollection, id, cancellationToken).ConfigureAwait(false);
        return Result(deleted ? AutomationStatus.Success : AutomationStatus.Information,
            deleted ? "Script profile removed." : "Script profile was already removed.", new { id, deleted });
    }

    private async Task<AutomationResult> RunProfileAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var id = ReadId(input);
        var record = await services.Library!.GetAsync(ProfileCollection, id, cancellationToken).ConfigureAwait(false);
        if (record is null) return Error("The selected script profile no longer exists.");
        var profile = JsonSerializer.Deserialize<ScriptRunnerProfile>(record.Data.GetRawText(), JsonOptions)
            ?? throw new InvalidDataException("The saved script profile is invalid.");
        Validate(profile);

        if (_variables is not null)
        {
            var globals = await _variables.GetAsync(cancellationToken).ConfigureAwait(false);
            profile = profile with { Arguments = profile.Arguments.Select(argument => AutomationVariableInterpolation.Expand(argument, globals.Values)).ToArray() };
            Validate(profile);
        }
        var arguments = profile.Interpreter == ScriptRunnerInterpreter.Powershell
            ? new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-File", profile.ScriptPath }.Concat(profile.Arguments).ToArray()
            : new[] { profile.ScriptPath }.Concat(profile.Arguments).ToArray();
        var processResult = await services.Processes!.ExecuteAsync(new AutomationProcessRequest(
            profile.InterpreterPath,
            arguments,
            profile.WorkingDirectory,
            TimeSpan.FromSeconds(profile.TimeoutSeconds)), cancellationToken).ConfigureAwait(false);

        JsonElement? structuredOutput = null;
        var parseFailure = false;
        if (profile.OutputMode == ScriptRunnerOutputMode.Json && !string.IsNullOrWhiteSpace(processResult.StandardOutput))
        {
            try
            {
                using var document = JsonDocument.Parse(processResult.StandardOutput);
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
            stdout = processResult.StandardOutput,
            stderr = processResult.StandardError,
            stdoutTruncated = processResult.StandardOutputTruncated,
            stderrTruncated = processResult.StandardErrorTruncated,
            durationMilliseconds = processResult.DurationMilliseconds,
            structuredOutput,
            parseFailure,
        }, [new AutomationAction("runAgain", "Run again", 1, JsonSerializer.SerializeToElement(new { id = profile.Id }))]);
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
