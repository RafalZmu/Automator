using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Automator.Core.Automation;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

public sealed record PlaywrightTaskProfile(string Id, string Name, string RelativePath, string ProjectRoot,
    IReadOnlyList<CodexTaskInputDeclaration> Inputs, IReadOnlyList<string> Scope,
    IReadOnlyList<CodexTaskEffectDeclaration> Effects, string Plan, string CodeHash, CodexTaskApproval Approval);

/// <summary>Host-only workflow and Scheduler adapter for reviewed generated Playwright tasks.</summary>
public sealed class PlaywrightTaskSavedProfileHandler(IAutomationLibraryStore library, IAutomationProcessService processes)
    : IAutomationSavedProfileHandler
{
    public const string Module = "playwright-task";
    public const string Collection = "profiles";
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    public string ModuleId => Module;

    public async Task<IReadOnlyList<AutomationSavedProfileSummary>> ListProfilesAsync(CancellationToken cancellationToken)
    {
        var rows = await library.ListAsync(Module, Collection, cancellationToken).ConfigureAwait(false);
        return rows.Select(row => TryRead(row.Data)).Where(profile => profile is not null)
            .Select(profile => new AutomationSavedProfileSummary(profile!.Id, profile.Name)).ToArray();
    }

    public async Task<AutomationProfileExecutionOutput> ExecuteAsync(string profileId, JsonElement? input,
        AutomationExecutionMetadata metadata, CancellationToken cancellationToken)
    {
        var record = await library.GetAsync(Module, Collection, profileId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("The selected Playwright task no longer exists.");
        var profile = TryRead(record.Data) ?? throw new InvalidDataException("The saved Playwright task is invalid.");
        if (profile.Id != profileId || !Path.IsPathFullyQualified(profile.ProjectRoot)
            || !SamePath(profile.ProjectRoot, PlaywrightTestExplorer.ConfiguredManagedProjectRoot))
            throw new InvalidDataException("The generated task's managed Playwright project changed. Review and approve it again.");
        ValidateInputs(profile, input);
        var explorer = new PlaywrightTestExplorer(library, processes);
        var root = PlaywrightTestExplorer.ConfiguredManagedProjectRoot!;
        var source = await explorer.ReadCodexTaskSourceAsync(profile.RelativePath, cancellationToken).ConfigureAwait(false);
        var hash = Hash(source);
        if (!string.Equals(hash, profile.CodeHash, StringComparison.Ordinal)
            || !string.Equals(hash, profile.Approval.SourceHash, StringComparison.Ordinal)
            || !string.Equals(ScriptRunnerExecution.HashReview(profile.Scope, profile.Inputs, profile.Effects), profile.Approval.ScopeHash, StringComparison.Ordinal))
            throw new InvalidDataException("This generated task changed after approval. Review and approve its current source and scope before running it.");
        var started = System.Diagnostics.Stopwatch.StartNew();
        var result = await explorer.RunCodexTaskAsync(profile.RelativePath, source, input, cancellationToken).ConfigureAwait(false);
        return new AutomationProfileExecutionOutput(result.Output,
            new AutomationExecutionSummary(result.Status, result.Status == AutomationStatus.Success ? "completed" : "playwright-failed", (long)started.Elapsed.TotalMilliseconds));
    }

    public static void ValidateInputs(PlaywrightTaskProfile profile, JsonElement? input)
    {
        ValidateDeclaredInputs(profile.Inputs, input);
    }

    public static void ValidateDeclaredInputs(IReadOnlyList<CodexTaskInputDeclaration> declarations, JsonElement? input)
    {
        if (declarations is null || declarations.Count > 32 || declarations.Any(declaration => declaration is null
            || string.IsNullOrEmpty(declaration.Name) || !System.Text.RegularExpressions.Regex.IsMatch(declaration.Name, "^[A-Za-z][A-Za-z0-9_]{0,63}$")
            || declaration.Description is null || declaration.Description.Length > 512 || string.IsNullOrEmpty(declaration.Type)
            || declaration.Type is not ("string" or "number" or "boolean" or "path" or "json")))
            throw new InvalidDataException("The generated task input declarations are invalid.");
        if (input is null) input = JsonSerializer.SerializeToElement(new { });
        if (input.Value.ValueKind != JsonValueKind.Object || Encoding.UTF8.GetByteCount(input.Value.GetRawText()) > 48 * 1024)
            throw new InvalidDataException("Playwright task input must be a JSON object under 48 KiB.");
        foreach (var declaration in declarations)
        {
            if (!input.Value.TryGetProperty(declaration.Name, out var value))
            {
                if (declaration.Required) throw new InvalidDataException($"Required input '{declaration.Name}' is missing.");
                continue;
            }
            var matches = declaration.Type switch { "string" or "path" => value.ValueKind == JsonValueKind.String, "number" => value.ValueKind == JsonValueKind.Number,
                "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False, "object" => value.ValueKind == JsonValueKind.Object,
                "array" => value.ValueKind == JsonValueKind.Array, "json" => value.ValueKind != JsonValueKind.Undefined, _ => false };
            if (!matches) throw new InvalidDataException($"Input '{declaration.Name}' does not match its declared type.");
        }
        var known = declarations.Select(declaration => declaration.Name).ToHashSet(StringComparer.Ordinal);
        if (input.Value.EnumerateObject().Any(property => !known.Contains(property.Name)))
            throw new InvalidDataException("Playwright task input contains an undeclared name.");
    }

    public static string Hash(string source) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    public static PlaywrightTaskProfile? TryRead(JsonElement data)
    {
        try
        {
            var profile = JsonSerializer.Deserialize<PlaywrightTaskProfile>(data.GetRawText(), Options);
            if (profile is null || string.IsNullOrEmpty(profile.Id) || !System.Text.RegularExpressions.Regex.IsMatch(profile.Id, "^[a-z0-9][a-z0-9._-]{0,63}$")
                || string.IsNullOrWhiteSpace(profile.Name) || profile.Name.Length > 128 || string.IsNullOrEmpty(profile.ProjectRoot) || !Path.IsPathFullyQualified(profile.ProjectRoot)
                || string.IsNullOrEmpty(profile.RelativePath) || profile.RelativePath.Length > 256 || profile.RelativePath.Contains("..", StringComparison.Ordinal)
                || !profile.RelativePath.StartsWith("tests/codex/", StringComparison.Ordinal) || !profile.RelativePath.EndsWith(".spec.ts", StringComparison.Ordinal)
                || profile.Inputs is null || profile.Inputs.Count > 32 || profile.Scope is null || profile.Scope.Count > 32
                || profile.Effects is null || profile.Effects.Count > 32 || profile.Plan is null || profile.Plan.Length > 12_000
                || profile.CodeHash is null || !System.Text.RegularExpressions.Regex.IsMatch(profile.CodeHash, "^[A-F0-9]{64}$")
                || profile.Approval is null || string.IsNullOrEmpty(profile.Approval.SourceHash)
                || string.IsNullOrEmpty(profile.Approval.ScopeHash) || profile.Approval.Scope is null
                || !System.Text.RegularExpressions.Regex.IsMatch(profile.Approval.SourceHash, "^[A-F0-9]{64}$")
                || !System.Text.RegularExpressions.Regex.IsMatch(profile.Approval.ScopeHash, "^[A-F0-9]{64}$")
                || profile.Approval.Scope.Count > 32 || profile.Approval.Scope.Any(item => string.IsNullOrWhiteSpace(item) || item.Length > 256)
                || profile.Approval.DraftId is null || !System.Text.RegularExpressions.Regex.IsMatch(profile.Approval.DraftId, "^[A-Fa-f0-9]{32}$")
                || profile.Inputs.Any(item => item is null || string.IsNullOrEmpty(item.Name)
                    || !System.Text.RegularExpressions.Regex.IsMatch(item.Name, "^[A-Za-z][A-Za-z0-9_]{0,63}$")
                    || item.Description is null || item.Description.Length > 512 || string.IsNullOrEmpty(item.Type)
                    || item.Type is not ("string" or "number" or "boolean" or "path" or "json"))) return null;
            if (profile.Scope.Any(item => string.IsNullOrWhiteSpace(item) || item.Length > 256)
                || profile.Effects.Any(effect => effect is null || string.IsNullOrWhiteSpace(effect.Kind)
                    || string.IsNullOrWhiteSpace(effect.Description) || string.IsNullOrWhiteSpace(effect.Target))) return null;
            return profile;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidDataException) { return null; }
    }
    private static bool SamePath(string left, string? right) => right is not null && string.Equals(Path.GetFullPath(left).TrimEnd('\\'), Path.GetFullPath(right).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
}
