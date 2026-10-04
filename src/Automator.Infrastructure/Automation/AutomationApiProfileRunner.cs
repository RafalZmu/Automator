using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Automator.Application.Automation;
using Automator.Application.Logging;
using Automator.Core.Automation;

namespace Automator.Infrastructure.Automation;

/// <summary>Executes the API module's saved profiles through the shared policy-enforcing HTTP transport.</summary>
public sealed class AutomationApiProfileRunner(
    IAutomationLibraryStore library,
    IAutomationSecretValueReader secrets,
    SharedHttpService http,
    IApplicationLog log,
    IAutomationVariableProvider? variables = null) : IAutomationApiProfileRunner
{
    private const int MaximumInputBytes = 512 * 1024;
    private const int MaximumHeaderBytes = 64 * 1024;

    public async Task<AutomationApiProfileResponse> RunProfileAsync(
        string profileId,
        JsonElement? input,
        CancellationToken cancellationToken)
    {
        if (input is { } inputValue && (inputValue.ValueKind == JsonValueKind.Undefined
            || Encoding.UTF8.GetByteCount(inputValue.GetRawText()) > MaximumInputBytes))
            throw new InvalidDataException("API profile input is invalid or too large.");

        var record = await library.GetAsync(ApiModule.IdValue, ApiModule.ProfileCollection, profileId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("The selected API profile does not exist.");
        var profile = ApiModule.TryReadProfile(record)
            ?? throw new InvalidDataException("The saved API profile is invalid.");
        if (!string.Equals(profile.Id, profileId, StringComparison.Ordinal))
            throw new InvalidDataException("The saved API profile key is invalid.");

        var secretValues = new List<string>(profile.SecretHeaders.Count);
        var headers = new Dictionary<string, string>(profile.Headers, StringComparer.OrdinalIgnoreCase);
        foreach (var (headerName, secretId) in profile.SecretHeaders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var secret = await secrets.GetValueAsync(profile.Id, secretId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("An API profile is missing a required secret.");
            secretValues.Add(secret);
            headers.Add(headerName, secret);
        }

        var headerBytes = headers.Sum(pair => Encoding.UTF8.GetByteCount(pair.Key) + Encoding.UTF8.GetByteCount(pair.Value));
        if (headerBytes > MaximumHeaderBytes)
            throw new InvalidDataException("API profile headers exceed the supported size.");

        JsonElement? effectiveInput = input ?? (profile.DefaultInput.ValueKind == JsonValueKind.Undefined ? null : profile.DefaultInput);
        if (effectiveInput is { } defaultOrOverride
            && (defaultOrOverride.ValueKind == JsonValueKind.Undefined || Encoding.UTF8.GetByteCount(defaultOrOverride.GetRawText()) > MaximumInputBytes))
            throw new InvalidDataException("API profile input is invalid or too large.");
        var inputText = effectiveInput?.GetRawText();
        var template = profile.BodyTemplate;
        if (template is not null && variables is not null)
        {
            var globals = await variables.GetAsync(cancellationToken).ConfigureAwait(false);
            template = AutomationVariableInterpolation.Expand(template, globals.Values);
        }
        // Transient run input is data, so placeholders inside it are never interpreted.
        var body = template?.Replace("{{input}}", inputText ?? "null", StringComparison.Ordinal) ?? inputText;
        if (!Uri.TryCreate(profile.Url, UriKind.Absolute, out var profileUri)
            || (profileUri.Scheme != Uri.UriSchemeHttp && profileUri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrWhiteSpace(profileUri.DnsSafeHost))
            throw new InvalidDataException("The saved API profile URL is invalid.");
        // The exact host grant is derived only from this host-owned saved URL. Legacy allowedHosts
        // fields are ignored, so an imported profile cannot widen its redirect/network scope.
        var policy = new AutomationHttpPolicy([profileUri.DnsSafeHost.TrimEnd('.')], profile.AllowLocalNetwork);
        var transport = http.ForModule(ApiModule.IdValue, policy, TimeSpan.FromSeconds(profile.TimeoutSeconds));
        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            var response = await transport.SendAsync(new AutomationHttpRequest(profile.Url, profile.Method, headers, body),
                cancellationToken).ConfigureAwait(false);
            var redactionValues = secretValues.Where(value => value.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .OrderByDescending(value => value.Length)
                .ToArray();
            var responseBody = Redact(response.BodyText, redactionValues);
            var safeHeaders = response.Headers.ToDictionary(pair => pair.Key,
                pair => Redact(pair.Value, redactionValues), StringComparer.OrdinalIgnoreCase);
            var contentType = Redact(response.ContentType, redactionValues);
            var duration = (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            log.Write(ApplicationLogLevel.Information, "automation.api-profile.completed",
                "A saved API profile completed.", properties: new Dictionary<string, object?>
                {
                    ["profileId"] = profile.Id,
                    ["statusCode"] = response.StatusCode,
                    ["durationMs"] = duration
                });
            return new AutomationApiProfileResponse(response.StatusCode, contentType, safeHeaders, responseBody, duration);
        }
        catch (OperationCanceledException) { throw; }
        catch (AutomationServiceException) { throw; }
        catch
        {
            // Transport and credential error text can contain user-provided values; never log or rethrow it.
            log.Write(ApplicationLogLevel.Warning, "automation.api-profile.failed",
                "A saved API profile failed.", properties: new Dictionary<string, object?> { ["profileId"] = profile.Id });
            throw new InvalidOperationException("The API profile request failed.");
        }
    }

    private static string Redact(string value, IReadOnlyList<string> secrets)
    {
        foreach (var secret in secrets)
            value = value.Replace(secret, "[redacted]", StringComparison.Ordinal);
        return value;
    }
}
