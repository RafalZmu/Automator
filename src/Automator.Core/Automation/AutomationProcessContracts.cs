namespace Automator.Core.Automation;

/// <summary>Arguments are kept separate from the executable command line to avoid shell interpolation.</summary>
public sealed record AutomationProcessRequest(
    string ExecutablePath,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    TimeSpan Timeout,
    string? StandardInput = null,
    IReadOnlyDictionary<string, string>? EnvironmentVariables = null);

public sealed record AutomationProcessResult(
    int? ExitCode,
    bool TimedOut,
    string StandardOutput,
    string StandardError,
    bool StandardOutputTruncated,
    bool StandardErrorTruncated,
    long DurationMilliseconds);

public static class AutomationProcessLimits
{
    public const int MaximumArgumentCount = 64;
    public const int MaximumArgumentLength = 8 * 1024;
    public const int MaximumStandardInputCharacters = 1024 * 1024;
    public const int MaximumStandardInputBytes = 1024 * 1024;
    public const int MaximumEnvironmentVariableCount = 32;
    public const int MaximumEnvironmentKeyLength = 128;
    public const int MaximumEnvironmentValueLength = 48 * 1024;
    public const int MaximumOutputCharacters = 64 * 1024;
    public static readonly TimeSpan MaximumTimeout = TimeSpan.FromHours(1);

    public static void ValidateEnvironmentVariables(IReadOnlyDictionary<string, string>? environment)
    {
        if (environment is null) return;
        if (environment.Count > MaximumEnvironmentVariableCount
            || environment.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Key.Length > MaximumEnvironmentKeyLength
                || pair.Key.Any(character => !(char.IsAsciiLetterOrDigit(character) || character == '_'))
                || pair.Value is null || pair.Value.Length > MaximumEnvironmentValueLength))
            throw new InvalidDataException("The process environment variable map is invalid or too large.");
    }
}
