namespace Automator.Core.Automation;

/// <summary>Arguments are kept separate from the executable command line to avoid shell interpolation.</summary>
public sealed record AutomationProcessRequest(
    string ExecutablePath,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    TimeSpan Timeout,
    string? StandardInput = null);

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
    public const int MaximumOutputCharacters = 64 * 1024;
    public static readonly TimeSpan MaximumTimeout = TimeSpan.FromHours(1);
}
