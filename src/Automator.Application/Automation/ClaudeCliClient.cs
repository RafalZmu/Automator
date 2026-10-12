using System.Text.Json;
using Automator.Core.Automation;

namespace Automator.Application.Automation;

/// <summary>Discovers and invokes the installed Claude Code CLI using structured process arguments.</summary>
public sealed class ClaudeCliClient : IAuthoringCliClient
{
    public const string Model = "claude-haiku-5-5";
    private readonly string _requestRoot;
    private readonly string? _launcherPath;
    private readonly IAutomationProcessService _processService;

    public ClaudeCliClient(string requestRoot, IAutomationProcessService processService, string? launcherPath = null)
    {
        _requestRoot = Path.GetFullPath(requestRoot);
        _processService = processService ?? throw new ArgumentNullException(nameof(processService));
        _launcherPath = launcherPath ?? DiscoverLauncher();
    }

    public bool IsAvailable => _launcherPath is not null;

    public async Task<CodexTaskStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        if (_launcherPath is null) return new(false, "unavailable", "Claude CLI was not found. Install Claude Code and try again.");
        try
        {
            var version = await RunAsync(["--version"], null, cancellationToken, null, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            if (version.ExitCode != 0) return new(false, "unavailable", "Claude CLI could not be started.");
            var auth = await RunAsync(["auth", "status"], null, cancellationToken, null, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            if (auth.ExitCode != 0 || LooksSignedOut(auth.Stdout + " " + auth.Stderr))
                return new(false, "signedOut", "Claude CLI is installed but signed out. Sign in with the Claude CLI, then retry.");
            return new(true, "ready", "Claude CLI is ready.", Bound(version.Stdout.Trim(), 128));
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or TimeoutException or InvalidDataException)
        {
            return new(false, "unavailable", "Claude CLI could not be started.");
        }
    }

    public async Task<JsonElement> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        if (_launcherPath is null) throw new InvalidOperationException("Claude CLI is unavailable.");
        Directory.CreateDirectory(_requestRoot);
        var requestDirectory = Path.Combine(_requestRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(requestDirectory);
        try
        {
            var cliPrompt = "Create a task draft only. Do not execute the requested task or inspect its inputs. Return a plan and source. " + prompt;
            // No tools, MCP servers, slash commands, hooks, or saved sessions: the call only turns text into structured text.
            var args = new List<string>
            {
                "-p", "--model", Model, "--output-format", "json", "--json-schema", CodexCliClient.TaskSchema,
                "--tools", "", "--strict-mcp-config", "--disable-slash-commands", "--no-session-persistence",
                "--settings", "{\"disableAllHooks\":true}",
            };
            var result = await RunAsync(args, cliPrompt, cancellationToken, requestDirectory, TimeSpan.FromMinutes(20)).ConfigureAwait(false);
            if (result.ExitCode != 0)
            {
                if (LooksSignedOut(result.Stdout + " " + result.Stderr)) throw new InvalidOperationException("Claude CLI is signed out.");
                throw new InvalidOperationException("Claude CLI could not generate a draft.");
            }
            return CodexCliClient.ParseStructuredResult(ExtractStructuredOutput(result.Stdout), CodexCliClient.MaximumResultBytes);
        }
        finally
        {
            try { Directory.Delete(requestDirectory, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    /// <summary>Unwraps the CLI's JSON envelope: prefers structured_output and falls back to a JSON string in result.</summary>
    public static string ExtractStructuredOutput(string stdout)
    {
        try
        {
            using var document = JsonDocument.Parse(stdout, new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Claude result is not a JSON object.");
            if (root.TryGetProperty("is_error", out var isError) && isError.ValueKind == JsonValueKind.True)
                throw new InvalidOperationException("Claude CLI could not generate a draft.");
            if (root.TryGetProperty("structured_output", out var structured) && structured.ValueKind == JsonValueKind.Object)
                return structured.GetRawText();
            if (root.TryGetProperty("result", out var text) && text.ValueKind == JsonValueKind.String && text.GetString() is { } value)
                return value;
            throw new InvalidDataException("Claude result is missing structured output.");
        }
        catch (JsonException exception) { throw new InvalidDataException("Claude result is invalid or truncated JSON.", exception); }
    }

    private async Task<(int? ExitCode, string Stdout, string Stderr)> RunAsync(IReadOnlyList<string> args, string? stdin,
        CancellationToken cancellationToken, string? workingDirectory, TimeSpan timeout)
    {
        var result = await _processService.ExecuteAsync(new AutomationProcessRequest(_launcherPath!, args,
            workingDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), timeout, stdin), cancellationToken).ConfigureAwait(false);
        if (result.TimedOut) throw new TimeoutException("Claude CLI request timed out.");
        if (result.StandardOutputTruncated) throw new InvalidDataException("Claude CLI output exceeded the supported limit.");
        return (result.ExitCode, result.StandardOutput, Bound(result.StandardError, 16_384));
    }

    private static string? DiscoverLauncher()
    {
        var candidates = new List<string>
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin", "claude.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "claude", "claude.exe"),
        };
        foreach (var item in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            candidates.Add(Path.Combine(item, "claude.exe"));
        return candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
    }

    private static bool LooksSignedOut(string text) => text.Contains("not logged in", StringComparison.OrdinalIgnoreCase)
        || text.Contains("\"loggedIn\":false", StringComparison.OrdinalIgnoreCase) || text.Contains("\"loggedIn\": false", StringComparison.OrdinalIgnoreCase)
        || text.Contains("/login", StringComparison.OrdinalIgnoreCase);
    private static string Bound(string value, int max) => value.Length <= max ? value : value[..max];
}
