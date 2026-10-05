using System.Text.Json;
using Automator.Application.Automation;
using Automator.Core.Automation;
using Automator.Core.Plugins;

internal static class ScriptRunnerTemplateRunSpecs
{
    public static async Task RunAsync()
    {
        var store = new RecordingLibraryStore();
        var process = new TemplateProcess();
        var module = new ScriptRunnerModule();
        var registry = new AutomationCapabilityRegistry(libraryStoreFactory: _ => store, processServiceFactory: _ => process);
        await using var context = registry.CreateContext(new(module.Id, module.Definition.Capabilities));
        var settings = module.CreateDefaultSettings();
        var catalog = await module.ExecuteAsync("listTemplates", JsonSerializer.SerializeToElement(new { }), settings, context, default);
        Ensure(catalog.Status == AutomationStatus.Success && catalog.Data.GetProperty("templates").GetArrayLength() == 1, "catalog action");
        Ensure(!catalog.Data.GetRawText().Contains("masterkey"), "catalog excludes credential defaults");
        var profile = new ScriptRunnerProfile("template-run", "Backup", ScriptRunnerInterpreter.Powershell,
            @"C:\Windows\powershell.exe", @"C:\Scripts\backup.ps1", [], @"C:\Scripts", ScriptRunnerOutputMode.Text, 60,
            new("firebird-3-backup-zip", 1));
        var saved = await module.ExecuteAsync("saveProfile", JsonSerializer.SerializeToElement(profile, Options()), settings, context, default);
        Ensure(saved.Status == AutomationStatus.Success, saved.Message);
        var before = store.LastWrite!.Data.GetRawText();
        var missing = await module.ExecuteAsync("runProfile", JsonSerializer.SerializeToElement(new { id = profile.Id }), settings, context, default);
        Ensure(missing.Status == AutomationStatus.Error && missing.Message.Contains("input", StringComparison.OrdinalIgnoreCase) && process.Calls == 0, "interactive inputs required before dispatch");
        var values = new { database = @"C:\Data Files\live.fdb", backup = @"D:\Backup Files\live.fbk", archive = @"D:\Backup Files\live.zip", gbak = @"C:\Program Files\Firebird\gbak.exe", username = "SYSDBA", password = "unique-private-password" };
        var request = JsonSerializer.SerializeToElement(new { id = profile.Id, templateValues = values });
        var run = await module.ExecuteAsync("runProfile", request, settings, context, default);
        Ensure(run.Status == AutomationStatus.Success && process.Last!.Arguments[5] == values.database && process.Last.Arguments[10] == values.password, "transient structured arguments");
        Ensure(store.LastWrite.Data.GetRawText() == before && !before.Contains(values.password), "run never writes values");
        Ensure(!run.Data.GetRawText().Contains(values.password) && run.Data.GetProperty("stdout").GetString()!.Contains("[redacted]"), "stdout stderr sensitive redaction");
        process.Failure = new IOException(values.password);
        var failed = await module.ExecuteAsync("runProfile", request, settings, context, default);
        Ensure(failed.Status == AutomationStatus.Error && !failed.Message.Contains(values.password), "safe process failure");
        process.Failure = null;
        process.Result = new(7, false, "", "failed", false, false, 5);
        Ensure((await module.ExecuteAsync("runProfile", request, settings, context, default)).Status == AutomationStatus.Error, "nonzero run failure");
        process.Result = new(null, true, "", "", false, false, 5);
        Ensure((await module.ExecuteAsync("runProfile", request, settings, context, default)).Status == AutomationStatus.Warning, "timeout preserved");
        process.Failure = new OperationCanceledException();
        try { await module.ExecuteAsync("runProfile", request, settings, context, default); throw new Exception("cancellation swallowed"); }
        catch (OperationCanceledException) { }
        process.Failure = null;
        var calls = process.Calls;
        var handler = new ScriptRunnerSavedProfileHandler(store, process);
        try { await handler.ExecuteAsync(profile.Id, null, new(AutomationExecutionOrigin.Workflow, "test"), default); throw new Exception("scheduled template launched"); }
        catch (InvalidDataException exception) { Ensure(exception.Message.Contains("input", StringComparison.OrdinalIgnoreCase), "saved-profile required input error"); }
        Ensure(process.Calls == calls, "workflow/scheduler cannot dispatch template without form");
        var incompatible = profile with { TemplateOrigin = new("firebird-3-backup-zip", 2) };
        await module.ExecuteAsync("saveProfile", JsonSerializer.SerializeToElement(incompatible, Options()), settings, context, default);
        Ensure((await module.ExecuteAsync("runProfile", request, settings, context, default)).Status == AutomationStatus.Error && process.Calls == calls, "template version mismatch rejected");
        await module.ExecuteAsync("saveProfile", JsonSerializer.SerializeToElement(profile with { TemplateOrigin = null }, Options()), settings, context, default);
        Ensure((await module.ExecuteAsync("runProfile", request, settings, context, default)).Status == AutomationStatus.Error && process.Calls == calls, "transient values require template origin");
    }
    private static JsonSerializerOptions Options() { var options = new JsonSerializerOptions(JsonSerializerDefaults.Web); options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase)); return options; }
    private static void Ensure(bool condition, string message) { if (!condition) throw new Exception(message); }
    private sealed class TemplateProcess : IAutomationProcessService
    {
        public int Calls;
        public AutomationProcessRequest? Last;
        public Exception? Failure;
        public AutomationProcessResult Result = new(0, false, "unique-private-password output", "unique-private-password diagnostic", false, false, 5);
        public Task<AutomationProcessResult> ExecuteAsync(AutomationProcessRequest request, CancellationToken cancellationToken)
        { Calls++; Last = request; if (Failure is not null) throw Failure; return Task.FromResult(Result); }
    }
}
