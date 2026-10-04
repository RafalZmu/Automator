using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Automator.Application.Launcher;
using Automator.Core.Automation;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

/// <summary>Persisted browser profile and its exact network grant.</summary>
public sealed record BrowserAutomationProfile(
    string Id,
    string Name,
    string StartUrl,
    IReadOnlyList<string> AllowedHosts,
    bool AllowLocalNetwork);

/// <summary>Bundled Browser Automation tab. Browser isolation and request policy are host-owned.</summary>
public sealed class BrowserAutomationModule : ILauncherTabModuleProvider
{
    private readonly IAutomationVariableProvider? _variables;
    public BrowserAutomationModule(IAutomationVariableProvider? variables = null) => _variables = variables;
    public const string IdValue = "browser-automation";
    public const int ContractVersionValue = 1;
    public const int SettingsVersionValue = 1;
    public const string ProfileCollection = "profiles";
    private const int MaximumProfiles = 256;
    private const int MaximumActionInputBytes = 48 * 1024;
    private const int MaximumOutputBytes = AutomationSavedProfileExecutor.MaximumStructuredDataBytes - 1024;
    private const int MaximumTextBytes = 128 * 1024;
    private static readonly Regex ProfileIdPattern = new("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly AutomationCapabilityRequirement LibraryCapability = new(AutomationCapabilityIds.LibraryStorage, 1);
    private static readonly AutomationCapabilityRequirement BrowserCapability = new(AutomationCapabilityIds.BrowserSession, 1);
    private static readonly AutomationCapabilityRequirement ProcessCapability = new(AutomationCapabilityIds.ProcessExecution, 1);
    internal static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public AutomationModuleDefinition Definition { get; } = new(
        4,
        IdValue,
        "Browser Automation",
        "globe",
        "browser-automation",
        false,
        ContractVersionValue,
        SettingsVersionValue,
        [LibraryCapability, BrowserCapability, ProcessCapability],
        [
            new("getExplorerState", 1, [LibraryCapability]),
            new("setProject", 1, [LibraryCapability]),
            new("discoverTests", 1, [LibraryCapability, ProcessCapability]),
            new("createSection", 1, [LibraryCapability]),
            new("appendTest", 1, [LibraryCapability]),
            new("saveTestTags", 1, [LibraryCapability, ProcessCapability]),
            new("removeStaleTags", 1, [LibraryCapability, ProcessCapability]),
            new("runTests", 1, [LibraryCapability, ProcessCapability]),
            new("listProfiles", 1, [LibraryCapability]),
            new("saveProfile", 1, [LibraryCapability]),
            new("deleteProfile", 1, [LibraryCapability, BrowserCapability]),
            new("getRuntimeStatus", 1, [BrowserCapability]),
            new("runAction", 1, [LibraryCapability, BrowserCapability]),
            new("closeSession", 1, [BrowserCapability]),
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
                "getExplorerState" => await CreateExplorer(services).GetStateAsync(cancellationToken).ConfigureAwait(false),
                "setProject" => await CreateExplorer(services).SetProjectAsync(input, cancellationToken).ConfigureAwait(false),
                "discoverTests" => await CreateExplorer(services).DiscoverAsync(cancellationToken).ConfigureAwait(false),
                "createSection" => await CreateExplorer(services).CreateSectionAsync(input, cancellationToken).ConfigureAwait(false),
                "appendTest" => await CreateExplorer(services).AppendTestAsync(input, cancellationToken).ConfigureAwait(false),
                "saveTestTags" => await CreateExplorer(services).SaveTagsAsync(input, cancellationToken).ConfigureAwait(false),
                "removeStaleTags" => await CreateExplorer(services).RemoveStaleTagsAsync(cancellationToken).ConfigureAwait(false),
                "runTests" => await CreateExplorer(services).RunAsync(input, cancellationToken).ConfigureAwait(false),
                "listProfiles" => await ListProfilesAsync(services, cancellationToken).ConfigureAwait(false),
                "saveProfile" => await SaveProfileAsync(input, services, cancellationToken).ConfigureAwait(false),
                "deleteProfile" => await DeleteProfileAsync(input, services, cancellationToken).ConfigureAwait(false),
                "getRuntimeStatus" => await GetRuntimeStatusAsync(services, cancellationToken).ConfigureAwait(false),
                "runAction" => await RunActionAsync(input, services, cancellationToken).ConfigureAwait(false),
                "closeSession" => await CloseSessionAsync(input, services, cancellationToken).ConfigureAwait(false),
                _ => Error("Unknown Browser Automation action."),
            };
        }
        catch (OperationCanceledException) { throw; }
        catch (BrowserProfileValidationException exception) { return Error(exception.Message); }
        catch (InvalidDataException exception) when (actionId is "setProject" or "discoverTests" or "createSection" or "appendTest" or "saveTestTags" or "removeStaleTags" or "runTests")
        { return Error(exception.Message); }
        catch
        {
            // Host/browser exceptions can include URLs, page content, or local paths. Keep details out of the UI result.
            return Error(actionId switch
            {
                "getExplorerState" => "The Playwright Test Explorer could not load its saved state.",
                "setProject" => "The selected Playwright project folder could not be saved.",
                "discoverTests" => "Playwright test discovery failed.",
                "createSection" => "The new test section could not be created.",
                "appendTest" => "The new test could not be added to that section.",
                "saveTestTags" => "The test tags could not be saved.",
                "removeStaleTags" => "Stale test tags could not be removed.",
                "runTests" => "The Playwright test run failed. Check the project runner and configuration.",
                "getRuntimeStatus" => "Browser runtime status is unavailable.",
                "listProfiles" => "Browser profiles could not be loaded.",
                "saveProfile" => "Browser profile could not be saved.",
                "deleteProfile" => "Browser profile could not be removed.",
                "closeSession" => "The browser session could not be closed.",
                _ => "The browser action failed. Check the browser runtime and the profile's network policy.",
            });
        }
    }

    private static PlaywrightTestExplorer CreateExplorer(AutomationServicesContext services) =>
        new(services.Library!, services.Processes!);

    private static async Task<AutomationResult> ListProfilesAsync(AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var records = await services.Library!.ListAsync(ProfileCollection, cancellationToken).ConfigureAwait(false);
        var profiles = records.Select(TryReadProfile).Where(profile => profile is not null).Cast<BrowserAutomationProfile>()
            .OrderBy(profile => profile.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(MaximumProfiles + 1).ToArray();
        var truncated = profiles.Length > MaximumProfiles;
        if (truncated) profiles = profiles[..MaximumProfiles];
        return Result(AutomationStatus.Success,
            $"{profiles.Length} saved browser profile{(profiles.Length == 1 ? string.Empty : "s")}{(truncated ? " (showing the first 256)" : string.Empty)}.",
            new { profiles, truncated });
    }

    private static async Task<AutomationResult> SaveProfileAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        BrowserAutomationProfile profile;
        try
        {
            profile = JsonSerializer.Deserialize<BrowserAutomationProfile>(input.GetRawText(), JsonOptions)
                ?? throw new BrowserProfileValidationException("Browser profile data is empty or invalid.");
        }
        catch (JsonException)
        {
            throw new BrowserProfileValidationException("Browser profile data is invalid.");
        }

        profile = NormalizeAndValidate(profile);
        await services.Library!.UpsertAsync(ProfileCollection, profile.Id, SettingsVersionValue,
            JsonSerializer.SerializeToElement(profile, JsonOptions), cancellationToken).ConfigureAwait(false);
        return Result(AutomationStatus.Success, $"Saved {profile.Name}.", new { profile });
    }

    private static async Task<AutomationResult> DeleteProfileAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var id = ReadProfileId(input);
        await services.Browser!.CloseSessionAsync(id, cancellationToken).ConfigureAwait(false);
        var deleted = await services.Library!.DeleteAsync(ProfileCollection, id, cancellationToken).ConfigureAwait(false);
        return Result(deleted ? AutomationStatus.Success : AutomationStatus.Information,
            deleted ? "Browser profile removed." : "Browser profile was already removed.", new { id, deleted });
    }

    private static async Task<AutomationResult> GetRuntimeStatusAsync(
        AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var runtime = await services.Browser!.GetRuntimeStatusAsync(cancellationToken).ConfigureAwait(false);
        var progress = runtime.ProgressPercent is >= 0 and <= 100 ? runtime.ProgressPercent : null;
        var fallback = runtime.Ready ? "Browser runtime ready."
            : runtime.Installed ? "Browser runtime is preparing."
            : "The browser will be installed automatically the first time you run an action.";
        var rawMessage = string.IsNullOrWhiteSpace(runtime.Message) ? fallback : runtime.Message;
        var message = rawMessage.Length <= 256 ? rawMessage : rawMessage[..256];
        var data = new { installed = runtime.Installed, ready = runtime.Ready, message, progressPercent = progress };
        var status = runtime.Ready ? AutomationStatus.Success
            : message.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ? AutomationStatus.Warning
            : AutomationStatus.Information;
        return Result(status, message, data);
    }

    private async Task<AutomationResult> RunActionAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var id = ReadProfileId(input);
        var profile = await RequireProfileAsync(id, services.Library!, cancellationToken).ConfigureAwait(false);
        var action = ReadAction(input, profile);
        if (_variables is not null && action.Value is not null)
        {
            var globals = await _variables.GetAsync(cancellationToken).ConfigureAwait(false);
            action = action with { Value = AutomationVariableInterpolation.Expand(action.Value, globals.Values) };
            if (action.Value.Length > 32 * 1024) throw new InvalidDataException("The expanded browser action value is too large.");
        }
        var browserResult = await services.Browser!.ExecuteAsync(id, action, cancellationToken).ConfigureAwait(false);
        var bounded = BoundResult(browserResult, profile);
        var payload = JsonSerializer.SerializeToElement(new { id, action }, JsonOptions);
        return Result(AutomationStatus.Success, $"{profile.Name}: {ActionLabel(action.Kind)} completed.", bounded,
            [new AutomationAction("repeatAction", "Run again", 1, payload)]);
    }

    private static async Task<AutomationResult> CloseSessionAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var id = ReadProfileId(input);
        await services.Browser!.CloseSessionAsync(id, cancellationToken).ConfigureAwait(false);
        return Result(AutomationStatus.Success, "Browser session closed.", new { id, closed = true });
    }

    private static async Task<BrowserAutomationProfile> RequireProfileAsync(
        string id, IAutomationLibrary library, CancellationToken cancellationToken)
    {
        var record = await library.GetAsync(ProfileCollection, id, cancellationToken).ConfigureAwait(false);
        var profile = record is null ? null : TryReadProfile(record);
        return profile is null
            ? throw new BrowserProfileValidationException("The selected browser profile does not exist or is invalid.")
            : profile;
    }

    private static AutomationBrowserAction ReadAction(JsonElement input, BrowserAutomationProfile profile)
    {
        if (!input.TryGetProperty("action", out var value) || value.ValueKind == JsonValueKind.Null)
            return new AutomationBrowserAction(AutomationBrowserActionKind.Navigate, profile.StartUrl);
        if (value.ValueKind != JsonValueKind.Object || Encoding.UTF8.GetByteCount(value.GetRawText()) > MaximumActionInputBytes)
            throw new BrowserProfileValidationException("Browser action input is invalid or too large.");

        AutomationBrowserAction action;
        try
        {
            action = JsonSerializer.Deserialize<AutomationBrowserAction>(value.GetRawText(), JsonOptions)
                ?? throw new BrowserProfileValidationException("Browser action data is invalid.");
        }
        catch (JsonException)
        {
            throw new BrowserProfileValidationException("Browser action data is invalid.");
        }

        ValidateAction(action, profile);
        return action;
    }

    private static AutomationBrowserActionResult BoundResult(
        AutomationBrowserActionResult result, BrowserAutomationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(result);
        var currentUrl = SanitizeUrl(result.CurrentUrl, maximumLength: 8192) ?? "about:blank";
        var title = TruncateUtf8(result.Title ?? string.Empty, 4096);
        var text = TruncateUtf8(result.Text ?? string.Empty, MaximumTextBytes);
        var links = (result.Links ?? []).Take(64)
            .Where(link => link is not null && !string.IsNullOrWhiteSpace(link.Url))
            .Select(link => SanitizeUrl(link.Url, 2048) is { } safeUrl
                ? new AutomationBrowserLink(TruncateUtf8(link.Text ?? string.Empty, 512), safeUrl)
                : null)
            .OfType<AutomationBrowserLink>()
            .Where(link => IsAllowedHost(link.Url, profile.AllowedHosts))
            .ToList();
        while (true)
        {
            var bounded = new AutomationBrowserActionResult(currentUrl, title, text, links.ToArray());
            if (Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(bounded, JsonOptions)) <= MaximumOutputBytes)
                return bounded;
            if (Encoding.UTF8.GetByteCount(text) > 0)
                text = TruncateUtf8(text, Math.Max(1, Encoding.UTF8.GetByteCount(text) / 2));
            else if (links.Count > 0)
                links.RemoveRange(links.Count / 2, links.Count - links.Count / 2);
            else if (Encoding.UTF8.GetByteCount(title) > 0)
                title = TruncateUtf8(title, Math.Max(1, Encoding.UTF8.GetByteCount(title) / 2));
            else
                throw new BrowserProfileValidationException("Browser result exceeded the supported display size.");
        }
    }

    private static void ValidateAction(AutomationBrowserAction action, BrowserAutomationProfile profile)
    {
        if (!Enum.IsDefined(action.Kind)) throw new BrowserProfileValidationException("Browser action is unsupported.");
        if (action.TimeoutMilliseconds is < 250 or > 30_000)
            throw new BrowserProfileValidationException("Browser action timeout must be between 250 and 30,000 milliseconds.");

        if (action.Kind == AutomationBrowserActionKind.Navigate)
        {
            if (!IsAllowedHost(action.Url, profile.AllowedHosts))
                throw new BrowserProfileValidationException("The URL must use HTTP or HTTPS and a host in this profile's exact allowed-host list.");
        }

        var needsLocator = action.Kind is AutomationBrowserActionKind.Click or AutomationBrowserActionKind.Fill
            or AutomationBrowserActionKind.Select or AutomationBrowserActionKind.WaitFor or AutomationBrowserActionKind.ReadText;
        if (needsLocator)
        {
            if (action.LocatorKind is not { } locatorKind || !Enum.IsDefined(locatorKind))
                throw new BrowserProfileValidationException("Choose a supported locator type.");
            if (string.IsNullOrWhiteSpace(action.Locator) || action.Locator.Length > 2048)
                throw new BrowserProfileValidationException("Locator is required and must be at most 2,048 characters.");
        }

        if (action.Kind is AutomationBrowserActionKind.Fill or AutomationBrowserActionKind.Select
            && (action.Value is null || action.Value.Length > 32 * 1024))
            throw new BrowserProfileValidationException("Action value is required and must be at most 32 KiB.");
        if (action.Value is { Length: > 32 * 1024 })
            throw new BrowserProfileValidationException("Action value must be at most 32 KiB.");
        if (action.Url is { Length: > 4096 })
            throw new BrowserProfileValidationException("Action URL must be at most 4,096 characters.");
    }

    private static BrowserAutomationProfile NormalizeAndValidate(BrowserAutomationProfile profile)
    {
        var issues = Validate(profile);
        if (issues.Count > 0) throw new BrowserProfileValidationException(string.Join(" ", issues));
        return profile with
        {
            Id = profile.Id.Trim().ToLowerInvariant(),
            Name = profile.Name.Trim(),
            StartUrl = profile.StartUrl.Trim(),
            AllowedHosts = profile.AllowedHosts.Select(NormalizeHost).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
        };
    }

    private static IReadOnlyList<string> Validate(BrowserAutomationProfile profile)
    {
        var issues = new List<string>();
        if (!ProfileIdPattern.IsMatch(profile.Id ?? string.Empty))
            issues.Add("Profile key must be a short lowercase identifier.");
        if (string.IsNullOrWhiteSpace(profile.Name) || profile.Name.Length > 128)
            issues.Add("Name is required and must be at most 128 characters.");

        if (!TryReadWebUri(profile.StartUrl, out var startUri) || profile.StartUrl.Length > 8192)
            issues.Add("Start URL must be an absolute HTTP or HTTPS address without credentials.");

        if (profile.AllowedHosts is null || profile.AllowedHosts.Count is < 1 or > 64)
        {
            issues.Add("Add between 1 and 64 exact allowed hosts.");
        }
        else
        {
            var normalizedHosts = new List<string>();
            foreach (var host in profile.AllowedHosts)
            {
                try { normalizedHosts.Add(NormalizeHost(host)); }
                catch (ArgumentException) { issues.Add("Allowed hosts must be exact host names without wildcards, ports, or paths."); }
            }
            if (normalizedHosts.Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalizedHosts.Count)
                issues.Add("Allowed hosts must be unique.");
            if (startUri is not null)
            {
                try
                {
                    var startHost = NormalizeHost(startUri.IdnHost);
                    if (!normalizedHosts.Contains(startHost, StringComparer.OrdinalIgnoreCase))
                        issues.Add("The start URL host must appear in the exact allowed-host list.");
                }
                catch (ArgumentException) { issues.Add("The start URL host is invalid."); }
            }
        }

        return issues.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static string NormalizeHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("A host name is required.");
        var value = host.Trim().TrimEnd('.');
        if (value.Length is 0 or > 253 || value.Contains('*') || value.Any(char.IsWhiteSpace)
            || value.Contains('/') || value.Contains('@') || value.Contains('?') || value.Contains('#'))
            throw new ArgumentException("Allowed hosts must be exact host names.");
        if (IPAddress.TryParse(value, out var address)) return address.ToString().ToLowerInvariant();
        if (value.Contains(':')) throw new ArgumentException("Allowed hosts must not contain a port.");
        try { return new IdnMapping().GetAscii(value).ToLowerInvariant(); }
        catch (ArgumentException) { throw new ArgumentException("Allowed host name is invalid."); }
    }

    private static bool TryReadWebUri(string? value, out Uri? uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && string.IsNullOrEmpty(uri.UserInfo)) return true;
        uri = null;
        return false;
    }

    private static bool IsAllowedHost(string? value, IReadOnlyList<string> allowedHosts)
    {
        if (!TryReadWebUri(value, out var uri) || uri is null) return false;
        try
        {
            var host = NormalizeHost(uri.IdnHost);
            return allowedHosts.Any(allowed => string.Equals(NormalizeHost(allowed), host, StringComparison.OrdinalIgnoreCase));
        }
        catch (ArgumentException) { return false; }
    }

    private static string? SanitizeUrl(string? value, int maximumLength)
    {
        if (value is null || value.Length > maximumLength) return null;
        if (TryReadWebUri(value, out var uri) && uri is not null)
        {
            var builder = new UriBuilder(uri) { UserName = string.Empty, Password = string.Empty };
            return TruncateUtf8(builder.Uri.AbsoluteUri, maximumLength);
        }
        return string.Equals(value, "about:blank", StringComparison.OrdinalIgnoreCase) ? "about:blank" : null;
    }

    private static string ReadProfileId(JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object || !input.TryGetProperty("id", out var value)
            || value.ValueKind != JsonValueKind.String)
            throw new BrowserProfileValidationException("A browser profile id is required.");
        var id = value.GetString()!;
        if (!ProfileIdPattern.IsMatch(id)) throw new BrowserProfileValidationException("Browser profile id is invalid.");
        return id;
    }

    /// <summary>Reads validated schema-v1 profiles for the host browser service and workflow handler.</summary>
    public static BrowserAutomationProfile? TryReadProfile(AutomationLibraryRecord record)
    {
        if (record.SchemaVersion != SettingsVersionValue
            || !string.Equals(record.ModuleId, IdValue, StringComparison.Ordinal)
            || !string.Equals(record.Collection, ProfileCollection, StringComparison.Ordinal)) return null;
        try
        {
            var profile = record.Data.Deserialize<BrowserAutomationProfile>(JsonOptions);
            if (profile is null || !string.Equals(profile.Id, record.Id, StringComparison.Ordinal) || Validate(profile).Count > 0)
                return null;
            return NormalizeAndValidate(profile);
        }
        catch (Exception exception) when (exception is JsonException or BrowserProfileValidationException or ArgumentException)
        {
            return null;
        }
    }

    private static string ActionLabel(AutomationBrowserActionKind kind) => kind switch
    {
        AutomationBrowserActionKind.Navigate => "Navigation",
        AutomationBrowserActionKind.Click => "Click",
        AutomationBrowserActionKind.Fill => "Fill",
        AutomationBrowserActionKind.Select => "Select",
        AutomationBrowserActionKind.WaitFor => "Wait",
        AutomationBrowserActionKind.ReadTitle => "Title read",
        AutomationBrowserActionKind.ReadText => "Text read",
        AutomationBrowserActionKind.ListLinks => "Link listing",
        _ => "Action",
    };

    private static string TruncateUtf8(string value, int maximumBytes)
    {
        if (Encoding.UTF8.GetByteCount(value) <= maximumBytes) return value;
        var result = new StringBuilder(Math.Min(value.Length, maximumBytes));
        var used = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            if (used + rune.Utf8SequenceLength > maximumBytes) break;
            result.Append(rune.ToString());
            used += rune.Utf8SequenceLength;
        }
        return result.ToString();
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

    private sealed class BrowserProfileValidationException(string message) : Exception(message);
}

/// <summary>Host-constructed adapter for Browser Automation profiles invoked by workflows.</summary>
public sealed class BrowserSavedProfileHandler(
    IAutomationLibraryStore libraryStore,
    IAutomationBrowserService browser,
    IAutomationVariableProvider? variables = null) : IAutomationSavedProfileHandler
{
    private const int MaximumInputBytes = 48 * 1024;
    private const int MaximumOutputBytes = AutomationSavedProfileExecutor.MaximumStructuredDataBytes - 1024;
    private const int MaximumTextBytes = 128 * 1024;

    public string ModuleId => BrowserAutomationModule.IdValue;

    public async Task<IReadOnlyList<AutomationSavedProfileSummary>> ListProfilesAsync(CancellationToken cancellationToken)
    {
        var records = await libraryStore.ListAsync(ModuleId, BrowserAutomationModule.ProfileCollection, cancellationToken).ConfigureAwait(false);
        return Array.AsReadOnly(records.Select(BrowserAutomationModule.TryReadProfile).Where(profile => profile is not null)
            .Select(profile => new AutomationSavedProfileSummary(profile!.Id, profile.Name)).ToArray());
    }

    public async Task<AutomationProfileExecutionOutput> ExecuteAsync(
        string profileId,
        JsonElement? input,
        AutomationExecutionMetadata metadata,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        if (input is { } inputValue && (inputValue.ValueKind == JsonValueKind.Undefined
            || Encoding.UTF8.GetByteCount(inputValue.GetRawText()) > MaximumInputBytes))
            throw new InvalidDataException("Browser profile input is invalid or too large.");

        var startedAt = System.Diagnostics.Stopwatch.StartNew();
        var record = await libraryStore.GetAsync(ModuleId, BrowserAutomationModule.ProfileCollection, profileId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("The selected browser profile does not exist.");
        var profile = BrowserAutomationModule.TryReadProfile(record);
        if (profile is null || !string.Equals(profile.Id, profileId, StringComparison.Ordinal))
            throw new InvalidDataException("The selected browser profile is invalid.");

        var action = input is null
            ? new AutomationBrowserAction(AutomationBrowserActionKind.Navigate, profile.StartUrl)
            : DeserializeAction(input.Value, profile);
        if (variables is not null && action.Value is not null)
        {
            var globals = await variables.GetAsync(cancellationToken).ConfigureAwait(false);
            action = action with { Value = AutomationVariableInterpolation.Expand(action.Value, globals.Values) };
            if (action.Value.Length > 32 * 1024) throw new InvalidDataException("The expanded browser action value is too large.");
        }
        var result = await browser.ExecuteAsync(profile.Id, action, cancellationToken).ConfigureAwait(false);
        var output = BoundedOutput(result, profile);
        var summary = new AutomationExecutionSummary(AutomationStatus.Success, "browser.completed",
            Math.Min((long)startedAt.Elapsed.TotalMilliseconds, 86_400_000));
        return new AutomationProfileExecutionOutput(output, summary);
    }

    private static AutomationBrowserAction DeserializeAction(JsonElement input, BrowserAutomationProfile profile)
    {
        if (input.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Browser workflow input must be one action object.");
        try
        {
            var action = JsonSerializer.Deserialize<AutomationBrowserAction>(input.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            }) ?? throw new InvalidDataException("Browser workflow action is invalid.");
            if (!Enum.IsDefined(action.Kind) || action.TimeoutMilliseconds is < 250 or > 30_000)
                throw new InvalidDataException("Browser workflow action is unsupported.");
            if (action.Kind == AutomationBrowserActionKind.Navigate && !IsAllowedHost(action.Url, profile.AllowedHosts))
                throw new InvalidDataException("Browser workflow URL is outside the profile's allowed hosts.");
            if (action.Kind is AutomationBrowserActionKind.Click or AutomationBrowserActionKind.Fill or AutomationBrowserActionKind.Select
                or AutomationBrowserActionKind.WaitFor or AutomationBrowserActionKind.ReadText)
            {
                if (action.LocatorKind is not { } locatorKind || !Enum.IsDefined(locatorKind)
                    || string.IsNullOrWhiteSpace(action.Locator) || action.Locator.Length > 2048)
                    throw new InvalidDataException("Browser workflow locator is invalid.");
            }
            if (action.Kind is AutomationBrowserActionKind.Fill or AutomationBrowserActionKind.Select
                && (action.Value is null || action.Value.Length > 32 * 1024))
                throw new InvalidDataException("Browser workflow action value is invalid.");
            if (action.Value is { Length: > 32 * 1024 } || action.Url is { Length: > 4096 })
                throw new InvalidDataException("Browser workflow action is too large.");
            return action;
        }
        catch (JsonException)
        {
            throw new InvalidDataException("Browser workflow action is invalid.");
        }
    }

    private static JsonElement BoundedOutput(AutomationBrowserActionResult result, BrowserAutomationProfile profile)
    {
        if (result is null) throw new InvalidOperationException("The browser service returned no result.");
        var currentUrl = SafeUrl(result.CurrentUrl, 8192) ?? "about:blank";
        var title = Truncate(result.Title ?? string.Empty, 4096);
        var text = Truncate(result.Text ?? string.Empty, MaximumTextBytes);
        var links = (result.Links ?? []).Take(64)
            .Where(link => link is not null && !string.IsNullOrWhiteSpace(link.Url))
            .Select(link => new { text = Truncate(link.Text ?? string.Empty, 512), url = SafeUrl(link.Url, 2048) })
            .Where(link => link.url is not null && IsAllowedHost(link.url, profile.AllowedHosts))
            .ToList();
        while (true)
        {
            var output = JsonSerializer.SerializeToElement(new { currentUrl, title, text, links });
            if (Encoding.UTF8.GetByteCount(output.GetRawText()) <= MaximumOutputBytes) return output;
            if (Encoding.UTF8.GetByteCount(text) > 0)
                text = Truncate(text, Math.Max(1, Encoding.UTF8.GetByteCount(text) / 2));
            else if (links.Count > 0)
                links.RemoveRange(links.Count / 2, links.Count - links.Count / 2);
            else if (Encoding.UTF8.GetByteCount(title) > 0)
                title = Truncate(title, Math.Max(1, Encoding.UTF8.GetByteCount(title) / 2));
            else
                throw new InvalidOperationException("The browser result metadata exceeds the supported size.");
        }
    }

    private static bool IsAllowedHost(string? value, IReadOnlyList<string> allowedHosts)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(uri.UserInfo)) return false;
        try
        {
            var host = NormalizeHost(uri.IdnHost);
            return allowedHosts.Any(allowed => string.Equals(NormalizeHost(allowed), host, StringComparison.OrdinalIgnoreCase));
        }
        catch (ArgumentException) { return false; }
    }

    private static string? SafeUrl(string? value, int maximumLength)
    {
        if (value is null || value.Length > maximumLength) return null;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return new UriBuilder(uri) { UserName = string.Empty, Password = string.Empty }.Uri.AbsoluteUri;
        return string.Equals(value, "about:blank", StringComparison.OrdinalIgnoreCase) ? "about:blank" : null;
    }

    private static string NormalizeHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("Host is required.");
        var value = host.Trim().TrimEnd('.');
        if (IPAddress.TryParse(value, out var address)) return address.ToString().ToLowerInvariant();
        if (value.Contains('*') || value.Contains(':') || value.Any(char.IsWhiteSpace)) throw new ArgumentException("Host is invalid.");
        return new IdnMapping().GetAscii(value).ToLowerInvariant();
    }

    private static string Truncate(string value, int maximumBytes)
    {
        if (Encoding.UTF8.GetByteCount(value) <= maximumBytes) return value;
        var result = new StringBuilder();
        var used = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            if (used + rune.Utf8SequenceLength > maximumBytes) break;
            result.Append(rune.ToString());
            used += rune.Utf8SequenceLength;
        }
        return result.ToString();
    }
}
