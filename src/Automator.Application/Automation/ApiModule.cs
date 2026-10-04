using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Automator.Application.Launcher;
using Automator.Core.Automation;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

public enum ApiResponseMode
{
    Text,
    Json,
}

/// <summary>Persisted API profile. SecretHeaders maps HTTP header names to opaque vault IDs.</summary>
public sealed record ApiProfile(
    string Id,
    string Name,
    string Method,
    string Url,
    bool AllowLocalNetwork,
    IReadOnlyDictionary<string, string> Headers,
    IReadOnlyDictionary<string, string> SecretHeaders,
    string? BodyTemplate,
    ApiResponseMode ResponseMode,
    int TimeoutSeconds,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] JsonElement DefaultInput = default);

/// <summary>Bundled API tab. The host profile runner owns policy enforcement and secret injection.</summary>
public sealed class ApiModule : ILauncherTabModuleProvider
{
    public const string IdValue = "api";
    public const int ContractVersionValue = 1;
    public const int SettingsVersionValue = 1;
    public const string ProfileCollection = "profiles";
    private const int MaximumInputBytes = 512 * 1024;
    private const int MaximumOutputBytes = AutomationSavedProfileExecutor.MaximumStructuredDataBytes - 1024;
    private const int MaximumOutputBodyBytes = 384 * 1024;
    private static readonly Regex ProfileIdPattern = new("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex SecretIdPattern = new("^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex HeaderNamePattern = new("^[!#$%&'*+.^_`|~0-9A-Za-z-]+$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly HashSet<string> SecretOnlyHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "Proxy-Authorization", "Cookie", "Set-Cookie",
    };
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private static readonly AutomationCapabilityRequirement LibraryCapability = new(AutomationCapabilityIds.LibraryStorage, 1);
    private static readonly AutomationCapabilityRequirement SecretsCapability = new(AutomationCapabilityIds.SecretManagement, 1);
    private static readonly AutomationCapabilityRequirement ProfileHttpCapability = new(AutomationCapabilityIds.ApiProfileHttp, 1);

    public AutomationModuleDefinition Definition { get; } = new(
        3,
        IdValue,
        "API",
        "globe",
        "api",
        false,
        ContractVersionValue,
        SettingsVersionValue,
        [LibraryCapability, SecretsCapability, ProfileHttpCapability],
        [
            new("listProfiles", 1, [LibraryCapability, SecretsCapability]),
            new("saveProfile", 1, [LibraryCapability]),
            new("deleteProfile", 1, [LibraryCapability, SecretsCapability]),
            new("setSecret", 1, [LibraryCapability, SecretsCapability]),
            new("clearSecret", 1, [LibraryCapability, SecretsCapability]),
            new("runProfile", 1, [LibraryCapability, ProfileHttpCapability]),
        ]);

    public int ContractVersion => Definition.ContractVersion;
    public string Id => Definition.Id;
    public string Title => Definition.Title;

    public LauncherModuleState CreateInitialState() =>
        new(Id, LauncherTabRegistry.Version, new Dictionary<string, string>(StringComparer.Ordinal) { ["status"] = "ready" });

    public JsonElement CreateDefaultSettings() => JsonSerializer.SerializeToElement(new { });

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
                "setSecret" => await SetSecretAsync(input, services, cancellationToken).ConfigureAwait(false),
                "clearSecret" => await ClearSecretAsync(input, services, cancellationToken).ConfigureAwait(false),
                "runProfile" => await RunProfileAsync(input, services, cancellationToken).ConfigureAwait(false),
                _ => Error("Unknown API profile action."),
            };
        }
        catch (OperationCanceledException) { throw; }
        catch (ApiProfileValidationException exception) { return Error(exception.Message); }
        catch (AutomationServiceException exception) { return Error(exception.Message); }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or ArgumentException or InvalidOperationException)
        {
            return Error(exception.Message);
        }
        catch
        {
            // Credential Manager and transport exceptions may contain request or credential details.
            return Error(actionId == "setSecret" ? "The API secret could not be stored." : "The API profile action failed.");
        }
    }

    private static async Task<AutomationResult> ListProfilesAsync(AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var records = await services.Library!.ListAsync(ProfileCollection, cancellationToken).ConfigureAwait(false);
        var profiles = records.Select(TryReadProfile).Where(profile => profile is not null).Cast<ApiProfile>().ToArray();
        var configured = new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);
        foreach (var profile in profiles)
        {
            var status = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var (headerName, secretId) in profile.SecretHeaders)
                status[headerName] = await services.Secrets!.ExistsAsync(profile.Id, secretId, cancellationToken).ConfigureAwait(false);
            configured[profile.Id] = status;
        }

        return Result(AutomationStatus.Success, $"{profiles.Length} saved API profile{(profiles.Length == 1 ? string.Empty : "s")}.",
            new { profiles, secretStatus = configured });
    }

    private static async Task<AutomationResult> SaveProfileAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        ApiProfile profile;
        try
        {
            profile = JsonSerializer.Deserialize<ApiProfile>(input.GetRawText(), JsonOptions)
                ?? throw new ApiProfileValidationException("The API profile is empty or invalid.");
        }
        catch (JsonException)
        {
            throw new ApiProfileValidationException("The API profile data is invalid.");
        }

        profile = NormalizeAndValidate(profile);
        var previous = await services.Library!.GetAsync(ProfileCollection, profile.Id, cancellationToken).ConfigureAwait(false);
        var previousProfile = previous is null ? null : TryReadProfile(previous);
        await services.Library.UpsertAsync(ProfileCollection, profile.Id, SettingsVersionValue,
            JsonSerializer.SerializeToElement(profile, JsonOptions), cancellationToken).ConfigureAwait(false);

        var currentSecretIds = new HashSet<string>(profile.SecretHeaders.Values, StringComparer.Ordinal);
        var staleSecrets = previousProfile is null ? [] : previousProfile.SecretHeaders.Values
            .Where(secretId => !currentSecretIds.Contains(secretId)).Distinct(StringComparer.Ordinal).ToArray();
        var cleanupFailed = await DeleteSecretsAsync(services.Secrets!, profile.Id, staleSecrets, cancellationToken).ConfigureAwait(false);
        return Result(cleanupFailed ? AutomationStatus.Warning : AutomationStatus.Success,
            cleanupFailed ? $"Saved {profile.Name}; an unused credential could not be removed." : $"Saved {profile.Name}.",
            new { profile });
    }

    private static async Task<AutomationResult> DeleteProfileAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var id = ReadProfileId(input);
        var record = await services.Library!.GetAsync(ProfileCollection, id, cancellationToken).ConfigureAwait(false);
        if (record is null)
            return Result(AutomationStatus.Information, "API profile was already removed.", new { id, deleted = false });

        var profile = TryReadProfile(record);
        var deleted = await services.Library.DeleteAsync(ProfileCollection, id, cancellationToken).ConfigureAwait(false);
        if (!deleted) return Result(AutomationStatus.Information, "API profile was already removed.", new { id, deleted = false });
        var cleanupFailed = profile is not null && await DeleteSecretsAsync(services.Secrets!, id,
            profile.SecretHeaders.Values.Distinct(StringComparer.Ordinal), cancellationToken).ConfigureAwait(false);
        return Result(cleanupFailed ? AutomationStatus.Warning : AutomationStatus.Success,
            cleanupFailed ? "Removed the API profile; an unused credential could not be removed." : "API profile removed.",
            new { id, deleted = true });
    }

    private static async Task<AutomationResult> SetSecretAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var (profileId, headerName, value) = ReadSecretInput(input);
        var profile = await RequireProfileAsync(profileId, services, cancellationToken).ConfigureAwait(false);
        if (!TryGetSecretId(profile, headerName, out var secretId))
            throw new ApiProfileValidationException("Add and save this secret header before setting its value.");
        if (string.IsNullOrWhiteSpace(value) || Encoding.UTF8.GetByteCount(value) > 16 * 1024)
            throw new ApiProfileValidationException("Secret value must be non-empty and at most 16 KiB.");

        try
        {
            await services.Secrets!.SetAsync(profile.Id, secretId, value, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            // Never surface a Credential Manager exception that could contain the submitted value.
            return Error("The API secret could not be stored.");
        }
        return Result(AutomationStatus.Success, $"Secret for {headerName} saved.", new { profileId, headerName, configured = true });
    }

    private static async Task<AutomationResult> ClearSecretAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var profileId = ReadProfileId(input);
        var headerName = ReadString(input, "headerName", maximumLength: 128);
        var profile = await RequireProfileAsync(profileId, services, cancellationToken).ConfigureAwait(false);
        if (!TryGetSecretId(profile, headerName, out var secretId))
            return Result(AutomationStatus.Information, "The profile has no secret for this header.", new { profileId, headerName, configured = false });

        var updatedSecrets = profile.SecretHeaders
            .Where(pair => !string.Equals(pair.Key, headerName, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var updatedProfile = profile with { SecretHeaders = updatedSecrets };
        await services.Library!.UpsertAsync(ProfileCollection, profile.Id, SettingsVersionValue,
            JsonSerializer.SerializeToElement(updatedProfile, JsonOptions), cancellationToken).ConfigureAwait(false);
        try
        {
            await services.Secrets!.DeleteAsync(profile.Id, secretId, cancellationToken).ConfigureAwait(false);
            return Result(AutomationStatus.Success, $"Secret binding for {headerName} cleared.", new { profileId, headerName, configured = false });
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            return Result(AutomationStatus.Warning, $"Secret binding for {headerName} was cleared; its unused credential could not be removed.",
                new { profileId, headerName, configured = false });
        }
    }

    private static async Task<AutomationResult> RunProfileAsync(
        JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var profileId = ReadProfileId(input);
        var profile = await RequireProfileAsync(profileId, services, cancellationToken).ConfigureAwait(false);
        var useDefaultInput = input.ValueKind == JsonValueKind.Object
            && input.TryGetProperty("useDefaultInput", out var useDefault)
            && useDefault.ValueKind == JsonValueKind.True;
        var requestInput = useDefaultInput
            ? profile.DefaultInput.ValueKind == JsonValueKind.Undefined ? null : profile.DefaultInput
            : ReadOptionalInput(input);
        var execution = await ExecuteProfileAsync(profile.Id, profile.ResponseMode, requestInput,
            services.ApiProfiles!, cancellationToken).ConfigureAwait(false);
        return Result(execution.Summary.Status, StatusMessage(profile.Name, execution.Response.StatusCode, execution.ParseFailure,
            execution.BodyTruncated), execution.Output,
            [new AutomationAction("runAgain", "Run again", 1,
                JsonSerializer.SerializeToElement(new { id = profile.Id, input = requestInput }, JsonOptions))]);
    }

    private static async Task<ApiProfile> RequireProfileAsync(string id, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var record = await services.Library!.GetAsync(ProfileCollection, id, cancellationToken).ConfigureAwait(false);
        var profile = record is null ? null : TryReadProfile(record);
        return profile is null ? throw new ApiProfileValidationException("The selected API profile does not exist or is invalid.")
            : NormalizeAndValidate(profile);
    }

    internal static async Task<ApiExecution> ExecuteProfileAsync(
        string profileId,
        ApiResponseMode responseMode,
        JsonElement? input,
        IAutomationApiProfileRunner runner,
        CancellationToken cancellationToken)
    {
        var response = await runner.RunProfileAsync(profileId, input, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is < 100 or > 599 || response.DurationMilliseconds < 0)
            throw new InvalidOperationException("The host returned invalid API response metadata.");

        var headers = response.Headers.Take(32)
            .Where(pair => pair.Key.Length <= 128)
            .ToDictionary(pair => pair.Key, pair => TruncateUtf8(pair.Value, 2048), StringComparer.OrdinalIgnoreCase);
        var body = TruncateUtf8(response.BodyText, MaximumOutputBodyBytes);
        var bodyTruncated = !string.Equals(body, response.BodyText, StringComparison.Ordinal);
        var decoded = DecodeResponse(responseMode, body, bodyTruncated);
        var output = CreateBoundedOutput(response, headers, profileId, responseMode, decoded, bodyTruncated,
            out bodyTruncated, out decoded);
        var status = response.StatusCode >= 500 ? AutomationStatus.Error
            : response.StatusCode is < 200 or >= 300 || decoded.ParseFailure || bodyTruncated ? AutomationStatus.Warning
            : AutomationStatus.Success;
        var category = response.StatusCode >= 500 ? "http.server_error"
            : response.StatusCode >= 400 ? "http.client_error"
            : response.StatusCode is < 200 or >= 300 ? "http.non_success"
            : decoded.ParseFailure ? "http.invalid_json"
            : bodyTruncated ? "http.response_truncated"
            : "http.success";
        return new ApiExecution(response, output, decoded.ParseFailure, bodyTruncated,
            new AutomationExecutionSummary(status, category, Math.Min(response.DurationMilliseconds, 86_400_000)));
    }

    private static JsonElement CreateBoundedOutput(
        AutomationApiProfileResponse response,
        IReadOnlyDictionary<string, string> headers,
        string profileId,
        ApiResponseMode responseMode,
        DecodedBody decoded,
        bool bodyTruncated,
        out bool finalBodyTruncated,
        out DecodedBody finalDecoded)
    {
        var body = decoded.BodyText;
        var parsed = decoded.StructuredOutput;
        var parseFailure = decoded.ParseFailure;
        var truncated = bodyTruncated;
        while (true)
        {
            var data = JsonSerializer.SerializeToElement(new
            {
                profileId,
                statusCode = response.StatusCode,
                contentType = TruncateUtf8(response.ContentType, 512),
                headers,
                durationMilliseconds = Math.Min(response.DurationMilliseconds, 86_400_000),
                responseMode = responseMode.ToString().ToLowerInvariant(),
                bodyText = body,
                structuredOutput = parsed,
                structuredOutputPresent = parsed.HasValue,
                parseFailure,
                bodyTruncated = truncated,
            }, JsonOptions);
            if (Encoding.UTF8.GetByteCount(data.GetRawText()) <= MaximumOutputBytes)
            {
                finalBodyTruncated = truncated;
                finalDecoded = new DecodedBody(body, parsed, parseFailure);
                return data;
            }

            if (parsed is not null)
            {
                body = TruncateUtf8(response.BodyText, MaximumOutputBodyBytes / 2);
                parsed = null;
                parseFailure = false;
                truncated = true;
            }
            else if (body.Length > 0)
            {
                body = TruncateUtf8(body, Math.Max(1, Encoding.UTF8.GetByteCount(body) / 2));
                truncated = true;
            }
            else
            {
                throw new InvalidOperationException("API response metadata exceeds the module result limit.");
            }
        }
    }

    private static DecodedBody DecodeResponse(ApiResponseMode responseMode, string body, bool alreadyTruncated)
    {
        if (responseMode != ApiResponseMode.Json || alreadyTruncated)
            return new DecodedBody(body, null, false);
        try
        {
            using var document = JsonDocument.Parse(body);
            return new DecodedBody(string.Empty, document.RootElement.Clone(), false);
        }
        catch (JsonException)
        {
            return new DecodedBody(body, null, true);
        }
    }

    private static async Task<bool> DeleteSecretsAsync(
        IAutomationSecretManager secrets,
        string profileId,
        IEnumerable<string> secretIds,
        CancellationToken cancellationToken)
    {
        var failed = false;
        foreach (var secretId in secretIds)
        {
            try { await secrets.DeleteAsync(profileId, secretId, cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { throw; }
            catch { failed = true; }
        }
        return failed;
    }

    private static ApiProfile NormalizeAndValidate(ApiProfile profile)
    {
        var errors = Validate(profile);
        if (errors.Count > 0) throw new ApiProfileValidationException(string.Join(" ", errors));
        return profile with
        {
            Id = profile.Id.Trim().ToLowerInvariant(),
            Name = profile.Name.Trim(),
            Method = profile.Method.Trim().ToUpperInvariant(),
            Url = profile.Url.Trim(),
            Headers = profile.Headers.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
            SecretHeaders = profile.SecretHeaders.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
        };
    }

    private static IReadOnlyList<string> Validate(ApiProfile profile)
    {
        var errors = new List<string>();
        if (profile is null) return ["The API profile is missing."];
        if (!ProfileIdPattern.IsMatch(profile.Id ?? string.Empty)) errors.Add("Profile key must be a short lowercase identifier.");
        if (string.IsNullOrWhiteSpace(profile.Name) || profile.Name.Length > 128) errors.Add("Name is required and must be at most 128 characters.");
        var method = profile.Method?.Trim().ToUpperInvariant() ?? string.Empty;
        if (method is not ("GET" or "POST" or "PUT" or "PATCH" or "DELETE" or "HEAD" or "OPTIONS"))
            errors.Add("Choose a supported HTTP method.");
        if (!Uri.TryCreate(profile.Url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(uri.UserInfo) || string.IsNullOrWhiteSpace(uri.DnsSafeHost))
            errors.Add("URL must be an absolute HTTP or HTTPS address without credentials.");

        if (profile.Headers is null || profile.Headers.Count > 64) errors.Add("Ordinary headers are invalid or exceed 64 entries.");
        else
        {
            foreach (var (name, value) in profile.Headers)
            {
                if (!HeaderNamePattern.IsMatch(name) || name.Length > 128) errors.Add($"Header name '{name}' is invalid.");
                if (value is null || value.Length > 8192) errors.Add($"Header '{name}' has an invalid value.");
                if (SecretOnlyHeaders.Contains(name)) errors.Add($"Header '{name}' must be stored as a secret header.");
            }
            if (profile.Headers.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != profile.Headers.Count)
                errors.Add("Header names must be unique regardless of letter casing.");
        }

        if (profile.SecretHeaders is null || profile.SecretHeaders.Count > 64) errors.Add("Secret headers are invalid or exceed 64 entries.");
        else
        {
            var ordinaryNames = new HashSet<string>(profile.Headers?.Keys ?? [], StringComparer.OrdinalIgnoreCase);
            var secretNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var secretIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (name, secretId) in profile.SecretHeaders)
            {
                if (!HeaderNamePattern.IsMatch(name) || name.Length > 128) errors.Add($"Secret header name '{name}' is invalid.");
                if (!SecretIdPattern.IsMatch(secretId ?? string.Empty)) errors.Add($"Secret reference for '{name}' is invalid.");
                if (ordinaryNames.Contains(name) || !secretNames.Add(name)) errors.Add($"Secret header '{name}' duplicates another header.");
                if (!secretIds.Add(secretId ?? string.Empty)) errors.Add("Each secret header must use a distinct secret reference.");
            }
        }

        if (profile.BodyTemplate is { } body && Encoding.UTF8.GetByteCount(body) > 1024 * 1024)
            errors.Add("Body template must be at most 1 MiB.");
        if (profile.DefaultInput.ValueKind != JsonValueKind.Undefined
            && Encoding.UTF8.GetByteCount(profile.DefaultInput.GetRawText()) > MaximumInputBytes)
            errors.Add("Default JSON input must be valid JSON data and at most 512 KiB.");
        if (!Enum.IsDefined(profile.ResponseMode)) errors.Add("Choose text or JSON response mode.");
        if (profile.TimeoutSeconds is < 1 or > 3600) errors.Add("Timeout must be between 1 and 3600 seconds.");
        return errors.Distinct(StringComparer.Ordinal).ToArray();
    }

    /// <summary>Reads only valid schema-v1 profiles; host HTTP policy resolution uses this exact validator.</summary>
    public static ApiProfile? TryReadProfile(AutomationLibraryRecord record)
    {
        if (record.SchemaVersion != SettingsVersionValue) return null;
        try
        {
            var profile = record.Data.Deserialize<ApiProfile>(JsonOptions);
            return profile is null || profile.Id != record.Id || Validate(profile).Count > 0 ? null : profile;
        }
        catch (JsonException) { return null; }
    }

    private static bool TryGetSecretId(ApiProfile profile, string headerName, out string secretId)
    {
        var match = profile.SecretHeaders.FirstOrDefault(pair => string.Equals(pair.Key, headerName, StringComparison.OrdinalIgnoreCase));
        secretId = match.Value ?? string.Empty;
        return match.Key is not null;
    }

    private static string ReadProfileId(JsonElement input)
    {
        var id = ReadString(input, "id", 64);
        if (!ProfileIdPattern.IsMatch(id)) throw new ApiProfileValidationException("Profile key is invalid.");
        return id;
    }

    private static (string ProfileId, string HeaderName, string Value) ReadSecretInput(JsonElement input)
    {
        var profileId = ReadProfileId(input);
        var headerName = ReadString(input, "headerName", 128);
        var value = ReadString(input, "value", 16 * 1024);
        if (!HeaderNamePattern.IsMatch(headerName)) throw new ApiProfileValidationException("Secret header name is invalid.");
        return (profileId, headerName, value);
    }

    private static string ReadString(JsonElement input, string propertyName, int maximumLength)
    {
        if (input.ValueKind != JsonValueKind.Object || !input.TryGetProperty(propertyName, out var element)
            || element.ValueKind != JsonValueKind.String)
            throw new ApiProfileValidationException($"A valid {propertyName} is required.");
        var value = element.GetString()!;
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength)
            throw new ApiProfileValidationException($"{propertyName} is empty or too long.");
        return value;
    }

    private static JsonElement? ReadOptionalInput(JsonElement input)
    {
        if (!input.TryGetProperty("input", out var value)) return null;
        if (value.ValueKind == JsonValueKind.Undefined || Encoding.UTF8.GetByteCount(value.GetRawText()) > MaximumInputBytes)
            throw new ApiProfileValidationException("Request input is missing or exceeds 512 KiB.");
        return value.Clone();
    }

    private static string TruncateUtf8(string value, int maximumBytes)
    {
        if (Encoding.UTF8.GetByteCount(value) <= maximumBytes) return value;
        var builder = new StringBuilder(Math.Min(value.Length, maximumBytes));
        var used = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            if (used + rune.Utf8SequenceLength > maximumBytes) break;
            builder.Append(rune.ToString());
            used += rune.Utf8SequenceLength;
        }
        return builder.ToString();
    }

    private static string StatusMessage(string profileName, int statusCode, bool parseFailure, bool bodyTruncated)
    {
        if (bodyTruncated) return $"{profileName} completed; the response was shortened to fit the result limit.";
        if (parseFailure) return $"{profileName} completed, but its response was not valid JSON.";
        if (statusCode is < 200 or >= 300) return $"{profileName} returned HTTP {statusCode}.";
        return $"{profileName} returned HTTP {statusCode}.";
    }

    private static AutomationResult Error(string message) => Result(AutomationStatus.Error, message, new { });

    private static AutomationResult Result(AutomationStatus status, string message, object data,
        IReadOnlyList<AutomationAction>? actions = null) => new(
            AutomationTabContract.CurrentVersion,
            status,
            message,
            JsonSerializer.SerializeToElement(data, JsonOptions),
            actions ?? []);

    internal sealed record DecodedBody(string BodyText, JsonElement? StructuredOutput, bool ParseFailure);
    internal sealed record ApiExecution(
        AutomationApiProfileResponse Response,
        JsonElement Output,
        bool ParseFailure,
        bool BodyTruncated,
        AutomationExecutionSummary Summary);

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private sealed class ApiProfileValidationException(string message) : Exception(message);
}

/// <summary>Host-constructed workflow adapter; it exposes only safe profile names and bounded outputs.</summary>
public sealed class ApiSavedProfileHandler(
    IAutomationApiProfileRunner apiProfiles,
    IAutomationLibraryStore libraryStore) : IAutomationSavedProfileHandler
{
    public string ModuleId => ApiModule.IdValue;

    public async Task<IReadOnlyList<AutomationSavedProfileSummary>> ListProfilesAsync(CancellationToken cancellationToken)
    {
        var records = await libraryStore.ListAsync(ApiModule.IdValue, ApiModule.ProfileCollection, cancellationToken).ConfigureAwait(false);
        return records
            .Select(ApiModule.TryReadProfile)
            .Where(profile => profile is not null)
            .Select(profile => new AutomationSavedProfileSummary(profile!.Id, profile.Name))
            .ToArray();
    }

    public async Task<AutomationProfileExecutionOutput> ExecuteAsync(
        string profileId,
        JsonElement? input,
        AutomationExecutionMetadata metadata,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        if (!ProfileIdPattern.IsMatch(profileId)) throw new InvalidDataException("The API profile id is invalid.");
        if (input is { } value && (value.ValueKind == JsonValueKind.Undefined || Encoding.UTF8.GetByteCount(value.GetRawText()) > MaximumInputBytes))
            throw new InvalidDataException("The API profile input is invalid or too large.");
        var record = await libraryStore.GetAsync(ApiModule.IdValue, ApiModule.ProfileCollection, profileId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("The selected API profile does not exist.");
        var profile = ApiModule.TryReadProfile(record);
        if (profile is null || profile.Id != profileId) throw new InvalidDataException("The selected API profile is invalid.");
        var execution = await ApiModule.ExecuteProfileAsync(profileId, profile.ResponseMode, input?.Clone(), apiProfiles, cancellationToken).ConfigureAwait(false);
        return new AutomationProfileExecutionOutput(execution.Output, execution.Summary);
    }

    private static readonly Regex ProfileIdPattern = new("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private const int MaximumInputBytes = 512 * 1024;
}
