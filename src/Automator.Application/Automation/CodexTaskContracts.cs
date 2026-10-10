using System.Text.Json;
using System.Text.Json.Serialization;

namespace Automator.Application.Automation;

public sealed record CodexTaskStatus(bool Available, string State, string Message, string? Version = null);
public sealed record CodexTaskGenerateRequest(string Prompt, IReadOnlyList<string> Scope, string? PreferredFormat = null);
public sealed record CodexTaskInputDeclaration(string Name, string Description, string Type, bool Required);
public sealed record CodexTaskEffectDeclaration(string Kind, string Description, string Target);
public sealed record CodexTaskDraft(string Id, string Plan, string Format, string Source, IReadOnlyList<string> Scope,
    IReadOnlyList<string> ProposedScope, IReadOnlyList<CodexTaskInputDeclaration> Inputs,
    IReadOnlyList<CodexTaskEffectDeclaration> Effects,
    string SourceHash, DateTimeOffset CreatedAt, bool RunSucceeded = false, bool Saved = false,
    string? ManagedPath = null, string? ApprovedRevision = null, string? SuccessfulRunRevision = null,
    string? ReviewSourceHash = null, bool PlanIsHistorical = false);
public sealed record CodexTaskDraftSummary(string Id, string Plan, string Format, DateTimeOffset CreatedAt, bool RunSucceeded, bool Saved);
public sealed record CodexTaskRunResult(bool Succeeded, string Message, JsonElement? Output = null, Automator.Core.Plugins.AutomationStatus Status = Automator.Core.Plugins.AutomationStatus.Success);
public sealed record CodexTaskSaveResult(bool Saved, string Message);

public interface IAutomationCodexTaskService
{
    Task<CodexTaskStatus> GetStatusAsync(CancellationToken cancellationToken);
    Task<CodexTaskDraft> GenerateDraftAsync(CodexTaskGenerateRequest request, CancellationToken cancellationToken);
    Task<CodexTaskDraft?> GetDraftAsync(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CodexTaskDraftSummary>> ListDraftsAsync(CancellationToken cancellationToken);
    Task<CodexTaskRunResult> RunDraftAsync(string id, bool effectConfirmed, JsonElement? taskInput, CancellationToken cancellationToken);
    Task<CodexTaskSaveResult> SaveDraftAsync(string id, CancellationToken cancellationToken);
    Task<CodexTaskDraft> ApproveChangesAsync(string id, string? expectedReviewSourceHash, CancellationToken cancellationToken);
    Task<string> ExportDraftAsync(string id, CancellationToken cancellationToken);
}

public static class CodexTaskJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };

    public static JsonElement ToElement<T>(T value)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value, Options));
        return document.RootElement.Clone();
    }
}
