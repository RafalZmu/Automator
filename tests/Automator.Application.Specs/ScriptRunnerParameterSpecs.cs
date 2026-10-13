using System.Text.Json;
using Automator.Application.Automation;
using Automator.Core.Automation;
using Automator.Core.Plugins;

internal static class ScriptRunnerParameterSpecs
{
    public static async Task RunAsync()
    {
        DefinitionRules();
        MappingRules();
        var root = Path.Combine(Path.GetTempPath(), "Automator-parameter-specs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var script = Path.Combine(root, "report.ps1");
            File.WriteAllText(script, "param()");
            await ProfileRunsAsync(root, script);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void DefinitionRules()
    {
        ScriptRunnerParameters.ValidateDefinitions(Parameters(), allowSensitive: false, maximumCount: ScriptRunnerParameters.MaximumProfileParameters);
        Throws(() => ScriptRunnerParameters.ValidateDefinitions([new("secret", "Secret", ScriptRunnerTemplateParameterType.Text, true, 0, Sensitive: true)], false, 32),
            "sensitive parameters are not allowed on user profiles");
        Throws(() => ScriptRunnerParameters.ValidateDefinitions([new("a", "A", ScriptRunnerTemplateParameterType.Text, false, 0), new("a", "B", ScriptRunnerTemplateParameterType.Text, false, 1)], false, 32),
            "duplicate parameter keys");
        Throws(() => ScriptRunnerParameters.ValidateDefinitions([new("a", "A", ScriptRunnerTemplateParameterType.Text, false, 1)], false, 32),
            "argument positions must follow declaration order");
        Throws(() => ScriptRunnerParameters.ValidateDefinitions([new("c", "C", ScriptRunnerTemplateParameterType.Choice, false, 0)], false, 32),
            "choice parameters need options");
        Throws(() => ScriptRunnerParameters.ValidateDefinitions([new("c", "C", ScriptRunnerTemplateParameterType.Choice, false, 0, Options: ["fast"], DefaultValue: "slow")], false, 32),
            "choice defaults must be declared options");
        ScriptRunnerParameters.ValidateDefinitions([new("d", "D", ScriptRunnerTemplateParameterType.File, false, 0, Example: @"C:\Data.csv")], false, 32);
        Throws(() => ScriptRunnerParameters.ValidateDefinitions([new("d", "D", ScriptRunnerTemplateParameterType.Text, false, 0, Example: "  ")], false, 32),
            "blank examples are invalid");
        Throws(() => ScriptRunnerParameters.ValidateDefinitions([new("f", "F", ScriptRunnerTemplateParameterType.File, false, 0, Options: ["x"])], false, 32),
            "only choice parameters may declare options");
        Throws(() => ScriptRunnerParameters.ValidateDefinitions([new("t", "T", ScriptRunnerTemplateParameterType.Text, false, 0, DefaultValue: true)], false, 32),
            "text parameters reject boolean defaults");
        Throws(() => ScriptRunnerParameters.ValidateDefinitions(Enumerable.Range(0, 33).Select(index => new ScriptRunnerTemplateParameter($"p{index}", "P", ScriptRunnerTemplateParameterType.Text, false, index)).ToArray(), false, 32),
            "profiles are capped at 32 inputs");
    }

    private static void MappingRules()
    {
        var parameters = Parameters();
        var defaults = ScriptRunnerParameters.Resolve(parameters, Json("""{"source":"C:\\Data"}"""), enforceRequired: true);
        Ensure(defaults.SequenceEqual(new[] { "C:\\Data", "90", "true", "safe" }), "defaults fill missing values in declaration order");
        var explicitValues = ScriptRunnerParameters.Resolve(parameters, Json("""{"source":"D:\\Logs","days":"30","dry":false,"mode":"fast"}"""), enforceRequired: true);
        Ensure(explicitValues.SequenceEqual(new[] { "D:\\Logs", "30", "false", "fast" }), "explicit values map by position");
        Throws(() => ScriptRunnerParameters.Resolve(parameters, Json("""{}"""), enforceRequired: true), "required input is enforced when running");
        Throws(() => ScriptRunnerParameters.Resolve(parameters, Json("""{"source":"Logs"}"""), enforceRequired: true), "relative folder is rejected");
        Throws(() => ScriptRunnerParameters.Resolve(parameters, Json("""{"source":"C:\\Data","dry":"yes"}"""), enforceRequired: true), "boolean inputs must be typed");
        Throws(() => ScriptRunnerParameters.Resolve(parameters, Json("""{"source":"C:\\Data","mode":"other"}"""), enforceRequired: true), "choice must be declared");
        Throws(() => ScriptRunnerParameters.Resolve(parameters, Json("""{"source":"C:\\Data","extra":"x"}"""), enforceRequired: true), "unknown keys are rejected");
        var partial = ScriptRunnerParameters.Resolve(parameters, Json("""{}"""), enforceRequired: false);
        Ensure(partial[0] == string.Empty && partial[1] == "90", "saved partial values keep required gaps until a run");
        ScriptRunnerParameters.ValidateSavedValues(parameters, Json("""{}"""));
        Throws(() => ScriptRunnerParameters.ValidateSavedValues(parameters, Json("""{"source":"Logs"}""")), "saved relative folders are rejected");
        var secret = new[] { new ScriptRunnerTemplateParameter("password", "Password", ScriptRunnerTemplateParameterType.Text, true, 0, Sensitive: true) };
        Throws(() => ScriptRunnerParameters.ValidateSavedValues(secret, Json("""{"password":"typed-secret"}""")), "sensitive values are never saved");
    }

    private static async Task ProfileRunsAsync(string root, string script)
    {
        var store = new MapLibraryStore();
        var process = new RecordingProcess();
        var module = new ScriptRunnerModule();
        var registry = new AutomationCapabilityRegistry(libraryStoreFactory: _ => store, processServiceFactory: _ => process);
        await using var context = registry.CreateContext(new(module.Id, module.Definition.Capabilities));
        var settings = module.CreateDefaultSettings();
        var profile = new ScriptRunnerProfile("report", "Report", ScriptRunnerInterpreter.Powershell, Environment.ProcessPath!, script,
            ["--fixed"], root, ScriptRunnerOutputMode.Text, 30, Parameters: Parameters(), Values: Json("""{"source":"C:\\Data"}"""));
        var saved = await module.ExecuteAsync("saveProfile", JsonSerializer.SerializeToElement(profile, Options()), settings, context, default);
        Ensure(saved.Status == AutomationStatus.Success, saved.Message);

        var prefix = new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-File", script };
        var savedRun = await module.ExecuteAsync("runProfile", Json("""{"id":"report"}"""), settings, context, default);
        Ensure(savedRun.Status == AutomationStatus.Success, savedRun.Message);
        Ensure(process.Last!.Arguments.SequenceEqual(prefix.Concat(new[] { "--fixed", "C:\\Data", "90", "true", "safe" })), "a run without values uses the remembered values");

        var overridden = await module.ExecuteAsync("runProfile",
            Json("""{"id":"report","values":{"source":"D:\\Logs","days":"30","dry":false,"mode":"fast"}}"""), settings, context, default);
        Ensure(overridden.Status == AutomationStatus.Success, overridden.Message);
        Ensure(process.Last!.Arguments.SequenceEqual(prefix.Concat(new[] { "--fixed", "D:\\Logs", "30", "false", "fast" })), "run values apply to one run");
        var storedRecord = await store.GetAsync(ScriptRunnerModule.IdValue, ScriptRunnerModule.ProfileCollection, "report", default);
        var stored = JsonSerializer.Deserialize<ScriptRunnerProfile>(storedRecord!.Data.GetRawText(), Options())!;
        Ensure(stored.Values!.Value.GetProperty("source").GetString() == "C:\\Data", "a one-run override is not remembered");

        var calls = process.Calls;
        var missing = profile with { Id = "needs-input", Values = null, Parameters = [new("name", "Name", ScriptRunnerTemplateParameterType.Text, true, 0)] };
        await module.ExecuteAsync("saveProfile", JsonSerializer.SerializeToElement(missing, Options()), settings, context, default);
        var missingRun = await module.ExecuteAsync("runProfile", Json("""{"id":"needs-input"}"""), settings, context, default);
        Ensure(missingRun.Status == AutomationStatus.Error && process.Calls == calls, "a required input without a value stops before the process starts");

        var plain = profile with { Id = "plain", Arguments = [], Parameters = null, Values = null };
        await module.ExecuteAsync("saveProfile", JsonSerializer.SerializeToElement(plain, Options()), settings, context, default);
        var plainRun = await module.ExecuteAsync("runProfile", Json("""{"id":"plain","values":{"source":"C:\\Data"}}"""), settings, context, default);
        Ensure(plainRun.Status == AutomationStatus.Error && process.Calls == calls, "profiles without inputs reject run values");

        var handler = new ScriptRunnerSavedProfileHandler(store, process);
        await handler.ExecuteAsync("report", null, new(AutomationExecutionOrigin.Workflow, "test"), default);
        Ensure(process.Last!.Arguments.SequenceEqual(prefix.Concat(new[] { "--fixed", "C:\\Data", "90", "true", "safe" })), "a workflow run uses the remembered values");
        try
        {
            await handler.ExecuteAsync("needs-input", null, new(AutomationExecutionOrigin.Workflow, "test"), default);
            throw new Exception("a workflow started without its required input");
        }
        catch (InvalidDataException) { }

        Ensure(ScriptRunnerExecution.HashProfileRevision(profile, "body") != ScriptRunnerExecution.HashProfileRevision(profile with { Parameters = null }, "body"),
            "declared inputs are part of the approved revision");
        Ensure(ScriptRunnerExecution.HashProfileRevision(profile with { Parameters = [] }, "body") == ScriptRunnerExecution.HashProfileRevision(profile with { Parameters = null }, "body"),
            "an empty input list keeps the existing approved revision");
        Ensure(ScriptRunnerExecution.BuildArguments(profile with { Parameters = null }, null).SequenceEqual(new[] { "--fixed" }),
            "profiles without inputs keep their fixed arguments");
    }

    private static IReadOnlyList<ScriptRunnerTemplateParameter> Parameters() =>
    [
        new("source", "Source folder", ScriptRunnerTemplateParameterType.Directory, true, 0),
        new("days", "Days", ScriptRunnerTemplateParameterType.Text, false, 1, DefaultValue: "90"),
        new("dry", "Dry run", ScriptRunnerTemplateParameterType.Boolean, false, 2, DefaultValue: true),
        new("mode", "Mode", ScriptRunnerTemplateParameterType.Choice, true, 3, Options: ["fast", "safe"], DefaultValue: "safe"),
    ];

    private static JsonSerializerOptions Options()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private static JsonElement Json(string text)
    {
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Throws(Action action, string label)
    {
        try { action(); }
        catch (InvalidDataException) { return; }
        throw new Exception($"Expected validation rejection: {label}");
    }

    /// <summary>Keeps every record, so profiles saved earlier in a scenario remain readable by later runs.</summary>
    private sealed class MapLibraryStore : IAutomationLibraryStore
    {
        private readonly Dictionary<(string Module, string Collection, string Id), AutomationLibraryRecord> _records = new();

        public Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(string moduleId, string collection, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AutomationLibraryRecord>>(_records.Where(pair => pair.Key.Module == moduleId && pair.Key.Collection == collection).Select(pair => pair.Value).ToArray());

        public Task<AutomationLibraryRecord?> GetAsync(string moduleId, string collection, string id, CancellationToken cancellationToken) =>
            Task.FromResult(_records.TryGetValue((moduleId, collection, id), out var record) ? record : null);

        public Task UpsertAsync(AutomationLibraryRecord record, CancellationToken cancellationToken)
        {
            _records[(record.ModuleId, record.Collection, record.Id)] = record;
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(string moduleId, string collection, string id, CancellationToken cancellationToken) =>
            Task.FromResult(_records.Remove((moduleId, collection, id)));
    }

    private sealed class RecordingProcess : IAutomationProcessService
    {
        public int Calls;
        public AutomationProcessRequest? Last;

        public Task<AutomationProcessResult> ExecuteAsync(AutomationProcessRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            Last = request;
            return Task.FromResult(new AutomationProcessResult(0, false, "ok", "", false, false, 5));
        }
    }
}
