using System.Text.Json;
using System.Reflection;
using Automator.Application.Automation;
using Automator.Application.Launcher;
using Automator.Core.Automation;

public static class CodexTaskSpecs
{
    public static async Task RunAsync()
    {
        await CapabilityIsOnlyBoundForCodex();
        await ModulePayloadsUseWebJsonAndValidatePreferredFormat();
        await UnsavedDraftCannotBeExported();
        await ModuleRejectsMalformedPayloads();
        CliResultParsingRejectsInvalidAndTruncatedJson();
        CliResultParsingValidatesDeclarations();
        await InterruptedDraftWriteLeavesNoCorruptRecord();
        DraftIdsAreValidated();
        await CliStatusChecksAuthenticationThroughProcessService();
        await CliStatusClassifiesConfigurationErrors();
        await CliStatusHandlesTruncationQuickly();
        await CliGenerationDisablesTaskToolsAndUsesPromptStdin();
        await CliGenerationBoundsAndPersistsStructuredResults();
        await PythonTaskRequiresEffectApprovalAndSavesOnlyTheRunRevision();
        await PlaywrightReapprovalReviewsCurrentSavedSource();
        await WorkflowHistoricalPlanTracksSemanticChanges();
        await PlaywrightTaskRejectsTamperedManagedSource();
    }

    private static async Task UnsavedDraftCannotBeExported()
    {
        var dataDirectory = Path.Combine(Path.GetTempPath(), "automator-codex-export-specs", Guid.NewGuid().ToString("N"));
        try
        {
            var process = new FakeProcessService();
            var service = new CodexTaskService(dataDirectory, process,
                new CodexCliClient(Path.Combine(dataDirectory, "requests"), process, "C:\\Codex\\codex.exe"));
            var draft = new CodexTaskDraft(Guid.NewGuid().ToString("N"), "plan", "python", "print(1)", ["files"], ["files"], [], [], "hash", DateTimeOffset.UtcNow);
            var write = typeof(CodexTaskService).GetMethod("WriteDraftCreateNewAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            await (Task)write.Invoke(service, [draft, CancellationToken.None])!;
            await Check.ThrowsAsync<InvalidDataException>(() => service.ExportDraftAsync(draft.Id, CancellationToken.None));
        }
        finally
        {
            if (Directory.Exists(dataDirectory)) Directory.Delete(dataDirectory, recursive: true);
        }
    }

    private static async Task CapabilityIsOnlyBoundForCodex()
    {
        var service = new FakeCodexTaskService();
        var capabilities = new AutomationCapabilityRegistry(codexTaskServiceFactory: id => id == "codex" ? service : null);
        var registry = LauncherTabRegistry.CreateAutomationRegistry(capabilities, dataDirectory: Path.GetTempPath());
        var codex = registry.Modules.Single(module => module.Id == "codex");
        Check.True(codex.Capabilities.Any(capability => capability.Id == AutomationCapabilityIds.CodexTaskBuilder));
        await using var context = capabilities.CreateContext(new AutomationModuleDescriptor("codex", codex.Capabilities));
        Check.True(context.CodexTasks is not null);
        Check.True(context.HasCapability(AutomationCapabilityIds.CodexTaskBuilder));
        Check.Throws<InvalidOperationException>(() => capabilities.CreateContext(
            new AutomationModuleDescriptor("script-runner", [new(AutomationCapabilityIds.CodexTaskBuilder, 1)])));
    }

    private static async Task ModuleRejectsMalformedPayloads()
    {
        var service = new FakeCodexTaskService();
        var module = new CodexTaskModule(service);
        await using var context = new AutomationCapabilityRegistry(codexTaskServiceFactory: _ => service)
            .CreateContext(new AutomationModuleDescriptor("codex", [new(AutomationCapabilityIds.CodexTaskBuilder, 1)]));
        using var invalid = JsonDocument.Parse("{\"scriptPath\":\"C:\\\\evil.py\"}");
        await Check.ThrowsAsync<InvalidOperationException>(() => module.ExecuteAsync("runDraft", invalid.RootElement,
            JsonDocument.Parse("{}").RootElement, context, CancellationToken.None).AsTask());
        Check.Equal(0, service.RunCalls);
    }

    private static async Task ModulePayloadsUseWebJsonAndValidatePreferredFormat()
    {
        var service = new FakeCodexTaskService();
        var module = new CodexTaskModule(service);
        await using var context = new AutomationCapabilityRegistry(codexTaskServiceFactory: _ => service)
            .CreateContext(new AutomationModuleDescriptor("codex", [new(AutomationCapabilityIds.CodexTaskBuilder, 1)]));
        using var empty = JsonDocument.Parse("{}");
        var status = await module.ExecuteAsync("getStatus", empty.RootElement, empty.RootElement, context, CancellationToken.None);
        Check.Equal("unavailable", status.Data.GetProperty("state").GetString());
        Check.Equal(false, status.Data.GetProperty("available").GetBoolean());
        Check.Equal(false, status.Data.TryGetProperty("Available", out _));

        using var valid = JsonDocument.Parse("{\"prompt\":\"prepare report\",\"scope\":[\"reports\"],\"preferredFormat\":\"playwright\"}");
        var generated = await module.ExecuteAsync("generateDraft", valid.RootElement, empty.RootElement, context, CancellationToken.None);
        Check.Equal("playwright", service.LastGenerateRequest!.PreferredFormat);
        Check.Equal("playwright", generated.Data.GetProperty("format").GetString());

        using var invalid = JsonDocument.Parse("{\"prompt\":\"prepare report\",\"scope\":[\"reports\"],\"preferredFormat\":\"unsupported\"}");
        await Check.ThrowsAsync<InvalidOperationException>(() => module.ExecuteAsync("generateDraft", invalid.RootElement,
            empty.RootElement, context, CancellationToken.None).AsTask());
        Check.Equal("playwright", service.LastGenerateRequest!.PreferredFormat);
    }

    private static void CliResultParsingRejectsInvalidAndTruncatedJson()
    {
        Check.Throws<InvalidDataException>(() => CodexCliClient.ParseStructuredResult("{\"plan\":", 128));
        Check.Throws<InvalidDataException>(() => CodexCliClient.ParseStructuredResult("{}", 1));
    }

    private static void CliResultParsingValidatesDeclarations()
    {
        var invalidInput = "{\"plan\":\"p\",\"format\":\"python\",\"source\":\"print(1)\",\"scope\":[\"files\"],\"inputs\":[{\"name\":\"bad-name\",\"description\":\"x\",\"type\":\"string\",\"required\":true}],\"effects\":[]}";
        var invalidEffect = "{\"plan\":\"p\",\"format\":\"python\",\"source\":\"print(1)\",\"scope\":[\"files\"],\"inputs\":[],\"effects\":[{\"kind\":\"execute-anything\",\"description\":\"x\",\"target\":\"y\"}]}";
        var blankEffect = "{\"plan\":\"p\",\"format\":\"python\",\"source\":\"print(1)\",\"scope\":[\"files\"],\"inputs\":[],\"effects\":[{\"kind\":\"delete\",\"description\":\" \",\"target\":\"file.txt\"}]}";
        var duplicateScope = "{\"plan\":\"p\",\"format\":\"python\",\"source\":\"print(1)\",\"scope\":[\"Files\",\"files\"],\"inputs\":[],\"effects\":[]}";
        Check.Throws<InvalidDataException>(() => CodexCliClient.ParseStructuredResult(invalidInput, 4096));
        Check.Throws<InvalidDataException>(() => CodexCliClient.ParseStructuredResult(invalidEffect, 4096));
        Check.Throws<InvalidDataException>(() => CodexCliClient.ParseStructuredResult(blankEffect, 4096));
        Check.Throws<InvalidDataException>(() => CodexCliClient.ParseStructuredResult(duplicateScope, 4096));
    }

    private static async Task InterruptedDraftWriteLeavesNoCorruptRecord()
    {
        var dataDirectory = Path.Combine(Path.GetTempPath(), "automator-codex-specs", Guid.NewGuid().ToString("N"));
        try
        {
            var process = new FakeProcessService();
            var service = new CodexTaskService(dataDirectory, process, new CodexCliClient(Path.Combine(dataDirectory, "requests"), process, "C:\\Codex\\codex.exe"));
            var draft = new CodexTaskDraft(Guid.NewGuid().ToString("N"), "plan", "python", "print(1)", ["files"], ["files"], [], [], "hash", DateTimeOffset.UtcNow);
            var method = typeof(CodexTaskService).GetMethod("WriteDraftCreateNewAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            using var canceled = new CancellationTokenSource();
            canceled.Cancel();
            var write = (Task)method.Invoke(service, [draft, canceled.Token])!;
            await Check.ThrowsAsync<OperationCanceledException>(() => write);
            var drafts = Path.Combine(dataDirectory, "CodexTasks", "drafts");
            Check.True(Directory.Exists(drafts));
            Check.True(!Directory.EnumerateFiles(drafts).Any());
        }
        finally
        {
            if (Directory.Exists(dataDirectory)) Directory.Delete(dataDirectory, recursive: true);
        }
    }

    private static void DraftIdsAreValidated()
    {
        Check.Throws<InvalidDataException>(() => CodexTaskService.ValidateDraftId("..\\outside"));
        Check.Throws<InvalidDataException>(() => CodexTaskService.ValidateDraftId(""));
    }

    private static async Task CliStatusChecksAuthenticationThroughProcessService()
    {
        var process = new FakeProcessService
        {
            Handler = request => request.Arguments.SequenceEqual(["--version"])
                ? new(0, false, "codex 1.2.3", "", false, false, 1)
                : new(1, false, "Not logged in", "", false, false, 1)
        };
        var cli = new CodexCliClient(Path.GetTempPath(), process, "C:\\Codex\\codex.exe");
        var status = await cli.GetStatusAsync(CancellationToken.None);
        Check.Equal("signedOut", status.State);
        Check.Equal(2, process.Requests.Count);
        Check.True(process.Requests[1].Arguments.SequenceEqual(["login", "status"]));
        Check.True(Path.IsPathFullyQualified(process.Requests[0].ExecutablePath));
    }

    private static async Task CliGenerationDisablesTaskToolsAndUsesPromptStdin()
    {
        var process = new FakeProcessService
        {
            Handler = request =>
            {
                var outputPath = request.Arguments[request.Arguments.ToList().IndexOf("--output-last-message") + 1];
                File.WriteAllText(outputPath, ValidResult("files"));
                return new(0, false, new string('o', 80_000), new string('e', 80_000), true, true, 1);
            }
        };
        var cli = new CodexCliClient(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), process, "C:\\Codex\\codex.exe");
        var result = await cli.GenerateAsync("print a file", CancellationToken.None);
        Check.Equal("python", result.GetProperty("format").GetString());
        var request = process.Requests.Single();
        Check.True(request.Arguments.Contains("--ignore-user-config"));
        Check.True(request.Arguments.Contains("--ephemeral"));
        Check.True(request.Arguments.Contains("--sandbox") && request.Arguments.Contains("read-only"));
        var approvalIndex = request.Arguments.ToList().IndexOf("--ask-for-approval");
        var execIndex = request.Arguments.ToList().IndexOf("exec");
        Check.True(approvalIndex >= 0 && approvalIndex + 1 < execIndex && request.Arguments[approvalIndex + 1] == "never");
        Check.True(execIndex >= 0 && request.Arguments.Contains("--json"));
        var disabledFeatures = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < request.Arguments.Count - 1; index++)
            if (request.Arguments[index] == "--disable") disabledFeatures.Add(request.Arguments[index + 1]);
        foreach (var feature in new[] { "apps", "browser_use", "computer_use", "multi_agent", "plugins", "tool_search", "tool_call_mcp_elicitation", "tool_suggest", "shell_snapshot", "shell_tool", "unified_exec", "code_mode", "code_mode_only", "codex_hooks", "memories", "workspace_dependencies", "skill_mcp_dependency_install", "image_generation", "in_app_browser" })
        {
            Check.True(disabledFeatures.Contains(feature));
        }
        Check.True(request.Arguments.Contains("web_search=\"disabled\""));
        Check.True(request.StandardInput!.Contains("print a file", StringComparison.Ordinal));
        Check.True(!request.Arguments.Any(argument => argument.Contains("print a file", StringComparison.Ordinal)));
        Check.Equal(TimeSpan.FromMinutes(20), request.Timeout);
    }

    private static async Task CliStatusClassifiesConfigurationErrors()
    {
        var process = new FakeProcessService
        {
            Handler = request => request.Arguments.SequenceEqual(["--version"])
                ? new(0, false, "codex 1.2.3", "", false, false, 1)
                : new(1, false, "Configuration parse error: invalid TOML", "", false, false, 1)
        };
        var cli = new CodexCliClient(Path.GetTempPath(), process, "C:\\Codex\\codex.exe");
        var status = await cli.GetStatusAsync(CancellationToken.None);
        Check.Equal("configurationError", status.State);
    }

    private static async Task CliStatusHandlesTruncationQuickly()
    {
        var process = new FakeProcessService { Handler = _ => new(0, false, "codex", "", true, false, 1) };
        var cli = new CodexCliClient(Path.GetTempPath(), process, "C:\\Codex\\codex.exe");
        var status = await cli.GetStatusAsync(CancellationToken.None);
        Check.Equal("unavailable", status.State);
        Check.Equal(TimeSpan.FromSeconds(15), process.Requests.Single().Timeout);
    }

    private static async Task CliGenerationBoundsAndPersistsStructuredResults()
    {
        var dataDirectory = Path.Combine(Path.GetTempPath(), "automator-codex-specs", Guid.NewGuid().ToString("N"));
        try
        {
            var process = new FakeProcessService
            {
                Handler = request =>
                {
                    var outputPath = request.Arguments[request.Arguments.ToList().IndexOf("--output-last-message") + 1];
                    File.WriteAllText(outputPath, ValidResult("files", "example.com"));
                    return new(0, false, "", "", false, false, 1);
                }
            };
            var cli = new CodexCliClient(Path.Combine(dataDirectory, "requests"), process, "C:\\Codex\\codex.exe");
            var service = new CodexTaskService(dataDirectory, process, cli);
            var draft = await service.GenerateDraftAsync(new("download the report", ["files"], "python"), CancellationToken.None);
            Check.True(process.Requests.Single().StandardInput!.Contains("user explicitly requested the python format", StringComparison.Ordinal));
            Check.True(draft.Scope.SequenceEqual(["files"]));
            Check.True(draft.ProposedScope.SequenceEqual(["files", "example.com"]));
            Check.Equal("reportName", draft.Inputs.Single().Name);
            Check.Equal("download", draft.Effects.Single().Kind);
            var loaded = await service.GetDraftAsync(draft.Id, CancellationToken.None);
            Check.Equal(draft.SourceHash, loaded!.SourceHash);
            Check.Equal("example.com", loaded.ProposedScope[1]);

            process.Handler = request =>
            {
                var outputPath = request.Arguments[request.Arguments.ToList().IndexOf("--output-last-message") + 1];
                using var stream = new FileStream(outputPath, FileMode.Open, FileAccess.Write, FileShare.None);
                stream.SetLength(CodexCliClient.MaximumResultBytes + 1);
                return new(0, false, "", "", false, false, 1);
            };
            await Check.ThrowsAsync<InvalidDataException>(() => cli.GenerateAsync("generate", CancellationToken.None));
        }
        finally
        {
            if (Directory.Exists(dataDirectory)) Directory.Delete(dataDirectory, recursive: true);
        }
    }

    private static async Task PythonTaskRequiresEffectApprovalAndSavesOnlyTheRunRevision()
    {
        var root = Path.Combine(Path.GetTempPath(), "automator-codex-revision", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var data = Path.Combine(root, "data");
        var python = Path.Combine(root, "python.exe");
        await File.WriteAllTextAsync(python, "test interpreter");
        var library = new MemoryLibrary();
        var profile = new ScriptRunnerProfile("existing-python", "Configured Python", ScriptRunnerInterpreter.Python,
            python, Path.Combine(root, "existing.py"), [], root, ScriptRunnerOutputMode.Text, 60);
        await library.UpsertAsync(new(ScriptRunnerModule.IdValue, ScriptRunnerModule.ProfileCollection, profile.Id, 1,
            JsonSerializer.SerializeToElement(profile, new JsonSerializerOptions(JsonSerializerDefaults.Web)
                { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } }), DateTimeOffset.UtcNow), CancellationToken.None);
        var process = new FakeProcessService();
        var cli = new CodexCliClient(Path.Combine(data, "requests"), process, "C:\\Codex\\codex.exe");
        var service = new CodexTaskService(data, process, cli, library);
        process.Handler = request =>
        {
            if (request.Arguments.Contains("--output-last-message"))
            {
                var output = request.Arguments[request.Arguments.ToList().IndexOf("--output-last-message") + 1];
                File.WriteAllText(output, "{\"plan\":\"Delete a selected file\",\"format\":\"python\",\"source\":\"print(1)\",\"scope\":[\"files\"],\"inputs\":[],\"effects\":[{\"kind\":\"delete\",\"description\":\"Delete selected file\",\"target\":\"selected file\"}]}");
            }
            return new(0, false, "{}", "", false, false, 1);
        };
        try
        {
            var draft = await service.GenerateDraftAsync(new("delete a selected file", ["files"]), CancellationToken.None);
            draft = await service.ApproveChangesAsync(draft.Id, null, CancellationToken.None);
            await Check.ThrowsAsync<InvalidDataException>(() => service.RunDraftAsync(draft.Id, false, null, CancellationToken.None));
            Check.Equal(1, process.Requests.Count);
            var run = await service.RunDraftAsync(draft.Id, true, null, CancellationToken.None);
            Check.True(run.Succeeded);
            var successful = await service.GetDraftAsync(draft.Id, CancellationToken.None);
            Check.Equal(successful!.ApprovedRevision, successful.SuccessfulRunRevision);
            Check.True(process.Requests.Last().ExecutablePath == python);
            var saved = await service.SaveDraftAsync(draft.Id, CancellationToken.None);
            Check.True(saved.Saved);
            var savedRecord = await library.GetAsync(ScriptRunnerModule.IdValue, ScriptRunnerModule.ProfileCollection,
                "codex-" + draft.Id, CancellationToken.None);
            var savedProfile = JsonSerializer.Deserialize<ScriptRunnerProfile>(savedRecord!.Data.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web)
                { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } });
            Check.Equal(python, savedProfile!.InterpreterPath);
            var savedSourcePath = savedProfile.ScriptPath;
            var replace = typeof(CodexTaskService).GetMethod("WriteDraftReplacementAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            await (Task)replace.Invoke(service, [successful! with { Saved = false }, CancellationToken.None])!;
            var changedInputs = new[] { new CodexTaskInputDeclaration("newValue", "Current saved input", "string", true) };
            var changedScope = new[] { "files", "example.com" };
            var changedEffects = new[] { new CodexTaskEffectDeclaration("download", "Current saved effect", "selected report") };
            var changedApproval = savedProfile.CodexApproval! with { Scope = changedScope, Inputs = changedInputs, Effects = changedEffects };
            savedProfile = savedProfile with { CodexApproval = changedApproval };
            var profileJsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
                { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };
            await library.UpsertAsync(savedRecord with { Data = JsonSerializer.SerializeToElement(savedProfile, profileJsonOptions) }, CancellationToken.None);
            await File.WriteAllTextAsync(savedSourcePath, "print('changed saved source')");
            var inspected = await service.GetDraftAsync(draft.Id, CancellationToken.None);
            Check.True(inspected!.Saved); // profile persisted before the interrupted draft.Saved update
            Check.Equal("print('changed saved source')", inspected!.Source);
            Check.True(inspected.PlanIsHistorical);
            Check.True(inspected.Scope.SequenceEqual(changedScope));
            Check.True(inspected.Inputs.SequenceEqual(changedInputs));
            Check.True(inspected.Effects.SequenceEqual(changedEffects));
            Check.True(inspected.ReviewSourceHash is not null);
            await Check.ThrowsAsync<InvalidDataException>(() => service.ApproveChangesAsync(draft.Id, null, CancellationToken.None));
            var hashBeforeRace = inspected.ReviewSourceHash;
            await File.WriteAllTextAsync(savedSourcePath, "print('changed after inspection')");
            await Check.ThrowsAsync<InvalidDataException>(() => service.ApproveChangesAsync(draft.Id, hashBeforeRace, CancellationToken.None));
            var refreshed = await service.GetDraftAsync(draft.Id, CancellationToken.None);

            await (Task)replace.Invoke(service, [refreshed! with { Format = "workflow" }, CancellationToken.None])!;
            await Check.ThrowsAsync<InvalidDataException>(() => service.ApproveChangesAsync(draft.Id, null, CancellationToken.None));
            var afterFormatMismatch = await library.GetAsync(ScriptRunnerModule.IdValue, ScriptRunnerModule.ProfileCollection,
                "codex-" + draft.Id, CancellationToken.None);
            var unchangedAfterFormatMismatch = JsonSerializer.Deserialize<ScriptRunnerProfile>(afterFormatMismatch!.Data.GetRawText(), profileJsonOptions);
            Check.Equal(changedApproval.SourceHash, unchangedAfterFormatMismatch!.CodexApproval!.SourceHash);
            await (Task)replace.Invoke(service, [refreshed!, CancellationToken.None])!;

            var scriptProfileReads = 0;
            library.BeforeGet = (moduleId, collection, profileId) =>
            {
                if (moduleId == ScriptRunnerModule.IdValue && collection == ScriptRunnerModule.ProfileCollection
                    && profileId == "codex-" + draft.Id && ++scriptProfileReads == 3)
                    File.WriteAllText(savedSourcePath, "print('changed during approval')");
            };
            await Check.ThrowsAsync<InvalidDataException>(() => service.ApproveChangesAsync(draft.Id, refreshed!.ReviewSourceHash, CancellationToken.None));
            library.BeforeGet = null;
            var afterToctouRecord = await library.GetAsync(ScriptRunnerModule.IdValue, ScriptRunnerModule.ProfileCollection,
                "codex-" + draft.Id, CancellationToken.None);
            var unchangedAfterToctou = JsonSerializer.Deserialize<ScriptRunnerProfile>(afterToctouRecord!.Data.GetRawText(), profileJsonOptions);
            Check.Equal(changedApproval.SourceHash, unchangedAfterToctou!.CodexApproval!.SourceHash);

            refreshed = await service.GetDraftAsync(draft.Id, CancellationToken.None);
            var reapproved = await service.ApproveChangesAsync(draft.Id, refreshed!.ReviewSourceHash, CancellationToken.None);
            Check.Equal("print('changed during approval')", reapproved.Source);
            Check.True(reapproved.PlanIsHistorical);
            Check.Equal(ScriptRunnerExecution.HashSource(reapproved.Source), reapproved.SourceHash);
            Check.True(!reapproved.RunSucceeded && reapproved.SuccessfulRunRevision is null);
            var staleSave = await service.SaveDraftAsync(draft.Id, CancellationToken.None);
            Check.True(!staleSave.Saved);
            Check.True(await library.DeleteAsync(ScriptRunnerModule.IdValue, ScriptRunnerModule.ProfileCollection, "codex-" + draft.Id, CancellationToken.None));
            var orphaned = await service.GetDraftAsync(draft.Id, CancellationToken.None);
            Check.True(!orphaned!.Saved && orphaned.ReviewSourceHash is null);
            var reapprovedAfterDeletion = await service.ApproveChangesAsync(draft.Id, null, CancellationToken.None);
            Check.True(!reapprovedAfterDeletion.Saved);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static async Task PlaywrightReapprovalReviewsCurrentSavedSource()
    {
        var root = Path.Combine(Path.GetTempPath(), "automator-codex-playwright-review", Guid.NewGuid().ToString("N"));
        var data = Path.Combine(root, "data");
        var project = Path.Combine(root, "managed");
        Directory.CreateDirectory(root);
        var priorProject = Environment.GetEnvironmentVariable("AUTOMATOR_PLAYWRIGHT_PROJECT_ROOT");
        var priorNode = Environment.GetEnvironmentVariable("AUTOMATOR_BROWSER_NODE_PATH");
        var priorModule = Environment.GetEnvironmentVariable("AUTOMATOR_PLAYWRIGHT_MODULE_ROOT");
        var node = Path.Combine(root, "node.exe");
        var module = Path.Combine(root, "playwright");
        Directory.CreateDirectory(module);
        await File.WriteAllTextAsync(node, "test node");
        await File.WriteAllTextAsync(Path.Combine(module, "cli.js"), "");
        await File.WriteAllTextAsync(Path.Combine(module, "package.json"), "{\"version\":\"1\"}");
        Environment.SetEnvironmentVariable("AUTOMATOR_PLAYWRIGHT_PROJECT_ROOT", project);
        Environment.SetEnvironmentVariable("AUTOMATOR_BROWSER_NODE_PATH", node);
        Environment.SetEnvironmentVariable("AUTOMATOR_PLAYWRIGHT_MODULE_ROOT", module);
        var process = new FakeProcessService();
        var library = new MemoryLibrary();
        var service = new CodexTaskService(data, process, new CodexCliClient(Path.Combine(data, "requests"), process, "C:\\Codex\\codex.exe"), library);
        try
        {
            process.Handler = request =>
            {
                if (request.Arguments.Contains("--output-last-message"))
                {
                    var output = request.Arguments[request.Arguments.ToList().IndexOf("--output-last-message") + 1];
                    File.WriteAllText(output, "{\"plan\":\"Run one browser check\",\"format\":\"playwright\",\"source\":\"initial source\",\"scope\":[\"example.com\"],\"inputs\":[],\"effects\":[]}");
                }
                return new(0, false, "{}", "", false, false, 1);
            };
            var draft = await service.GenerateDraftAsync(new("check a page", ["example.com"]), CancellationToken.None);
            var relativePath = $"tests/codex/{draft.Id}.spec.ts";
            var sourceFile = Path.Combine(project, "tests", "codex", draft.Id + ".spec.ts");
            Directory.CreateDirectory(Path.GetDirectoryName(sourceFile)!);
            await File.WriteAllTextAsync(sourceFile, "current saved source");
            var savedInputs = new[] { new CodexTaskInputDeclaration("account", "Current saved account", "string", true) };
            var savedEffects = new[] { new CodexTaskEffectDeclaration("submit", "Current submit behavior", "selected form") };
            var codeHash = PlaywrightTaskSavedProfileHandler.Hash("current saved source");
            var profile = new PlaywrightTaskProfile("codex-" + draft.Id, "Saved generated task", relativePath, project,
                savedInputs, ["example.com"], savedEffects, "Current saved plan", codeHash,
                new CodexTaskApproval(codeHash, ScriptRunnerExecution.HashReview(["example.com"], savedInputs, savedEffects), draft.Id,
                    ["example.com"], savedInputs, savedEffects));
            await library.UpsertAsync(new(PlaywrightTaskSavedProfileHandler.Module, PlaywrightTaskSavedProfileHandler.Collection,
                profile.Id, 1, JsonSerializer.SerializeToElement(profile), DateTimeOffset.UtcNow), CancellationToken.None);
            draft = draft with { Saved = true, ManagedPath = relativePath };
            var replace = typeof(CodexTaskService).GetMethod("WriteDraftReplacementAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            await (Task)replace.Invoke(service, [draft, CancellationToken.None])!;

            var inspected = await service.GetDraftAsync(draft.Id, CancellationToken.None);
            Check.Equal("current saved source", inspected!.Source);
            Check.Equal("Current saved plan", inspected.Plan);
            Check.True(inspected.Inputs.SequenceEqual(savedInputs));
            Check.True(inspected.Effects.SequenceEqual(savedEffects));
            var inspectedHash = inspected.ReviewSourceHash;
            await File.WriteAllTextAsync(sourceFile, "changed after inspection");
            await Check.ThrowsAsync<InvalidDataException>(() => service.ApproveChangesAsync(draft.Id, inspectedHash, CancellationToken.None));

            var refreshed = await service.GetDraftAsync(draft.Id, CancellationToken.None);
            var approved = await service.ApproveChangesAsync(draft.Id, refreshed!.ReviewSourceHash, CancellationToken.None);
            Check.Equal("changed after inspection", approved.Source);
            Check.True(!approved.RunSucceeded && approved.SuccessfulRunRevision is null);
            Check.Equal(PlaywrightTaskSavedProfileHandler.Hash(approved.Source), approved.SourceHash);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AUTOMATOR_PLAYWRIGHT_PROJECT_ROOT", priorProject);
            Environment.SetEnvironmentVariable("AUTOMATOR_BROWSER_NODE_PATH", priorNode);
            Environment.SetEnvironmentVariable("AUTOMATOR_PLAYWRIGHT_MODULE_ROOT", priorModule);
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task WorkflowHistoricalPlanTracksSemanticChanges()
    {
        var root = Path.Combine(Path.GetTempPath(), "automator-codex-workflow-review", Guid.NewGuid().ToString("N"));
        var data = Path.Combine(root, "data");
        var library = new MemoryLibrary();
        var process = new FakeProcessService();
        var service = new CodexTaskService(data, process, new CodexCliClient(Path.Combine(data, "requests"), process, "C:\\Codex\\codex.exe"), library);
        var draftId = Guid.NewGuid().ToString("N");
        var originalSource = "{\"id\":\"workflow-example\", \"name\":\"Workflow\", \"steps\":[], \"variables\":{\"beta\":2,\"alpha\":{\"n\":1}}, \"variableReferences\":{}}";
        using var variablesDocument = JsonDocument.Parse("{\"alpha\":{\"n\":1.0},\"beta\":2.0}");
        var variables = variablesDocument.RootElement.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);
        var scope = new[] { "files" };
        var inputs = Array.Empty<CodexTaskInputDeclaration>();
        var effects = Array.Empty<CodexTaskEffectDeclaration>();
        var workflow = new AutomationWorkflowProfile("workflow-example", "Workflow", [], variables)
        {
            CodexApproval = new CodexTaskApproval("0", ScriptRunnerExecution.HashReview(scope, inputs, effects), draftId, scope, inputs, effects),
        };
        await library.UpsertAsync(new(AutomationWorkflowEngine.ModuleId, AutomationWorkflowEngine.ProfileCollection, workflow.Id, 1,
            JsonSerializer.SerializeToElement(workflow, new JsonSerializerOptions(JsonSerializerDefaults.Web)), DateTimeOffset.UtcNow), CancellationToken.None);
        var draft = new CodexTaskDraft(draftId, "Generated plan", "workflow", originalSource, scope, scope, inputs, effects,
            ScriptRunnerExecution.HashSource(originalSource), DateTimeOffset.UtcNow, Saved: false);
        var write = typeof(CodexTaskService).GetMethod("WriteDraftCreateNewAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await (Task)write.Invoke(service, [draft, CancellationToken.None])!;

        var formattingOnly = await service.GetDraftAsync(draftId, CancellationToken.None);
        Check.True(formattingOnly!.Saved); // deterministic profile is recovered even though the draft write did not finish
        Check.True(!formattingOnly!.PlanIsHistorical);
        var originalReviewHash = formattingOnly.ReviewSourceHash;

        var changedVariablesDocument = JsonDocument.Parse("{\"alpha\":{\"n\":1},\"beta\":2,\"newValue\":true}");
        var changedVariables = changedVariablesDocument.RootElement.EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);
        workflow = workflow with { Variables = changedVariables };
        await library.UpsertAsync(new(AutomationWorkflowEngine.ModuleId, AutomationWorkflowEngine.ProfileCollection, workflow.Id, 1,
            JsonSerializer.SerializeToElement(workflow, new JsonSerializerOptions(JsonSerializerDefaults.Web)), DateTimeOffset.UtcNow), CancellationToken.None);
        var changed = await service.GetDraftAsync(draftId, CancellationToken.None);
        Check.True(changed!.PlanIsHistorical);
        Check.True(!string.Equals(originalReviewHash, changed.ReviewSourceHash, StringComparison.Ordinal));
        var approved = await service.ApproveChangesAsync(draftId, changed.ReviewSourceHash, CancellationToken.None);
        Check.True(approved.PlanIsHistorical);
    }

    private static async Task PlaywrightTaskRejectsTamperedManagedSource()
    {
        var inputProfile = new PlaywrightTaskProfile("codex-input", "Input task", "tests/codex/input.spec.ts",
            Path.GetTempPath(), [new CodexTaskInputDeclaration("target", "Target page", "string", true)],
            ["example.com"], [], "plan", new string('A', 64), new CodexTaskApproval(new string('A', 64), new string('B', 64),
                "input", ["example.com"], [new CodexTaskInputDeclaration("target", "Target page", "string", true)], []));
        using (var invalidInput = JsonDocument.Parse("{\"other\":\"x\"}"))
            Check.Throws<InvalidDataException>(() => PlaywrightTaskSavedProfileHandler.ValidateInputs(inputProfile, invalidInput.RootElement));
        var root = Path.Combine(Path.GetTempPath(), "automator-codex-playwright", Guid.NewGuid().ToString("N"));
        var data = Path.Combine(root, "data");
        var project = Path.Combine(root, "managed");
        Directory.CreateDirectory(root);
        var priorProject = Environment.GetEnvironmentVariable("AUTOMATOR_PLAYWRIGHT_PROJECT_ROOT");
        var priorNode = Environment.GetEnvironmentVariable("AUTOMATOR_BROWSER_NODE_PATH");
        var priorModule = Environment.GetEnvironmentVariable("AUTOMATOR_PLAYWRIGHT_MODULE_ROOT");
        var node = Path.Combine(root, "node.exe");
        var module = Path.Combine(root, "playwright");
        Directory.CreateDirectory(module);
        await File.WriteAllTextAsync(node, "test node");
        await File.WriteAllTextAsync(Path.Combine(module, "cli.js"), "");
        await File.WriteAllTextAsync(Path.Combine(module, "package.json"), "{\"version\":\"1\"}");
        Environment.SetEnvironmentVariable("AUTOMATOR_PLAYWRIGHT_PROJECT_ROOT", project);
        Environment.SetEnvironmentVariable("AUTOMATOR_BROWSER_NODE_PATH", node);
        Environment.SetEnvironmentVariable("AUTOMATOR_PLAYWRIGHT_MODULE_ROOT", module);
        var process = new FakeProcessService();
        var library = new MemoryLibrary();
        var service = new CodexTaskService(data, process, new CodexCliClient(Path.Combine(data, "requests"), process, "C:\\Codex\\codex.exe"), library);
        try
        {
            process.Handler = request =>
            {
                if (request.Arguments.Contains("--output-last-message"))
                {
                    var output = request.Arguments[request.Arguments.ToList().IndexOf("--output-last-message") + 1];
                    File.WriteAllText(output, "{\"plan\":\"Run one browser check\",\"format\":\"playwright\",\"source\":\"import { test } from '@playwright/test'; test('ok', async () => {});\",\"scope\":[\"example.com\"],\"inputs\":[],\"effects\":[]}");
                }
                return new(0, false, "{}", "", false, false, 1);
            };
            var draft = await service.GenerateDraftAsync(new("check a page", ["example.com"]), CancellationToken.None);
            await service.ApproveChangesAsync(draft.Id, null, CancellationToken.None);
            var managedPath = $"tests/codex/{draft.Id}.spec.ts";
            var tamperedFile = Path.Combine(project, "tests", "codex", draft.Id + ".spec.ts");
            Directory.CreateDirectory(Path.GetDirectoryName(tamperedFile)!);
            await File.WriteAllTextAsync(tamperedFile, "tampered source");
            draft = draft with { ManagedPath = managedPath };
            var replace = typeof(CodexTaskService).GetMethod("WriteDraftReplacementAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            await (Task)replace.Invoke(service, [draft, CancellationToken.None])!;
            await Check.ThrowsAsync<InvalidDataException>(() => service.RunDraftAsync(draft.Id, false, null, CancellationToken.None));
            Check.Equal(1, process.Requests.Count); // only the Codex generation process ran
        }
        finally
        {
            Environment.SetEnvironmentVariable("AUTOMATOR_PLAYWRIGHT_PROJECT_ROOT", priorProject);
            Environment.SetEnvironmentVariable("AUTOMATOR_BROWSER_NODE_PATH", priorNode);
            Environment.SetEnvironmentVariable("AUTOMATOR_PLAYWRIGHT_MODULE_ROOT", priorModule);
            Directory.Delete(root, recursive: true);
        }
    }

    private static string ValidResult(string scope, string? additionalScope = null)
    {
        var scopes = additionalScope is null ? $"[\"{scope}\"]" : $"[\"{scope}\",\"{additionalScope}\"]";
        return $"{{\"plan\":\"Read the report and download it.\",\"format\":\"python\",\"source\":\"print(1)\",\"scope\":{scopes},\"inputs\":[{{\"name\":\"reportName\",\"description\":\"Report to retrieve\",\"type\":\"string\",\"required\":true}}],\"effects\":[{{\"kind\":\"download\",\"description\":\"Download the report\",\"target\":\"selected site\"}}]}}";
    }

    private sealed class FakeCodexTaskService : IAutomationCodexTaskService
    {
        public int RunCalls { get; private set; }
        public CodexTaskGenerateRequest? LastGenerateRequest { get; private set; }
        public Task<CodexTaskStatus> GetStatusAsync(CancellationToken cancellationToken) => Task.FromResult(new CodexTaskStatus(false, "unavailable", "CLI unavailable"));
        public Task<CodexTaskDraft> GenerateDraftAsync(CodexTaskGenerateRequest request, CancellationToken cancellationToken)
        {
            LastGenerateRequest = request;
            return Task.FromResult(new CodexTaskDraft(new string('a', 32), "Prepare report", request.PreferredFormat ?? "python",
                "print(1)", request.Scope, request.Scope, [], [], new string('b', 64), DateTimeOffset.UtcNow));
        }
        public Task<CodexTaskDraft?> GetDraftAsync(string id, CancellationToken cancellationToken) => Task.FromResult<CodexTaskDraft?>(null);
        public Task<IReadOnlyList<CodexTaskDraftSummary>> ListDraftsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CodexTaskDraftSummary>>([]);
        public Task<CodexTaskRunResult> RunDraftAsync(string id, bool effectConfirmed, JsonElement? taskInput, CancellationToken cancellationToken) { RunCalls++; return Task.FromResult(new CodexTaskRunResult(false, "unavailable")); }
        public Task<CodexTaskSaveResult> SaveDraftAsync(string id, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<CodexTaskDraft> ApproveChangesAsync(string id, string? expectedReviewSourceHash, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<string> ExportDraftAsync(string id, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class FakeProcessService : IAutomationProcessService
    {
        public List<AutomationProcessRequest> Requests { get; } = [];
        public Func<AutomationProcessRequest, AutomationProcessResult>? Handler { get; set; }
        public Task<AutomationProcessResult> ExecuteAsync(AutomationProcessRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(Handler?.Invoke(request) ?? new AutomationProcessResult(0, false, "", "", false, false, 1));
        }
    }

    private sealed class MemoryLibrary : IAutomationLibraryStore
    {
        private readonly Dictionary<(string Module, string Collection, string Id), AutomationLibraryRecord> _records = [];
        public Action<string, string, string>? BeforeGet { get; set; }
        public Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(string moduleId, string collection, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AutomationLibraryRecord>>(_records.Values.Where(record => record.ModuleId == moduleId && record.Collection == collection).ToArray());
        public Task<AutomationLibraryRecord?> GetAsync(string moduleId, string collection, string id, CancellationToken cancellationToken)
        {
            BeforeGet?.Invoke(moduleId, collection, id);
            return Task.FromResult(_records.GetValueOrDefault((moduleId, collection, id)));
        }
        public Task UpsertAsync(AutomationLibraryRecord record, CancellationToken cancellationToken) { _records[(record.ModuleId, record.Collection, record.Id)] = record; return Task.CompletedTask; }
        public Task<bool> DeleteAsync(string moduleId, string collection, string id, CancellationToken cancellationToken) => Task.FromResult(_records.Remove((moduleId, collection, id)));
    }
}
