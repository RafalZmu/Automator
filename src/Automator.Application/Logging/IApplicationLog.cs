namespace Automator.Application.Logging;

public enum ApplicationLogLevel
{
    Information,
    Warning,
    Error,
    Critical
}

public interface IApplicationLog
{
    void Write(
        ApplicationLogLevel level,
        string eventName,
        string message,
        Exception? exception = null,
        IReadOnlyDictionary<string, object?>? properties = null);
}

public sealed class NullApplicationLog : IApplicationLog
{
    public static NullApplicationLog Instance { get; } = new();

    private NullApplicationLog() { }

    public void Write(ApplicationLogLevel level, string eventName, string message, Exception? exception = null,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
    }
}
