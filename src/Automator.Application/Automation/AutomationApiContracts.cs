using System.Text.Json;

namespace Automator.Application.Automation;

public sealed record AutomationApiProfileResponse(
    int StatusCode,
    string ContentType,
    IReadOnlyDictionary<string, string> Headers,
    string BodyText,
    long DurationMilliseconds);

/// <summary>Renderer-facing secret operations. Secret values can be set, checked, or deleted, never read.</summary>
public interface IAutomationSecretManager
{
    Task SetAsync(string profileId, string secretId, string value, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string profileId, string secretId, CancellationToken cancellationToken);
    Task DeleteAsync(string profileId, string secretId, CancellationToken cancellationToken);
}

/// <summary>Runs an API profile loaded and authorized by the host, not a renderer-supplied request policy.</summary>
public interface IAutomationApiProfileRunner
{
    Task<AutomationApiProfileResponse> RunProfileAsync(string profileId, JsonElement? input, CancellationToken cancellationToken);
}
