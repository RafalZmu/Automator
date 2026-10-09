using Automator.Core.Plugins;
using System.Text.Json;
using System.Text.Json.Serialization;
using Automator.Application.Automation;
using Automator.Core.Automation;

internal static class ScriptRunnerExplorerSpecs
{
    public static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "Automator-explorer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var file = Path.Combine(root, "dane \u017c\u00f3\u0142\u0107.FDB"); File.WriteAllText(file, "data");
            var store = new Store(); var process = new Process();
            var module = new ScriptRunnerModule();
            var registry = new AutomationCapabilityRegistry(libraryStoreFactory: _ => store, processServiceFactory: _ => process);
            await using var context = registry.CreateContext(new(module.Id, module.Definition.Capabilities));
            var settings = module.CreateDefaultSettings();
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            async Task<Automator.Core.Plugins.AutomationResult> Call(string action, object input) => await module.ExecuteAsync(action, JsonSerializer.SerializeToElement(input, options), settings, context, default);
            var profile = new ScriptRunnerProfile("regular", "Script", ScriptRunnerInterpreter.Python, Environment.ProcessPath!, Path.Combine(root, "script.py"), ["--file={{file.path}}"], root, ScriptRunnerOutputMode.Text, 30);
            Ensure((await Call("saveProfile", profile)).Status == AutomationStatus.Success, "save regular profile");
            var map = new { id = "backup", profileId = profile.Id, label = "Backup", extensions = new[] { ".FDB", ".fdb" } };
            Ensure((await Call("saveExplorerAction", map)).Status == AutomationStatus.Success, "save mapping");
            var list = await Call("listExplorerActions", new { });
            Ensure(list.Data.GetProperty("actions")[0].GetProperty("extensions").GetArrayLength() == 1, "normalize extensions");
            Ensure((await Call("runExplorerAction", new { id = "backup", filePath = file })).Status == AutomationStatus.Success, "run mapped file");
            Ensure(process.Last!.Arguments[1] == "--file=" + file, "structured file token substitution");
            var variableFile = Path.Combine(root, "literal-{{variables.db}}.fdb"); File.WriteAllText(variableFile, "data");
            var variableModule = new ScriptRunnerModule(new Variables());
            var savedVariableRun = await variableModule.ExecuteAsync("runExplorerAction", JsonSerializer.SerializeToElement(new { id = "backup", filePath = variableFile }), settings, context, default);
            Ensure(savedVariableRun.Status == AutomationStatus.Success && process.Last!.Arguments[1] == "--file=" + variableFile, "saved arguments preserve literal selected path");
            var variableRun = await variableModule.ExecuteAsync("runExplorerAction", JsonSerializer.SerializeToElement(new { id = "backup", filePath = variableFile, arguments = new[] { "--file={{file.path}}", "{{variables.db}}" } }), settings, context, default);
            Ensure(variableRun.Status == AutomationStatus.Success, variableRun.Message);
            Ensure(process.Last!.Arguments[1] == "--file=" + variableFile, "selected filename variable syntax remains literal");
            Ensure(process.Last.Arguments[2] == "expanded-db", "profile variables still expand");
            var prefilledRun = await variableModule.ExecuteAsync("runExplorerAction", JsonSerializer.SerializeToElement(new { id = "backup", filePath = variableFile, arguments = new[] { "--file=" + variableFile, "{{variables.db}}" } }), settings, context, default);
            Ensure(prefilledRun.Status == AutomationStatus.Success && process.Last!.Arguments[1] == "--file=" + variableFile, "prefilled transient path remains literal");
            Ensure(process.Last!.Arguments[2] == "expanded-db", "transient argument variables still expand");
            Ensure((await Call("runExplorerAction", new { id = "backup", filePath = file, arguments = new[] { "edited", "{{file.path}}" } })).Status == AutomationStatus.Success, "transient arguments");
            Ensure(process.Last!.Arguments[1] == "edited" && process.Last.Arguments[2] == file, "transient values applied");
            var saved = await Call("listProfiles", new { });
            Ensure(saved.Data.GetProperty("profiles")[0].GetProperty("arguments")[0].GetString() == "--file={{file.path}}", "run edits not saved");
            foreach (var invalid in new[] { "relative.fdb", Path.Combine(root, "missing.fdb"), root })
                Ensure((await Call("runExplorerAction", new { id = "backup", filePath = invalid })).Status == AutomationStatus.Error, "invalid file rejected");
            var other = Path.Combine(root, "wrong.txt"); File.WriteAllText(other, "");
            Ensure((await Call("runExplorerAction", new { id = "backup", filePath = other })).Status == AutomationStatus.Error, "extension rejected");
            Ensure((await Call("runExplorerAction", new { id = "missing", filePath = file })).Status == AutomationStatus.Error, "unknown action rejected");
            Ensure((await Call("saveExplorerAction", new { id = "invalid", profileId = profile.Id, label = "Bad", extensions = new[] { ".*" } })).Status == AutomationStatus.Error, "invalid extension rejected");
            await Call("saveProfile", profile with { Arguments = ["plain"] });
            Ensure((await Call("saveExplorerAction", map)).Status == AutomationStatus.Error, "missing token rejected");
            Ensure((await Call("runExplorerAction", new { id = "backup", filePath = file })).Status == AutomationStatus.Error, "stale profile mapping rejected");
            await Call("deleteProfile", new { id = profile.Id });
            Ensure((await Call("listExplorerActions", new { })).Data.GetProperty("actions").GetArrayLength() == 0, "profile deletion removes mappings");
            Ensure(ScriptRunnerTemplateCatalog.Get("firebird-3-backup")?.Parameters.Count == 5, "plain backup registered");
            var installer = new ScriptRunnerTemplateInstaller(Path.Combine(root, "templates"), Environment.ProcessPath);
            var templateModule = new ScriptRunnerModule(templateInstaller: installer);
            async Task<AutomationResult> TemplateCall(string action, object input) => await templateModule.ExecuteAsync(action, JsonSerializer.SerializeToElement(input, options), settings, context, default);
            var installed = await TemplateCall("installTemplate", new { id = "firebird-3-backup" });
            Ensure(installed.Status == AutomationStatus.Success, installed.Message);
            var templateId = installed.Data.GetProperty("profile").GetProperty("id").GetString();
            var templateMap = new { id = "template-backup", profileId = templateId, label = "Backup", extensions = new[] { ".fdb" }, fileParameterKey = "database" };
            Ensure((await TemplateCall("saveExplorerAction", templateMap)).Status == AutomationStatus.Success, "template mapping");
            Ensure((await TemplateCall("saveExplorerAction", new { id = "bad-param", profileId = templateId, label = "Bad", extensions = new[] { ".fdb" }, fileParameterKey = "password" })).Status == AutomationStatus.Error, "sensitive text parameter rejected");
            Ensure((await TemplateCall("saveExplorerAction", new { id = "no-param", profileId = templateId, label = "Bad", extensions = new[] { ".fdb" } })).Status == AutomationStatus.Error, "missing file parameter rejected");
            Ensure((await TemplateCall("runExplorerAction", new { id = "template-backup", filePath = file })).Status == AutomationStatus.Error, "template requires input form");
            var templateRun = await TemplateCall("runExplorerAction", new { id = "template-backup", filePath = file, templateValues = new { database = "relative-overridden", backup = Path.Combine(root, "out.fbk"), gbak = Environment.ProcessPath, username = "SYSDBA", password = "transient-secret" } });
            Ensure(templateRun.Status == AutomationStatus.Success && process.Last!.Arguments[5] == file && process.Last.Arguments[9] == "transient-secret", "selected file overrides mapped template input");
            Ensure(!(await TemplateCall("listProfiles", new { })).Data.GetRawText().Contains("transient-secret"), "template credentials not persisted");
            Ensure((await TemplateCall("deleteExplorerAction", new { id = "template-backup" })).Status == AutomationStatus.Success, "mapping deletion");
            Ensure((await TemplateCall("listExplorerActions", new { })).Data.GetProperty("actions").GetArrayLength() == 0, "mapping removed");
            var registrationMenu = new Menu();
            var registeredModule = new ScriptRunnerModule(fileExplorerExecutable: @"C:\Automator\Automator.exe");
            var registrationRegistry = new AutomationCapabilityRegistry(libraryStoreFactory: _ => store, processServiceFactory: _ => process, fileExplorerMenuFactory: _ => registrationMenu);
            await using var registrationContext = registrationRegistry.CreateContext(new(module.Id,
                [new(AutomationCapabilityIds.LibraryStorage, 1), new(AutomationCapabilityIds.ProcessExecution, 1), new(AutomationCapabilityIds.FileExplorerMenu, 1)]));
            async Task<AutomationResult> RegistrationCall(string action, object input) => await registeredModule.ExecuteAsync(action, JsonSerializer.SerializeToElement(input, options), settings, registrationContext, default);
            await RegistrationCall("saveProfile", profile);
            var registrationSaved = await RegistrationCall("saveExplorerAction", map);
            Ensure(registrationMenu.Calls == 1 && registrationMenu.Last!.Count == 1, "mapping save reconciles registry");
            Ensure(registrationSaved.Data.GetProperty("registration").GetProperty("state").GetString() == "updated", "registration outcome returned");
            await RegistrationCall("deleteProfile", new { id = profile.Id });
            Ensure(registrationMenu.Last!.Count == 0, "profile deletion reconciles empty menu");
            await RegistrationCall("saveProfile", profile);
            await RegistrationCall("saveExplorerAction", map);
            await RegistrationCall("deleteExplorerAction", new { id = "backup" });
            Ensure(registrationMenu.Last!.Count == 0, "last mapping deletion clears registry");
            registrationMenu.Fail = true;
            var failedRegistration = await RegistrationCall("saveExplorerAction", map);
            Ensure(failedRegistration.Status == AutomationStatus.Success && failedRegistration.Data.GetProperty("registration").GetProperty("state").GetString() == "error", "registration failure preserves successful persistence");
            var failedListRegistration = await RegistrationCall("listExplorerActions", new { });
            Ensure(failedListRegistration.Data.GetProperty("actions").GetArrayLength() == 1, "persisted mappings survive registry failure");
            Ensure(failedListRegistration.Data.GetProperty("registration").GetProperty("message").GetString() == "The Explorer menu could not be updated. Reopen this section to retry.", "list registration error does not claim a save");
            var failedDeleteRegistration = await RegistrationCall("deleteExplorerAction", new { id = "backup" });
            Ensure(failedDeleteRegistration.Data.GetProperty("registration").GetProperty("message").GetString() == "The Explorer menu could not be updated. Reopen this section to retry.", "delete registration error does not claim a save");
            var beforeDisabled = registrationMenu.Calls;
            var disabledResult = await module.ExecuteAsync("listExplorerActions", JsonSerializer.SerializeToElement(new { }), settings, registrationContext, default);
            Ensure(registrationMenu.Calls == beforeDisabled && disabledResult.Data.GetProperty("registration").GetProperty("state").GetString() == "disabled", "disabled host never calls registry");
            var menu = new Menu();
            var menuRegistry = new AutomationCapabilityRegistry(fileExplorerMenuFactory: _ => menu);
            try { menuRegistry.CreateContext(new("other-module", [new(AutomationCapabilityIds.FileExplorerMenu, 1)])); throw new Exception("other module received Explorer menu"); }
            catch (InvalidOperationException) { }
            var menuContext = menuRegistry.CreateContext(new(module.Id, [new(AutomationCapabilityIds.FileExplorerMenu, 1)]));
            Ensure(menuContext.FileExplorerMenu is not null && menuContext.HasCapability(AutomationCapabilityIds.FileExplorerMenu), "declared menu capability granted");
            await menuContext.FileExplorerMenu!.ReconcileAsync([], Environment.ProcessPath!, default);
            Ensure(menu.Calls == 1, "menu calls forwarded");
            await menuContext.DisposeAsync();
            try { await menuContext.FileExplorerMenu.ReconcileAsync([], Environment.ProcessPath!, default); throw new Exception("disposed menu capability accepted call"); }
            catch (ObjectDisposedException) { }
            await using var ungranted = menuRegistry.CreateContext(new(module.Id, []));
            Ensure(ungranted.FileExplorerMenu is null, "ungranted menu unavailable");
        }
        finally { Directory.Delete(root, true); }
    }
    private static void Ensure(bool value, string message) { if (!value) throw new Exception(message); }
    private sealed class Variables : IAutomationVariableProvider
    {
        public Task<AutomationVariableSnapshot> GetAsync(CancellationToken token) => Task.FromResult(new AutomationVariableSnapshot(1, new Dictionary<string, JsonElement> { ["db"] = JsonSerializer.SerializeToElement("expanded-db") }, []));
    }
    private sealed class Menu : IAutomationFileExplorerMenu
    {
        public int Calls; public bool Fail; public IReadOnlyList<ExplorerActionDefinition>? Last;
        public Task ReconcileAsync(IReadOnlyList<ExplorerActionDefinition> entries, string executablePath, CancellationToken token) { Calls++; Last = entries; if (Fail) throw new UnauthorizedAccessException("injected registry denial"); return Task.CompletedTask; }
    }
    private sealed class Store : IAutomationLibraryStore
    {
        private readonly Dictionary<(string, string, string), AutomationLibraryRecord> _records = [];
        public Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(string module, string collection, CancellationToken token) => Task.FromResult<IReadOnlyList<AutomationLibraryRecord>>(_records.Values.Where(record => record.ModuleId == module && record.Collection == collection).ToArray());
        public Task<AutomationLibraryRecord?> GetAsync(string module, string collection, string id, CancellationToken token) => Task.FromResult(_records.GetValueOrDefault((module, collection, id)));
        public Task UpsertAsync(AutomationLibraryRecord record, CancellationToken token) { _records[(record.ModuleId, record.Collection, record.Id)] = record; return Task.CompletedTask; }
        public Task<bool> DeleteAsync(string module, string collection, string id, CancellationToken token) => Task.FromResult(_records.Remove((module, collection, id)));
    }
    private sealed class Process : IAutomationProcessService
    {
        public AutomationProcessRequest? Last;
        public Task<AutomationProcessResult> ExecuteAsync(AutomationProcessRequest request, CancellationToken token)
        { Last = request; return Task.FromResult(new AutomationProcessResult(0, false, "", "", false, false, 1)); }
    }
}
