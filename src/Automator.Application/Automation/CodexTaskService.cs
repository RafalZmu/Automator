using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Automator.Core.Automation;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

/// <summary>Host-owned draft store. Files are private to Automator and are always created without replacement.</summary>
public sealed class CodexTaskService : IAutomationCodexTaskService
{
    private const int MaximumDraftBytes = 512 * 1024;
    private readonly string _draftDirectory;
    private readonly CodexCliClient _cli;
    private readonly IAutomationLibraryStore? _library;
    private readonly IAutomationProcessService? _processes;
    private readonly AutomationWorkflowEngine? _workflows;
    private readonly string _dataDirectory;

    public CodexTaskService(string dataDirectory, IAutomationProcessService processService, CodexCliClient? cli = null,
        IAutomationLibraryStore? library = null, AutomationWorkflowEngine? workflows = null, IAutomationVariableProvider? variables = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        _draftDirectory = Path.GetFullPath(Path.Combine(dataDirectory, "CodexTasks", "drafts"));
        _dataDirectory = Path.GetFullPath(dataDirectory);
        _library = library;
        _processes = processService;
        _workflows = workflows;
        _cli = cli ?? new CodexCliClient(Path.Combine(dataDirectory, "CodexTasks", "requests"), processService);
    }

    public Task<CodexTaskStatus> GetStatusAsync(CancellationToken cancellationToken) => _cli.GetStatusAsync(cancellationToken);

    public async Task<CodexTaskDraft> GenerateDraftAsync(CodexTaskGenerateRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Prompt) || request.Prompt.Length > 12_000 || request.Prompt.Any(char.IsControl))
            throw new InvalidDataException("Task description is invalid or exceeds the size limit.");
        if (request.Scope is null || request.Scope.Count is < 1 or > 32
            || request.Scope.Any(item => string.IsNullOrWhiteSpace(item) || item.Length > 256 || item.Any(char.IsControl)))
            throw new InvalidDataException("Task scope is invalid.");
        var generationPrompt = BuildPrompt(request);
        if (request.PreferredFormat is { } preferredFormat)
        {
            if (preferredFormat is not ("python" or "playwright" or "workflow")) throw new InvalidDataException("Requested task format is unsupported.");
            generationPrompt += $"\n\nThe user explicitly requested the {preferredFormat} format. Generate this task in that format unless it cannot represent the task; if it cannot, explain why in the plan.";
        }
        var json = await _cli.GenerateAsync(generationPrompt, cancellationToken).ConfigureAwait(false);
        var format = json.GetProperty("format").GetString()!;
        var source = json.GetProperty("source").GetString()!;
        var requestedScope = request.Scope.Distinct(StringComparer.Ordinal).ToArray();
        var proposedScope = json.GetProperty("scope").EnumerateArray().Select(item => item.GetString()!).ToArray();
        var inputs = json.GetProperty("inputs").EnumerateArray().Select(item => new CodexTaskInputDeclaration(
            item.GetProperty("name").GetString()!, item.GetProperty("description").GetString()!, item.GetProperty("type").GetString()!,
            item.GetProperty("required").GetBoolean())).ToArray();
        var effects = json.GetProperty("effects").EnumerateArray().Select(item => new CodexTaskEffectDeclaration(
            item.GetProperty("kind").GetString()!, item.GetProperty("description").GetString()!, item.GetProperty("target").GetString()!)).ToArray();
        var draft = new CodexTaskDraft(Guid.NewGuid().ToString("N"), json.GetProperty("plan").GetString()!, format,
            source, requestedScope, proposedScope, inputs, effects, Hash(source), DateTimeOffset.UtcNow);
        await WriteDraftCreateNewAsync(draft, cancellationToken).ConfigureAwait(false);
        return draft;
    }

    public async Task<CodexTaskDraft?> GetDraftAsync(string id, CancellationToken cancellationToken)
    {
        ValidateDraftId(id);
        var path = ResolveDraftPath(id);
        if (!File.Exists(path)) return null;
        EnsureNoReparsePath(path);
        var info = new FileInfo(path);
        if (info.Length > MaximumDraftBytes) throw new InvalidDataException("Draft exceeds the size limit.");
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var draft = await JsonSerializer.DeserializeAsync<CodexTaskDraft>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (draft is null || _library is null) return draft;
        var savedReview = await ReadSavedTaskReviewAsync(draft, cancellationToken).ConfigureAwait(false);
        if (savedReview is not null) return savedReview with { Saved = true };
        return draft.Saved ? draft with { Saved = false, ReviewSourceHash = null } : draft;
    }

    public async Task<IReadOnlyList<CodexTaskDraftSummary>> ListDraftsAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_draftDirectory)) return [];
        EnsureNoReparsePath(_draftDirectory);
        var result = new List<CodexTaskDraftSummary>();
        foreach (var path in Directory.EnumerateFiles(_draftDirectory, "*.json").Take(1000))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = Path.GetFileNameWithoutExtension(path);
            ValidateDraftId(id);
            var draft = await GetDraftAsync(id, cancellationToken).ConfigureAwait(false);
            if (draft is not null) result.Add(new(id, draft.Plan, draft.Format, draft.CreatedAt, draft.RunSucceeded, draft.Saved));
        }
        return result.OrderByDescending(item => item.CreatedAt).ToArray();
    }

    public async Task<CodexTaskRunResult> RunDraftAsync(string id, bool effectConfirmed, JsonElement? taskInput, CancellationToken cancellationToken)
    {
        var draft = await GetDraftAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("Draft was not found.");
        EnsureExecutionServices();
        var revision = DraftRevision(draft);
        if (!string.Equals(draft.SourceHash, CodexTaskDraftSourceHash(draft), StringComparison.Ordinal)
            || !string.Equals(draft.ApprovedRevision, revision, StringComparison.Ordinal))
        {
            await InvalidateSuccessfulRunAsync(draft, cancellationToken).ConfigureAwait(false);
            throw new InvalidDataException("Review and approve this task's current source and scope before running it.");
        }
        if (draft.ProposedScope.Except(draft.Scope, StringComparer.Ordinal).Any())
        {
            await InvalidateSuccessfulRunAsync(draft, cancellationToken).ConfigureAwait(false);
            throw new InvalidDataException("The proposed task scope expands the selected scope. Review and approve the expanded scope before running it.");
        }
        if (draft.Effects.Any(effect => effect.Kind is "overwrite" or "delete" or "submit" or "send") && !effectConfirmed)
            throw new InvalidDataException("Confirm the declared overwrite, delete, or submit effect before running this task.");
        PlaywrightTaskSavedProfileHandler.ValidateDeclaredInputs(draft.Inputs, taskInput);

        var result = draft.Format switch
        {
            "python" => await RunPythonDraftAsync(draft, taskInput, cancellationToken).ConfigureAwait(false),
            "playwright" => await RunPlaywrightDraftAsync(draft, taskInput, cancellationToken).ConfigureAwait(false),
            "workflow" => await RunWorkflowDraftAsync(draft, taskInput, cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidDataException("Draft format is unsupported.")
        };
        var latest = await GetDraftAsync(id, cancellationToken).ConfigureAwait(false) ?? draft;
        if (result.Succeeded && string.Equals(latest.SourceHash, CodexTaskDraftSourceHash(latest), StringComparison.Ordinal)
            && string.Equals(DraftRevision(latest), revision, StringComparison.Ordinal)
            && string.Equals(latest.ApprovedRevision, revision, StringComparison.Ordinal))
        {
            latest = latest with { RunSucceeded = true, SuccessfulRunRevision = revision };
            await WriteDraftReplacementAsync(latest, cancellationToken).ConfigureAwait(false);
        }
        return result;
    }

    public async Task<CodexTaskSaveResult> SaveDraftAsync(string id, CancellationToken cancellationToken)
    {
        var draft = await GetDraftAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("Draft was not found.");
        var currentRevision = DraftRevision(draft);
        if (!string.Equals(draft.SourceHash, CodexTaskDraftSourceHash(draft), StringComparison.Ordinal)
            || !string.Equals(draft.ApprovedRevision, currentRevision, StringComparison.Ordinal))
        {
            await InvalidateSuccessfulRunAsync(draft, cancellationToken).ConfigureAwait(false);
            return new(false, "Review and approve this task's current source and scope before saving it.");
        }
        if (!draft.RunSucceeded || !string.Equals(draft.SuccessfulRunRevision, currentRevision, StringComparison.Ordinal))
        {
            await InvalidateSuccessfulRunAsync(draft, cancellationToken).ConfigureAwait(false);
            return new(false, "Run this exact approved draft successfully before saving it.");
        }
        if (draft.Saved) return new(false, "This draft has already been saved.");
        EnsureExecutionServices();
        switch (draft.Format)
        {
            case "python": await SavePythonDraftAsync(draft, cancellationToken).ConfigureAwait(false); break;
            case "playwright": await SavePlaywrightDraftAsync(draft, cancellationToken).ConfigureAwait(false); break;
            case "workflow": await SaveWorkflowDraftAsync(draft, cancellationToken).ConfigureAwait(false); break;
            default: throw new InvalidDataException("Draft format is unsupported.");
        }
        await WriteDraftReplacementAsync(draft with { Saved = true }, cancellationToken).ConfigureAwait(false);
        return new(true, "Saved the successful task as an Automator profile.");
    }

    public async Task<CodexTaskDraft> ApproveChangesAsync(string id, string? expectedReviewSourceHash, CancellationToken cancellationToken)
    {
        var draft = await GetDraftAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("Draft was not found.");
        if (draft.Saved && (string.IsNullOrWhiteSpace(expectedReviewSourceHash)
            || !string.Equals(expectedReviewSourceHash, draft.ReviewSourceHash, StringComparison.Ordinal)))
            throw new InvalidDataException("The saved task changed after inspection. Review its current source and scope before approving it.");
        if (draft.ProposedScope.Except(draft.Scope, StringComparer.Ordinal).Any()) draft = draft with { Scope = draft.ProposedScope };
        var revision = DraftRevision(draft);
        draft = draft with { ApprovedRevision = revision, RunSucceeded = false, SuccessfulRunRevision = null };
        await ReapproveSavedTaskAsync(draft, expectedReviewSourceHash, cancellationToken).ConfigureAwait(false);
        await WriteDraftReplacementAsync(draft, cancellationToken).ConfigureAwait(false);
        return draft;
    }

    private async Task<CodexTaskDraft?> ReadSavedTaskReviewAsync(CodexTaskDraft draft, CancellationToken cancellationToken)
    {
        if (_library is null) return null;
        var id = "codex-" + draft.Id;
        var scriptRecord = await _library.GetAsync(ScriptRunnerModule.IdValue, ScriptRunnerModule.ProfileCollection, id, cancellationToken).ConfigureAwait(false);
        var playwrightRecord = await _library.GetAsync(PlaywrightTaskSavedProfileHandler.Module, PlaywrightTaskSavedProfileHandler.Collection, id, cancellationToken).ConfigureAwait(false);
        var workflowRecords = await _library.ListAsync(AutomationWorkflowEngine.ModuleId, AutomationWorkflowEngine.ProfileCollection, cancellationToken).ConfigureAwait(false);
        var workflowMatches = workflowRecords.Select(record => (Record: record, Profile: ReadWorkflow(record.Data)))
            .Where(item => item.Profile?.CodexApproval?.DraftId == draft.Id).ToArray();
        var existingProfileCount = (scriptRecord is null ? 0 : 1) + (playwrightRecord is null ? 0 : 1) + workflowMatches.Length;
        if (existingProfileCount > 1) throw new InvalidDataException("Multiple saved profiles belong to this generated task.");
        if (existingProfileCount == 0) return null;
        var actualFormat = scriptRecord is not null ? "python" : playwrightRecord is not null ? "playwright" : "workflow";
        if (!string.Equals(draft.Format, actualFormat, StringComparison.Ordinal))
            throw new InvalidDataException("The saved profile format does not match this draft. Review the saved profile before approval.");

        string source;
        string? plan = null;
        IReadOnlyList<string> scope;
        IReadOnlyList<CodexTaskInputDeclaration>? inputs;
        IReadOnlyList<CodexTaskEffectDeclaration>? effects;
        AutomationWorkflowProfile? workflowForPlanComparison = null;
        if (draft.Format == "python")
        {
            var profile = JsonSerializer.Deserialize<ScriptRunnerProfile>(scriptRecord!.Data.GetRawText(), ScriptRunnerModule.JsonOptions)
                ?? throw new InvalidDataException("Saved Script Runner task is invalid.");
            var expectedPath = Path.GetFullPath(Path.Combine(_dataDirectory, "CodexTasks", "scripts", draft.Id + ".py"));
            if (!string.Equals(profile.Id, id, StringComparison.Ordinal) || !SamePath(profile.ScriptPath, expectedPath))
                throw new InvalidDataException("The saved Script Runner task is outside its managed source path.");
            EnsureNoReparsePath(expectedPath);
            var sourceInfo = new FileInfo(expectedPath);
            if (!sourceInfo.Exists || sourceInfo.Length > 256 * 1024) throw new InvalidDataException("Saved Script Runner source is missing or exceeds the size limit.");
            source = await File.ReadAllTextAsync(expectedPath, cancellationToken).ConfigureAwait(false);
            var approval = profile.CodexApproval ?? throw new InvalidDataException("Saved Script Runner review declarations are missing.");
            scope = approval.Scope ?? throw new InvalidDataException("Saved Script Runner scope is invalid.");
            inputs = approval.Inputs ?? [];
            effects = approval.Effects ?? [];
        }
        else if (draft.Format == "playwright")
        {
            var profile = PlaywrightTaskSavedProfileHandler.TryRead(playwrightRecord!.Data)
                ?? throw new InvalidDataException("Saved Playwright task is invalid.");
            var managedRoot = PlaywrightTestExplorer.ConfiguredManagedProjectRoot;
            if (profile.Id != id || !string.Equals(profile.RelativePath, $"tests/codex/{draft.Id}.spec.ts", StringComparison.Ordinal)
                || managedRoot is null || !SamePath(profile.ProjectRoot, managedRoot))
                throw new InvalidDataException("The saved Playwright task no longer matches its managed project and draft path.");
            var explorer = new PlaywrightTestExplorer(_library, _processes!);
            source = await explorer.ReadCodexTaskSourceAsync(profile.RelativePath, cancellationToken).ConfigureAwait(false);
            scope = profile.Scope;
            inputs = profile.Inputs;
            effects = profile.Effects;
            plan = profile.Plan;
        }
        else if (draft.Format == "workflow")
        {
            using var originalDocument = JsonDocument.Parse(draft.Source);
            var originalWorkflow = ReadWorkflow(originalDocument.RootElement)
                ?? throw new InvalidDataException("Generated Workflow source is invalid.");
            var workflowRecord = workflowMatches.Single().Record;
            var workflow = workflowMatches.Single().Profile
                ?? throw new InvalidDataException("Saved Workflow task is invalid.");
            if (!string.Equals(workflow.Id, originalWorkflow.Id, StringComparison.Ordinal)
                || !string.Equals(workflowRecord.Id, originalWorkflow.Id, StringComparison.Ordinal))
                throw new InvalidDataException("The deterministic Workflow profile does not match the generated task source.");
            if (workflow.CodexApproval?.DraftId != draft.Id)
                throw new InvalidDataException("The deterministic Workflow profile does not belong to this generated task.");
            workflowForPlanComparison = workflow;
            source = JsonSerializer.Serialize(workflow with { CodexApproval = null }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var approval = workflow.CodexApproval!;
            scope = approval.Scope ?? throw new InvalidDataException("Saved Workflow scope is invalid.");
            inputs = approval.Inputs ?? [];
            effects = approval.Effects ?? [];
        }
        else return null;

        var planIsHistorical = draft.PlanIsHistorical ||
            (draft.Format == "python" && !string.Equals(draft.Source, source, StringComparison.Ordinal)) ||
            (draft.Format == "workflow" && workflowForPlanComparison is not null
                && !WorkflowSemanticsMatch(draft.Source, workflowForPlanComparison));
        var reviewHash = HashReviewSource(draft.Format, source, plan ?? draft.Plan, scope, inputs, effects, planIsHistorical);
        return draft with { Source = source, SourceHash = Hash(source), Plan = plan ?? draft.Plan, Scope = scope,
            ProposedScope = scope, Inputs = inputs, Effects = effects, ReviewSourceHash = reviewHash, PlanIsHistorical = planIsHistorical };
    }

    private async Task ReapproveSavedTaskAsync(CodexTaskDraft draft, string? expectedReviewSourceHash, CancellationToken cancellationToken)
    {
        if (_library is null) return;
        var fresh = await ReadSavedTaskReviewAsync(draft, cancellationToken).ConfigureAwait(false);
        if (fresh is not null && (string.IsNullOrWhiteSpace(expectedReviewSourceHash)
            || !string.Equals(expectedReviewSourceHash, fresh.ReviewSourceHash, StringComparison.Ordinal)))
            throw new InvalidDataException("The saved task changed after inspection. Review its current source and scope before approving it.");
        if (draft.Saved && fresh is null)
            throw new InvalidDataException("The saved task profile was removed during approval. Review the draft again before approving it.");
        var id = "codex-" + draft.Id;
        var scriptRecord = await _library.GetAsync(ScriptRunnerModule.IdValue, ScriptRunnerModule.ProfileCollection, id, cancellationToken).ConfigureAwait(false);
        if (scriptRecord is not null)
        {
            var profile = JsonSerializer.Deserialize<ScriptRunnerProfile>(scriptRecord.Data.GetRawText(), ScriptRunnerModule.JsonOptions)
                ?? throw new InvalidDataException("Saved Script Runner task is invalid.");
            var approval = profile.CodexApproval ?? throw new InvalidDataException("Saved Script Runner review declarations are missing.");
            var profileReviewHash = HashReviewSource("python", draft.Source, draft.Plan,
                approval.Scope ?? [], approval.Inputs, approval.Effects, draft.PlanIsHistorical);
            if (fresh is null || !string.Equals(profileReviewHash, expectedReviewSourceHash, StringComparison.Ordinal))
                throw new InvalidDataException("The saved Python task changed after inspection. Review its current source and scope before approving it.");
            var sourceRevision = ScriptRunnerExecution.HashProfileRevision(profile, draft.Source);
            var currentSourceRevision = await ScriptRunnerExecution.HashProfileRevisionAsync(profile, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(sourceRevision, currentSourceRevision, StringComparison.Ordinal))
                throw new InvalidDataException("The saved Python source changed during approval. Review its current source before approving it.");
            var updated = profile with { CodexApproval = new CodexTaskApproval(sourceRevision, ScriptRunnerExecution.HashReview(draft.Scope, draft.Inputs, draft.Effects), draft.Id, draft.Scope, draft.Inputs, draft.Effects) };
            await _library.UpsertAsync(scriptRecord with { Data = JsonSerializer.SerializeToElement(updated, ScriptRunnerModule.JsonOptions), UpdatedUtc = DateTimeOffset.UtcNow }, cancellationToken).ConfigureAwait(false);
        }
        var playwrightRecord = await _library.GetAsync(PlaywrightTaskSavedProfileHandler.Module, PlaywrightTaskSavedProfileHandler.Collection, id, cancellationToken).ConfigureAwait(false);
        if (playwrightRecord is not null)
        {
            var profile = PlaywrightTaskSavedProfileHandler.TryRead(playwrightRecord.Data)
                ?? throw new InvalidDataException("Saved Playwright task is invalid.");
            var explorer = new PlaywrightTestExplorer(_library, _processes!);
            var source = await explorer.ReadCodexTaskSourceAsync(profile.RelativePath, cancellationToken).ConfigureAwait(false);
            var profileReviewHash = HashReviewSource("playwright", source, profile.Plan, profile.Scope, profile.Inputs, profile.Effects, draft.PlanIsHistorical);
            if (fresh is null || !string.Equals(source, draft.Source, StringComparison.Ordinal)
                || !string.Equals(profileReviewHash, expectedReviewSourceHash, StringComparison.Ordinal))
                throw new InvalidDataException("The saved Playwright task changed after inspection. Review its current source and scope before approving it.");
            var codeHash = PlaywrightTaskSavedProfileHandler.Hash(source);
            var updated = profile with { CodeHash = codeHash, Scope = draft.Scope, Inputs = draft.Inputs, Effects = draft.Effects,
                Plan = draft.Plan, Approval = new CodexTaskApproval(codeHash, ScriptRunnerExecution.HashReview(draft.Scope, draft.Inputs, draft.Effects), draft.Id, draft.Scope, draft.Inputs, draft.Effects) };
            await _library.UpsertAsync(playwrightRecord with { Data = JsonSerializer.SerializeToElement(updated), UpdatedUtc = DateTimeOffset.UtcNow }, cancellationToken).ConfigureAwait(false);
        }
        var workflowRecords = await _library.ListAsync(AutomationWorkflowEngine.ModuleId, AutomationWorkflowEngine.ProfileCollection, cancellationToken).ConfigureAwait(false);
        foreach (var record in workflowRecords)
        {
            var profile = JsonSerializer.Deserialize<AutomationWorkflowProfile>(record.Data.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (profile?.CodexApproval?.DraftId != draft.Id) continue;
            var source = JsonSerializer.Serialize(profile with { CodexApproval = null }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var planIsHistorical = draft.PlanIsHistorical || !WorkflowSemanticsMatch(draft.Source, profile);
            var profileApproval = profile.CodexApproval;
            var profileReviewHash = HashReviewSource("workflow", source, draft.Plan, profileApproval.Scope ?? [],
                profileApproval.Inputs, profileApproval.Effects, planIsHistorical);
            if (fresh is null || !string.Equals(profileReviewHash, expectedReviewSourceHash, StringComparison.Ordinal))
                throw new InvalidDataException("The saved Workflow changed after inspection. Review its current source and scope before approving it.");
            var sourceRevision = HashWorkflow(profile);
            var updated = profile with { CodexApproval = new CodexTaskApproval(sourceRevision, ScriptRunnerExecution.HashReview(draft.Scope, draft.Inputs, draft.Effects), draft.Id, draft.Scope, draft.Inputs, draft.Effects) };
            await _library.UpsertAsync(record with { Data = JsonSerializer.SerializeToElement(updated), UpdatedUtc = DateTimeOffset.UtcNow }, cancellationToken).ConfigureAwait(false);
        }
    }

    private static AutomationWorkflowProfile? ReadWorkflow(JsonElement data)
    {
        try { return JsonSerializer.Deserialize<AutomationWorkflowProfile>(data.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web)); }
        catch (JsonException) { return null; }
    }

    private static string HashReviewSource(string format, string source, string plan, IReadOnlyList<string> scope,
        IReadOnlyList<CodexTaskInputDeclaration>? inputs, IReadOnlyList<CodexTaskEffectDeclaration>? effects, bool planIsHistorical) => Hash(
            JsonSerializer.Serialize(new { format, source, plan, scope, inputs = inputs ?? [], effects = effects ?? [], planIsHistorical }));

    private static bool WorkflowSemanticsMatch(string originalSource, AutomationWorkflowProfile current)
    {
        try
        {
            using var document = JsonDocument.Parse(originalSource);
            var original = ReadWorkflow(document.RootElement);
            return original is not null && string.Equals(HashWorkflowSemantics(original), HashWorkflowSemantics(current), StringComparison.Ordinal);
        }
        catch (JsonException) { return false; }
    }

    private static string HashWorkflowSemantics(AutomationWorkflowProfile profile)
    {
        var canonical = new
        {
            profile.Id,
            profile.Name,
            Steps = profile.Steps.Select(step => new
            {
                step.Id,
                step.ModuleId,
                step.ProfileId,
                Inputs = step.Inputs.Select(input => new
                {
                    input.TargetJsonPointer,
                    Literal = input.Literal is { } literal ? CanonicalizeJson(literal) : null,
                    input.SourceStepId,
                    input.SourceJsonPointer,
                    input.LiteralPresent,
                }).ToArray(),
            }).ToArray(),
            Variables = profile.Variables.OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => new { item.Key, Value = CanonicalizeJson(item.Value) }).ToArray(),
            VariableReferences = profile.VariableReferences.OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => new { item.Key, item.Value }).ToArray(),
        };
        return Hash(JsonSerializer.Serialize(canonical));
    }

    private static object? CanonicalizeJson(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => value.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToDictionary(property => property.Name, property => CanonicalizeJson(property.Value), StringComparer.Ordinal),
        JsonValueKind.Array => value.EnumerateArray().Select(CanonicalizeJson).ToArray(),
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => decimal.TryParse(value.GetRawText(), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var number)
                ? number.ToString("G29", System.Globalization.CultureInfo.InvariantCulture) : value.GetRawText(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => throw new InvalidDataException("Saved Workflow contains an unsupported JSON value."),
    };

    private static bool SamePath(string left, string right) => string.Equals(Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar),
        Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExportDraftAsync(string id, CancellationToken cancellationToken)
    {
        var draft = await GetDraftAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("Draft was not found.");
        if (!draft.Saved) throw new InvalidDataException("Save the task successfully before exporting its source.");
        return draft.Source;
    }

    private void EnsureExecutionServices()
    {
        if (_library is null || _processes is null) throw new InvalidOperationException("Task execution services are unavailable.");
    }

    private static string CodexTaskDraftSourceHash(CodexTaskDraft draft) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(draft.Source)));

    private async Task<CodexTaskRunResult> RunPythonDraftAsync(CodexTaskDraft draft, JsonElement? taskInput, CancellationToken cancellationToken)
    {
        var scriptPath = Path.Combine(_dataDirectory, "CodexTasks", "draft-scripts", draft.Id + ".py");
        await EnsureDraftSourceAsync(scriptPath, draft.Source, cancellationToken).ConfigureAwait(false);
        var interpreter = await FindPythonAsync(cancellationToken).ConfigureAwait(false) ?? throw new InvalidDataException("Python was not found. Configure Python in Script Runner before running this draft.");
        var profile = new ScriptRunnerProfile("codex-" + draft.Id, "Codex draft", ScriptRunnerInterpreter.Python,
            interpreter, scriptPath, [], Path.GetDirectoryName(scriptPath)!, ScriptRunnerOutputMode.Text, 600);
        var execution = await ScriptRunnerExecution.RunAsync(profile, _processes!, taskInput, AutomationExecutionOrigin.Manual,
            draft.Id, cancellationToken).ConfigureAwait(false);
        var succeeded = execution.Summary.Status == AutomationStatus.Success;
        return new(succeeded, succeeded ? "Python draft completed." : "Python draft did not complete successfully.", execution.Output, execution.Summary.Status);
    }

    private async Task<CodexTaskRunResult> RunPlaywrightDraftAsync(CodexTaskDraft draft, JsonElement? taskInput, CancellationToken cancellationToken)
    {
        var explorer = new PlaywrightTestExplorer(_library!, _processes!);
        var relativePath = draft.ManagedPath ?? await explorer.CreateCodexTaskAsync(draft.Id, draft.Source, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(relativePath, $"tests/codex/{draft.Id}.spec.ts", StringComparison.Ordinal))
            throw new InvalidDataException("Generated Playwright draft path does not match its draft ID.");
        var managedSource = await explorer.ReadCodexTaskSourceAsync(relativePath, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(managedSource, draft.Source, StringComparison.Ordinal)
            || !string.Equals(Hash(managedSource), draft.SourceHash, StringComparison.Ordinal))
        {
            await InvalidateSuccessfulRunAsync(draft, cancellationToken).ConfigureAwait(false);
            throw new InvalidDataException("The managed Playwright source differs from the inspected draft. Review and approve the current source before running it.");
        }
        var result = await explorer.RunCodexTaskAsync(relativePath, draft.Source, taskInput, cancellationToken).ConfigureAwait(false);
        draft = draft with { ManagedPath = relativePath };
        await WriteDraftReplacementAsync(draft, cancellationToken).ConfigureAwait(false);
        return new(result.Status == AutomationStatus.Success, result.Message, result.Output, result.Status);
    }

    private async Task<CodexTaskRunResult> RunWorkflowDraftAsync(CodexTaskDraft draft, JsonElement? taskInput, CancellationToken cancellationToken)
    {
        if (_workflows is null) throw new InvalidOperationException("Workflow execution is unavailable.");
        var profile = JsonSerializer.Deserialize<AutomationWorkflowProfile>(draft.Source, new JsonSerializerOptions(JsonSerializerDefaults.Web)
            { PropertyNameCaseInsensitive = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } })
            ?? throw new InvalidDataException("Generated workflow is invalid.");
        profile = profile with { CodexApproval = null };
        var validation = WorkflowModule.ValidateProfile(profile);
        if (validation is not null) throw new InvalidDataException(validation);
        var result = await _workflows.RunTransientAsync(profile, taskInput, cancellationToken).ConfigureAwait(false);
        return new(result.Summary.Status == AutomationStatus.Success, result.Summary.Status == AutomationStatus.Success
            ? "Workflow draft completed." : "Workflow draft did not complete successfully.", result.Output, result.Summary.Status);
    }

    private async Task SavePythonDraftAsync(CodexTaskDraft draft, CancellationToken cancellationToken)
    {
        var asset = Path.Combine(_dataDirectory, "CodexTasks", "scripts", draft.Id + ".py");
        EnsureNoReparsePath(Path.GetDirectoryName(asset)!);
        EnsureNoReparsePath(asset);
        await EnsureDraftSourceAsync(asset, draft.Source, cancellationToken).ConfigureAwait(false);
        EnsureNoReparsePath(asset);
        var interpreter = await FindPythonAsync(cancellationToken).ConfigureAwait(false) ?? throw new InvalidDataException("Python was not found. Configure Python in Script Runner before saving this task.");
        var id = "codex-" + draft.Id;
        var profile = new ScriptRunnerProfile(id, "Codex task " + draft.Id[..8], ScriptRunnerInterpreter.Python,
            interpreter, asset, [], Path.GetDirectoryName(asset)!, ScriptRunnerOutputMode.Text, 600);
        if (await _library!.GetAsync(ScriptRunnerModule.IdValue, ScriptRunnerModule.ProfileCollection, profile.Id, cancellationToken).ConfigureAwait(false) is not null)
            throw new InvalidDataException("A Script Runner profile already uses the generated task ID.");
        var revision = await ScriptRunnerExecution.HashProfileRevisionAsync(profile, cancellationToken).ConfigureAwait(false);
        var approval = new CodexTaskApproval(revision, ScriptRunnerExecution.HashReview(draft.Scope, draft.Inputs, draft.Effects), draft.Id, draft.Scope, draft.Inputs, draft.Effects);
        profile = profile with { CodexApproval = approval };
        await _library!.UpsertAsync(new AutomationLibraryRecord(ScriptRunnerModule.IdValue, ScriptRunnerModule.ProfileCollection,
            profile.Id, 1, JsonSerializer.SerializeToElement(profile, ScriptRunnerModule.JsonOptions), DateTimeOffset.UtcNow), cancellationToken).ConfigureAwait(false);
    }

    private async Task SavePlaywrightDraftAsync(CodexTaskDraft draft, CancellationToken cancellationToken)
    {
        var explorer = new PlaywrightTestExplorer(_library!, _processes!);
        var relative = draft.ManagedPath ?? await explorer.CreateCodexTaskAsync(draft.Id, draft.Source, cancellationToken).ConfigureAwait(false);
        var managedSource = await explorer.ReadCodexTaskSourceAsync(relative, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(managedSource, draft.Source, StringComparison.Ordinal)
            || !string.Equals(Hash(managedSource), draft.SourceHash, StringComparison.Ordinal))
        {
            await InvalidateSuccessfulRunAsync(draft, cancellationToken).ConfigureAwait(false);
            throw new InvalidDataException("The managed Playwright source differs from the inspected draft. It will not be saved.");
        }
        var root = PlaywrightTestExplorer.ConfiguredManagedProjectRoot ?? throw new InvalidDataException("Managed Playwright project is unavailable.");
        var codeHash = PlaywrightTaskSavedProfileHandler.Hash(draft.Source);
        var id = "codex-" + draft.Id;
        var profile = new PlaywrightTaskProfile(id, "Codex task " + draft.Id[..8], relative, root,
            draft.Inputs, draft.Scope, draft.Effects, draft.Plan, codeHash,
            new CodexTaskApproval(codeHash, ScriptRunnerExecution.HashReview(draft.Scope, draft.Inputs, draft.Effects), draft.Id, draft.Scope, draft.Inputs, draft.Effects));
        if (await _library!.GetAsync(PlaywrightTaskSavedProfileHandler.Module, PlaywrightTaskSavedProfileHandler.Collection, id, cancellationToken).ConfigureAwait(false) is not null)
            throw new InvalidDataException("A Playwright task already uses the generated task ID.");
        await _library!.UpsertAsync(new AutomationLibraryRecord(PlaywrightTaskSavedProfileHandler.Module,
            PlaywrightTaskSavedProfileHandler.Collection, id, 1, JsonSerializer.SerializeToElement(profile), DateTimeOffset.UtcNow), cancellationToken).ConfigureAwait(false);
    }

    private async Task SaveWorkflowDraftAsync(CodexTaskDraft draft, CancellationToken cancellationToken)
    {
        var profile = JsonSerializer.Deserialize<AutomationWorkflowProfile>(draft.Source, new JsonSerializerOptions(JsonSerializerDefaults.Web)
            { PropertyNameCaseInsensitive = true }) ?? throw new InvalidDataException("Generated workflow is invalid.");
        profile = profile with { CodexApproval = null };
        var validation = WorkflowModule.ValidateProfile(profile);
        if (validation is not null) throw new InvalidDataException(validation);
        if (await _library!.GetAsync(AutomationWorkflowEngine.ModuleId, AutomationWorkflowEngine.ProfileCollection, profile.Id, cancellationToken).ConfigureAwait(false) is not null)
            throw new InvalidDataException("A saved Workflow already uses this generated workflow ID.");
        var revision = HashWorkflow(profile);
        profile = profile with { CodexApproval = new CodexTaskApproval(revision,
            ScriptRunnerExecution.HashReview(draft.Scope, draft.Inputs, draft.Effects), draft.Id, draft.Scope, draft.Inputs, draft.Effects) };
        await _library!.UpsertAsync(new AutomationLibraryRecord(AutomationWorkflowEngine.ModuleId,
            AutomationWorkflowEngine.ProfileCollection, profile.Id, 1, JsonSerializer.SerializeToElement(profile), DateTimeOffset.UtcNow), cancellationToken).ConfigureAwait(false);
    }

    private static string HashWorkflow(AutomationWorkflowProfile profile) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(profile with { CodexApproval = null }))));

    private static string DraftRevision(CodexTaskDraft draft) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(new { draft.Source, draft.Format, draft.Plan, draft.Scope, draft.ProposedScope, draft.Inputs, draft.Effects }))));

    private async Task InvalidateSuccessfulRunAsync(CodexTaskDraft draft, CancellationToken cancellationToken)
    {
        if (!draft.RunSucceeded && draft.SuccessfulRunRevision is null) return;
        await WriteDraftReplacementAsync(draft with { RunSucceeded = false, SuccessfulRunRevision = null }, cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteDraftReplacementAsync(CodexTaskDraft draft, CancellationToken cancellationToken)
    {
        var target = ResolveDraftPath(draft.Id);
        EnsureNoReparsePath(target);
        var temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(draft);
            if (bytes.Length > MaximumDraftBytes) throw new InvalidDataException("Draft exceeds the size limit.");
            await File.WriteAllBytesAsync(temp, bytes, cancellationToken).ConfigureAwait(false);
            File.Move(temp, target, overwrite: true);
        }
        finally { try { if (File.Exists(temp)) File.Delete(temp); } catch (IOException) { } }
    }

    private async Task WriteSourceCreateNewAsync(string path, string source, CancellationToken cancellationToken)
    {
        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        EnsureNoReparsePath(Path.GetDirectoryName(full)!);
        await using var stream = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await stream.WriteAsync(Encoding.UTF8.GetBytes(source), cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureDraftSourceAsync(string path, string source, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            await WriteSourceCreateNewAsync(path, source, cancellationToken).ConfigureAwait(false);
            return;
        }
        EnsureNoReparsePath(path);
        var existing = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(existing, source, StringComparison.Ordinal))
            throw new InvalidDataException("The managed generated script has changed. It will not be overwritten.");
    }

    private async Task<string?> FindPythonAsync(CancellationToken cancellationToken)
    {
        if (_library is not null)
        {
            var profiles = await _library.ListAsync(ScriptRunnerModule.IdValue, ScriptRunnerModule.ProfileCollection, cancellationToken).ConfigureAwait(false);
            foreach (var record in profiles)
            {
                try
                {
                    var profile = JsonSerializer.Deserialize<ScriptRunnerProfile>(record.Data.GetRawText(), ScriptRunnerModule.JsonOptions);
                    if (profile?.Interpreter == ScriptRunnerInterpreter.Python && Path.IsPathFullyQualified(profile.InterpreterPath)
                        && File.Exists(profile.InterpreterPath)) return profile.InterpreterPath;
                }
                catch (JsonException) { }
            }
        }
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try { var candidate = Path.GetFullPath(Path.Combine(directory.Trim('"'), "python.exe")); if (File.Exists(candidate)) return candidate; }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException) { }
        }
        return null;
    }

    public static void ValidateDraftId(string id)
    {
        if (id is null || id.Length != 32 || !id.All(Uri.IsHexDigit))
            throw new InvalidDataException("Draft ID is invalid.");
    }

    private async Task WriteDraftCreateNewAsync(CodexTaskDraft draft, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_draftDirectory);
        EnsureNoReparsePath(_draftDirectory);
        var destinationPath = ResolveDraftPath(draft.Id);
        var temporaryPath = Path.Combine(_draftDirectory, draft.Id + "." + Guid.NewGuid().ToString("N") + ".tmp");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(draft);
        if (bytes.Length > MaximumDraftBytes) throw new InvalidDataException("Draft exceeds the size limit.");
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, destinationPath);
        }
        finally
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private string ResolveDraftPath(string id)
    {
        ValidateDraftId(id);
        var path = Path.GetFullPath(Path.Combine(_draftDirectory, id + ".json"));
        var prefix = _draftDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Draft path is invalid.");
        return path;
    }

    private void EnsureNoReparsePath(string path)
    {
        var root = Path.GetPathRoot(path)!;
        var current = root;
        foreach (var part in Path.GetFullPath(path)[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Draft storage cannot use a reparse point.");
        }
    }

    private static string BuildPrompt(CodexTaskGenerateRequest request) =>
        $"User task:\n{request.Prompt}\n\nUser-selected scope (approval boundary; may not be silently expanded):\n- {string.Join("\n- ", request.Scope)}\n\n" +
        "Return the selected scope you plan to use. If more scope is needed, declare it explicitly. Declare named inputs and all known effects. Return source only as a draft; do not execute the task. " +
        "For Python, read the single JSON object of named values from standard input. For a workflow, use its normal initial JSON input mappings. " +
        "For Playwright, read named workflow inputs from JSON.parse(process.env.AUTOMATOR_WORKFLOW_INPUT_JSON ?? '{}'); do not use command line interpolation. The same environment variable is provided on draft and saved-task runs.";
    private static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
