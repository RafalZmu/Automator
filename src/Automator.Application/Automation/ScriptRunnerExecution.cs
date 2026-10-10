using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Automator.Core.Automation;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

public sealed record CodexTaskApproval(string SourceHash, string ScopeHash, string DraftId, IReadOnlyList<string>? Scope = null,
    IReadOnlyList<CodexTaskInputDeclaration>? Inputs = null, IReadOnlyList<CodexTaskEffectDeclaration>? Effects = null);
public sealed record ScriptRunnerExecutionResult(JsonElement Output, AutomationExecutionSummary Summary,
    AutomationProcessResult ProcessResult, bool ParseFailure);

/// <summary>Shared Script Runner process and output path for interactive, workflow, and Codex draft runs.</summary>
public static class ScriptRunnerExecution
{
    public static string HashSource(string source) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    public static async Task<string> HashProfileRevisionAsync(ScriptRunnerProfile profile, CancellationToken cancellationToken)
    {
        var path = Path.GetFullPath(profile.ScriptPath);
        var volume = Path.GetPathRoot(path)!;
        var current = volume;
        foreach (var part in path[volume.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Approved generated script paths cannot use a reparse point.");
        }
        var info = new FileInfo(path);
        if (!info.Exists || info.Length > 256 * 1024)
            throw new InvalidDataException("Approved generated script is missing or exceeds the 256 KiB limit.");
        var source = await File.ReadAllTextAsync(profile.ScriptPath, cancellationToken).ConfigureAwait(false);
        return HashProfileRevision(profile, source);
    }

    public static string HashProfileRevision(ScriptRunnerProfile profile, string source)
    {
        var definition = JsonSerializer.Serialize(new { source = HashSource(source), profile.Interpreter, profile.InterpreterPath,
            profile.ScriptPath, profile.Arguments, profile.WorkingDirectory, profile.OutputMode, profile.TimeoutSeconds });
        return HashSource(definition);
    }
    public static string HashScope(IReadOnlyList<string> scope) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(scope.Order(StringComparer.Ordinal)))));
    public static string HashReview(IReadOnlyList<string> scope, IReadOnlyList<CodexTaskInputDeclaration>? inputs,
        IReadOnlyList<CodexTaskEffectDeclaration>? effects) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { scope, inputs = inputs ?? [], effects = effects ?? [] }))));

    public static async Task<ScriptRunnerExecutionResult> RunAsync(
        ScriptRunnerProfile profile, IAutomationProcessService processes, JsonElement? input,
        AutomationExecutionOrigin origin, string correlationId, CancellationToken cancellationToken)
    {
        ScriptRunnerModule.Validate(profile);
        if (profile.CodexApproval is { } approval)
        {
            var revision = await HashProfileRevisionAsync(profile, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(revision, approval.SourceHash, StringComparison.Ordinal)
                || approval.Scope is null || !string.Equals(HashReview(approval.Scope, approval.Inputs, approval.Effects), approval.ScopeHash, StringComparison.Ordinal))
                throw new InvalidDataException("This generated task changed after approval. Review and approve its current source before running it.");
        }
        var arguments = profile.Interpreter == ScriptRunnerInterpreter.Powershell
            ? new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-File", profile.ScriptPath }.Concat(profile.Arguments).ToArray()
            : new[] { profile.ScriptPath }.Concat(profile.Arguments).ToArray();
        var processResult = await processes.ExecuteAsync(new AutomationProcessRequest(
            profile.InterpreterPath, arguments, profile.WorkingDirectory, TimeSpan.FromSeconds(profile.TimeoutSeconds),
            input?.GetRawText()), cancellationToken).ConfigureAwait(false);
        JsonElement output;
        var invalidJson = false;
        if (profile.OutputMode == ScriptRunnerOutputMode.Json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(processResult.StandardOutput)) throw new JsonException();
                using var document = JsonDocument.Parse(processResult.StandardOutput);
                output = document.RootElement.Clone();
            }
            catch (JsonException) { invalidJson = true; output = TextOutput(profile.Id, processResult); }
        }
        else output = TextOutput(profile.Id, processResult);
        var status = processResult.TimedOut ? AutomationStatus.Warning : processResult.ExitCode != 0 ? AutomationStatus.Error
            : invalidJson ? AutomationStatus.Warning : AutomationStatus.Success;
        var category = processResult.TimedOut ? "timed-out" : processResult.ExitCode != 0 ? "nonzero-exit"
            : invalidJson ? "invalid-json" : "completed";
        return new(output, new AutomationExecutionSummary(status, category, processResult.DurationMilliseconds), processResult, invalidJson);
    }

    private static JsonElement TextOutput(string profileId, AutomationProcessResult processResult) => JsonSerializer.SerializeToElement(new
    {
        profileId, exitCode = processResult.ExitCode, timedOut = processResult.TimedOut,
        stdout = processResult.StandardOutput, stderr = processResult.StandardError,
        stdoutTruncated = processResult.StandardOutputTruncated, stderrTruncated = processResult.StandardErrorTruncated,
    });
}
