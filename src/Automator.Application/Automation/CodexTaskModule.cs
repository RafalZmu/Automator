using System.Text.Json;
using Automator.Application.Launcher;
using Automator.Core.Automation;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

public sealed class CodexTaskModule : ILauncherTabModuleProvider
{
    private static readonly AutomationCapabilityRequirement Capability = new(AutomationCapabilityIds.CodexTaskBuilder, 1);
    private static readonly string[] ActionIds = ["getStatus", "generateDraft", "getDraft", "listDrafts", "runDraft", "saveDraft", "approveChanges", "exportDraft"];
    private readonly IAutomationCodexTaskService? _testService;

    public CodexTaskModule(IAutomationCodexTaskService? testService = null) => _testService = testService;

    public AutomationModuleDefinition Definition { get; } = new(9, "codex", "Codex", "codex", "codex", false, 1, 1,
        [Capability],
        ActionIds.Select(id => new AutomationModuleActionDescriptor(id, 1, [Capability])).ToArray());
    public int ContractVersion => Definition.ContractVersion;
    public string Id => Definition.Id;
    public string Title => Definition.Title;
    public LauncherModuleState CreateInitialState() => new(Id, LauncherTabRegistry.Version, new Dictionary<string, string>());
    public JsonElement CreateDefaultSettings() => JsonDocument.Parse("{}").RootElement.Clone();
    public JsonElement MigrateSettings(int fromVersion, JsonElement value) => throw new InvalidOperationException("Codex settings migration is not supported.");

    public async ValueTask<AutomationResult> ExecuteAsync(string actionId, JsonElement input, JsonElement moduleSettings,
        AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var service = _testService ?? (services.ModuleId == Id ? services.CodexTasks : null)
            ?? throw new InvalidOperationException("The Codex task-builder capability is unavailable.");
        ValidateInput(actionId, input);
        object data = actionId switch
        {
            "getStatus" => await service.GetStatusAsync(cancellationToken).ConfigureAwait(false),
            "generateDraft" => await service.GenerateDraftAsync(new(input.GetProperty("prompt").GetString()!, ReadScope(input), ReadOptionalString(input, "preferredFormat")), cancellationToken).ConfigureAwait(false),
            "getDraft" => (object?)await service.GetDraftAsync(ReadId(input), cancellationToken).ConfigureAwait(false) ?? new { found = false },
            "listDrafts" => await service.ListDraftsAsync(cancellationToken).ConfigureAwait(false),
            "runDraft" => await service.RunDraftAsync(ReadId(input), ReadOptionalBoolean(input, "effectConfirmed"), input.TryGetProperty("taskInput", out var taskInput) ? taskInput.Clone() : null, cancellationToken).ConfigureAwait(false),
            "saveDraft" => await service.SaveDraftAsync(ReadId(input), cancellationToken).ConfigureAwait(false),
            "approveChanges" => await service.ApproveChangesAsync(ReadId(input), ReadOptionalString(input, "expectedReviewSourceHash"), cancellationToken).ConfigureAwait(false),
            "exportDraft" => new { source = await service.ExportDraftAsync(ReadId(input), cancellationToken).ConfigureAwait(false) },
            _ => throw new InvalidOperationException($"Action '{actionId}' is not supported.")
        };
        var result = CodexTaskJson.ToElement(data);
        if (System.Text.Encoding.UTF8.GetByteCount(result.GetRawText()) > 512 * 1024)
            throw new InvalidDataException("Codex action result exceeds the size limit.");
        return new(AutomationTabContract.CurrentVersion, AutomationStatus.Success, "Codex task builder action completed.", result, []);
    }

    private static void ValidateInput(string action, JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Codex action payload must be an object.");
        var allowed = action switch
        {
            "getStatus" or "listDrafts" => Array.Empty<string>(),
            "generateDraft" => ["prompt", "scope", "preferredFormat"],
            "getDraft" or "saveDraft" or "exportDraft" => ["id"],
            "approveChanges" => ["id", "expectedReviewSourceHash"],
            "runDraft" => ["id", "effectConfirmed", "taskInput"],
            _ => throw new InvalidOperationException($"Action '{action}' is not supported.")
        };
        if (input.EnumerateObject().Any(property => !allowed.Contains(property.Name, StringComparer.Ordinal)))
            throw new InvalidOperationException("Codex action payload contains unsupported properties.");
        if (action == "runDraft" && input.TryGetProperty("effectConfirmed", out var confirmation)
            && confirmation.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new InvalidOperationException("Effect confirmation must be a boolean.");
        if (action == "runDraft" && input.TryGetProperty("taskInput", out var taskInput)
            && (taskInput.ValueKind != JsonValueKind.Object || System.Text.Encoding.UTF8.GetByteCount(taskInput.GetRawText()) > 48 * 1024))
            throw new InvalidOperationException("Task inputs must be a JSON object under 48 KiB.");
        if (action == "generateDraft")
        {
            if (!input.TryGetProperty("prompt", out var prompt) || prompt.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(prompt.GetString()) || prompt.GetString()!.Length > 12_000
                || !input.TryGetProperty("scope", out var scope) || scope.ValueKind != JsonValueKind.Array
                || scope.GetArrayLength() is < 1 or > 32 || scope.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String || item.GetString()!.Length is < 1 or > 256)
                || (input.TryGetProperty("preferredFormat", out var preferred) && (preferred.ValueKind != JsonValueKind.String || preferred.GetString() is not ("python" or "playwright" or "workflow"))))
                throw new InvalidOperationException("Codex generation payload is invalid.");
        }
        else if (allowed.Contains("id"))
        {
            if (!input.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String
                || id.GetString() is not { Length: 32 } value || !value.All(Uri.IsHexDigit))
                throw new InvalidOperationException("Codex draft ID is invalid.");
        }
        if (action == "approveChanges" && input.TryGetProperty("expectedReviewSourceHash", out var expectedHash)
            && (expectedHash.ValueKind != JsonValueKind.String || expectedHash.GetString() is not { Length: 64 } hash || !hash.All(Uri.IsHexDigit)))
            throw new InvalidOperationException("Expected review source hash is invalid.");
    }

    private static string ReadId(JsonElement input) => input.GetProperty("id").GetString()!;
    private static bool ReadOptionalBoolean(JsonElement input, string name) => input.TryGetProperty(name, out var value) && value.GetBoolean();
    private static string? ReadOptionalString(JsonElement input, string name) => input.TryGetProperty(name, out var value) ? value.GetString() : null;
    private static string[] ReadScope(JsonElement input) => input.GetProperty("scope").EnumerateArray().Select(item => item.GetString()!).ToArray();
}
