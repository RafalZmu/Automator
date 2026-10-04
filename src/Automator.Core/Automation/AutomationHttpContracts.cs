namespace Automator.Core.Automation;

public sealed record AutomationHttpRequest(
    string Uri,
    string Method,
    IReadOnlyDictionary<string, string> Headers,
    string? Body);

public sealed record AutomationHttpResult(
    int StatusCode,
    IReadOnlyDictionary<string, string> Headers,
    string ContentType,
    string BodyText);

public enum AutomationServiceErrorCategory
{
    UnsupportedScheme,
    HostNotAllowed,
    NetworkNotAllowed,
    RequestTooLarge,
    ResponseTooLarge,
    TimedOut,
    Canceled,
    TransportFailure,
    InvalidResponse,
}

/// <summary>A normalized service failure whose message must not include request credentials or bodies.</summary>
public sealed class AutomationServiceException(AutomationServiceErrorCategory category, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public AutomationServiceErrorCategory Category { get; } = category;
}
