using System.Text.Json;
using Automator.Application.Automation;
using Automator.Core.Automation;
using Automator.Core.Configuration;
using Automator.Core.Plugins;

var tests = new (string Name, Func<Task> Run)[]
{
    ("workflow provider declares slot five and stores under its own library partition", SaveUsesWorkflowLibraryPartition),
    ("workflow rejects forward references, collisions, and unsupported profile modules", RejectsInvalidSteps),
    ("workflow preserves explicit JSON null literals", PreservesLiteralNull),
    ("workflow run calls the scoped host runner using only the saved workflow id", RunUsesSavedHostWorkflow),
    ("workflow profile catalog allows only registered module types", ListsOnlySupportedProfileTypes),
    ("workflow edit loads a single profile and oversized run input is rejected", LoadsSingleWorkflowAndBoundsInput),
    ("workflow accepts root variable mappings and rejects empty pointer segments", AcceptsRootVariableMappingsAndRejectsEmptyPointerSegments),
    ("workflow profiles persist typed variables and default legacy records to an empty object", PersistsVariablesAndDefaultsLegacyRecords),
    ("workflow runs map saved variables and transient legacy input without storing values in history", RunsWithSavedVariablesAndSafeHistory),
};

var failures = new List<string>();
foreach (var (name, run) in tests)
{
    try
    {
        await run();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        failures.Add($"FAIL {name}: {exception.Message}");
        Console.WriteLine(failures[^1]);
    }
}

Console.WriteLine($"{tests.Length - failures.Count}/{tests.Length} workflow specifications passed.");
return failures.Count == 0 ? 0 : 1;

static async Task SaveUsesWorkflowLibraryPartition()
{
    var module = new WorkflowModule();
    var store = new MemoryLibraryStore();
    var runner = new FakeWorkflowRunner();
    var (capabilities, context) = CreateContext(module, store, runner);
    await using (context)
    {
        using var input = JsonDocument.Parse(Fixtures.ValidWorkflowJson);
        var result = await module.ExecuteAsync("saveWorkflow", input.RootElement, Fixtures.EmptyObject(),
            context, CancellationToken.None);
        Equal(AutomationStatus.Success, result.Status);
        Equal(5, module.Definition.Slot);
        Equal("workflows", module.Id);
        Equal("workflows", store.LastWrite?.ModuleId);
        Equal("profiles", store.LastWrite?.Collection);
        Equal("daily-report", store.LastWrite?.Id);
    }
    _ = capabilities;
}

static async Task RejectsInvalidSteps()
{
    var module = new WorkflowModule();
    var store = new MemoryLibraryStore();
    var runner = new FakeWorkflowRunner();
    var (_, context) = CreateContext(module, store, runner);
    await using (context)
    {
        using var forwardReference = JsonDocument.Parse("""
        {"id":"broken","name":"Broken","steps":[
          {"id":"first","moduleId":"api","profileId":"request","inputs":[{"targetJsonPointer":"/item","sourceStepId":"later","sourceJsonPointer":"/value","literalPresent":false}]},
          {"id":"later","moduleId":"script-runner","profileId":"script","inputs":[]}
        ]}
        """);
        var forwardResult = await module.ExecuteAsync("saveWorkflow", forwardReference.RootElement, Fixtures.EmptyObject(),
            context, CancellationToken.None);
        Equal(AutomationStatus.Error, forwardResult.Status);
        Contains("earlier step", forwardResult.Message);

        using var unsupported = JsonDocument.Parse("""
        {"id":"bad-module","name":"Bad module","steps":[{"id":"first","moduleId":"launcher","profileId":"open","inputs":[]}]}
        """);
        var unsupportedResult = await module.ExecuteAsync("saveWorkflow", unsupported.RootElement, Fixtures.EmptyObject(),
            context, CancellationToken.None);
        Equal(AutomationStatus.Error, unsupportedResult.Status);
        Contains("unsupported", unsupportedResult.Message);

        using var collision = JsonDocument.Parse("""
        {"id":"collision","name":"Collision","steps":[{"id":"first","moduleId":"api","profileId":"request","inputs":[
          {"targetJsonPointer":"/settings","literal":{},"literalPresent":true},
          {"targetJsonPointer":"/settings/mode","literal":"fast","literalPresent":true}
        ]}]}
        """);
        var collisionResult = await module.ExecuteAsync("saveWorkflow", collision.RootElement, Fixtures.EmptyObject(),
            context, CancellationToken.None);
        Equal(AutomationStatus.Error, collisionResult.Status);
        Contains("conflicting", collisionResult.Message);
        Equal(null, store.LastWrite);
    }
}

static async Task PreservesLiteralNull()
{
    var module = new WorkflowModule();
    var store = new MemoryLibraryStore();
    var runner = new FakeWorkflowRunner();
    var (_, context) = CreateContext(module, store, runner);
    await using (context)
    {
        using var input = JsonDocument.Parse("""
        {"id":"null-value","name":"Null value","steps":[{"id":"first","moduleId":"api","profileId":"request","inputs":[
          {"targetJsonPointer":"/optional","literal":null,"literalPresent":true}
        ]}]}
        """);
        var result = await module.ExecuteAsync("saveWorkflow", input.RootElement, Fixtures.EmptyObject(), context, CancellationToken.None);
        Equal(AutomationStatus.Success, result.Status);
        var binding = store.LastWrite!.Data.GetProperty("steps")[0].GetProperty("inputs")[0];
        Equal(true, binding.GetProperty("literalPresent").GetBoolean());
        Equal(JsonValueKind.Null, binding.GetProperty("literal").ValueKind);
    }
}

static async Task RunUsesSavedHostWorkflow()
{
    var module = new WorkflowModule();
    var store = new MemoryLibraryStore();
    var runner = new FakeWorkflowRunner();
    using var stored = JsonDocument.Parse(Fixtures.ValidWorkflowJson);
    store.Seed("workflows", "profiles", "daily-report", 1, stored.RootElement);
    var (_, context) = CreateContext(module, store, runner);
    await using (context)
    {
        using var input = JsonDocument.Parse("""{"workflowId":"daily-report","initialInput":{"region":"eu"}}""");
        var result = await module.ExecuteAsync("runWorkflow", input.RootElement, Fixtures.EmptyObject(), context, CancellationToken.None);
        Equal(AutomationStatus.Success, result.Status);
        Equal("daily-report", runner.LastWorkflowId);
        Equal("eu", runner.InitialInput!.Value.GetProperty("region").GetString());
        Equal("ready", result.Data.GetProperty("output").GetProperty("status").GetString());
        Equal("seed", result.Data.GetProperty("steps")[0].GetProperty("output").GetProperty("source").GetString());
        Equal(0, store.WriteCountAfterSeed);
    }
}

static async Task ListsOnlySupportedProfileTypes()
{
    var module = new WorkflowModule();
    var runner = new FakeWorkflowRunner();
    var (_, context) = CreateContext(module, new MemoryLibraryStore(), runner);
    await using (context)
    {
        using var supported = JsonDocument.Parse("""{"moduleId":"api"}""");
        var result = await module.ExecuteAsync("listSavedProfiles", supported.RootElement, Fixtures.EmptyObject(), context, CancellationToken.None);
        Equal(AutomationStatus.Success, result.Status);
        Equal("publish", result.Data.GetProperty("profiles")[0].GetProperty("profileId").GetString());

        using var unsupported = JsonDocument.Parse("""{"moduleId":"launcher"}""");
        var rejected = await module.ExecuteAsync("listSavedProfiles", unsupported.RootElement, Fixtures.EmptyObject(), context, CancellationToken.None);
        Equal(AutomationStatus.Error, rejected.Status);
        Equal(1, runner.CatalogCalls);
    }
}

static async Task LoadsSingleWorkflowAndBoundsInput()
{
    var module = new WorkflowModule();
    var store = new MemoryLibraryStore();
    var runner = new FakeWorkflowRunner();
    using var stored = JsonDocument.Parse(Fixtures.ValidWorkflowJson);
    store.Seed("workflows", "profiles", "daily-report", 1, stored.RootElement);
    var (_, context) = CreateContext(module, store, runner);
    await using (context)
    {
        using var getInput = JsonDocument.Parse("""{"workflowId":"daily-report"}""");
        var loaded = await module.ExecuteAsync("getWorkflow", getInput.RootElement, Fixtures.EmptyObject(), context, CancellationToken.None);
        Equal(AutomationStatus.Success, loaded.Status);
        Equal("daily-report", loaded.Data.GetProperty("workflow").GetProperty("id").GetString());

        var largeInitialInput = JsonSerializer.Serialize(new
        {
            workflowId = "daily-report",
            initialInput = new { payload = new string('x', 49 * 1024) }
        });
        using var runInput = JsonDocument.Parse(largeInitialInput);
        var rejected = await module.ExecuteAsync("runWorkflow", runInput.RootElement, Fixtures.EmptyObject(), context, CancellationToken.None);
        Equal(AutomationStatus.Error, rejected.Status);
        Contains("48 KiB", rejected.Message);
        Equal(null, runner.LastWorkflowId);
    }
}

static async Task AcceptsRootVariableMappingsAndRejectsEmptyPointerSegments()
{
    var module = new WorkflowModule();
    var store = new MemoryLibraryStore();
    var runner = new FakeWorkflowRunner();
    var (_, context) = CreateContext(module, store, runner);
    await using (context)
    {
        using var initialInputMapping = JsonDocument.Parse("""
        {"id":"use-input","name":"Use initial input","steps":[
          {"id":"first","moduleId":"api","profileId":"request","inputs":[
            {"targetJsonPointer":"/region","sourceStepId":"$input","sourceJsonPointer":"/region","literalPresent":false}
          ]}
        ]}
        """);
        var accepted = await module.ExecuteAsync("saveWorkflow", initialInputMapping.RootElement, Fixtures.EmptyObject(), context, CancellationToken.None);
        Equal(AutomationStatus.Success, accepted.Status);

        using var emptySegment = JsonDocument.Parse("""
        {"id":"bad-pointer","name":"Bad pointer","steps":[{"id":"first","moduleId":"api","profileId":"request","inputs":[
          {"targetJsonPointer":"/settings/","literal":true,"literalPresent":true}
        ]}]}
        """);
        var rejected = await module.ExecuteAsync("saveWorkflow", emptySegment.RootElement, Fixtures.EmptyObject(), context, CancellationToken.None);
        Equal(AutomationStatus.Error, rejected.Status);
        Contains("pointer", rejected.Message);
    }
}

static async Task PersistsVariablesAndDefaultsLegacyRecords()
{
    var module = new WorkflowModule();
    var store = new MemoryLibraryStore();
    var runner = new FakeWorkflowRunner();
    var (_, context) = CreateContext(module, store, runner);
    await using (context)
    {
        using var withVariables = JsonDocument.Parse("""
        {"id":"with-vars","name":"With variables","variables":{"regions":["eu","apac"],"enabled":true,"count":4},"steps":[{"id":"first","moduleId":"api","profileId":"request","inputs":[]}]}
        """);
        var saved = await module.ExecuteAsync("saveWorkflow", withVariables.RootElement, Fixtures.EmptyObject(), context, CancellationToken.None);
        Equal(AutomationStatus.Success, saved.Status);
        Equal(JsonValueKind.Array, store.LastWrite!.Data.GetProperty("variables").GetProperty("regions").ValueKind);
        Equal(true, store.LastWrite.Data.GetProperty("variables").GetProperty("enabled").GetBoolean());
        Equal(4, store.LastWrite.Data.GetProperty("variables").GetProperty("count").GetInt32());

        using var legacy = JsonDocument.Parse("""
        {"id":"legacy","name":"Legacy","steps":[{"id":"first","moduleId":"api","profileId":"request","inputs":[]}]}
        """);
        store.Seed("workflows", "profiles", "legacy", 1, legacy.RootElement);
        using var getInput = JsonDocument.Parse("""{"workflowId":"legacy"}""");
        var loaded = await module.ExecuteAsync("getWorkflow", getInput.RootElement, Fixtures.EmptyObject(), context, CancellationToken.None);
        Equal(AutomationStatus.Success, loaded.Status);
        Equal(JsonValueKind.Object, loaded.Data.GetProperty("workflow").GetProperty("variables").ValueKind);
        Equal(0, loaded.Data.GetProperty("workflow").GetProperty("variables").EnumerateObject().Count());

        using var invalid = JsonDocument.Parse("""
        {"id":"invalid-vars","name":"Invalid variable key","variables":{"bad/key":1},"steps":[{"id":"first","moduleId":"api","profileId":"request","inputs":[]}]}
        """);
        var rejected = await module.ExecuteAsync("saveWorkflow", invalid.RootElement, Fixtures.EmptyObject(), context, CancellationToken.None);
        Equal(AutomationStatus.Error, rejected.Status);
        Contains("variable key", rejected.Message);

        using var duplicateKeys = JsonDocument.Parse("""
        {"id":"duplicate-vars","name":"Duplicate variables","variables":{"region":"eu","region":"apac"},"steps":[{"id":"first","moduleId":"api","profileId":"request","inputs":[]}]}
        """);
        var duplicateRejected = await module.ExecuteAsync("saveWorkflow", duplicateKeys.RootElement, Fixtures.EmptyObject(), context, CancellationToken.None);
        Equal(AutomationStatus.Error, duplicateRejected.Status);

        var tooMany = Enumerable.Range(0, 65).ToDictionary(index => $"v{index}", index => index);
        var tooManyJson = JsonSerializer.Serialize(new
        {
            id = "too-many-vars",
            name = "Too many variables",
            variables = tooMany,
            steps = new[] { new { id = "first", moduleId = "api", profileId = "request", inputs = Array.Empty<object>() } },
        });
        using var tooManyVariables = JsonDocument.Parse(tooManyJson);
        var tooManyRejected = await module.ExecuteAsync("saveWorkflow", tooManyVariables.RootElement, Fixtures.EmptyObject(), context, CancellationToken.None);
        Equal(AutomationStatus.Error, tooManyRejected.Status);
        Contains("64 variables", tooManyRejected.Message);
    }
}

static async Task RunsWithSavedVariablesAndSafeHistory()
{
    var store = new MemoryLibraryStore();
    using var stored = JsonDocument.Parse("""
    {"id":"workflow-vars","name":"Variables","variables":{"region":"eu","groups":["qa","staging"],"secret":"only-in-run"},"steps":[
      {"id":"root","moduleId":"api","profileId":"root-probe","inputs":[]},
      {"id":"mapped","moduleId":"api","profileId":"mapped-probe","inputs":[{"targetJsonPointer":"/selected","sourceStepId":"$input","sourceJsonPointer":"/groups/1","literalPresent":false}]}
    ]}
    """);
    store.Seed("workflows", "profiles", "workflow-vars", 1, stored.RootElement);
    var probe = new CapturingWorkflowProfileHandler("root-probe", "mapped-probe");
    var engine = new AutomationWorkflowEngine(store, new AutomationSavedProfileExecutor([probe]));
    using var legacyInput = JsonDocument.Parse("""{"region":"us","request":"transient-only"}""");

    var result = await engine.RunAsync("workflow-vars", legacyInput.RootElement, CancellationToken.None);
    Equal(AutomationStatus.Success, result.Summary.Status);
    var rootInput = probe.Inputs["root-probe"]!.Value;
    Equal("us", rootInput.GetProperty("region").GetString());
    Equal("staging", rootInput.GetProperty("groups")[1].GetString());
    Equal("transient-only", rootInput.GetProperty("request").GetString());
    Equal("only-in-run", rootInput.GetProperty("secret").GetString());
    Equal("staging", probe.Inputs["mapped-probe"]!.Value.GetProperty("selected").GetString());

    var history = await store.ListAsync("workflows", "run-history", CancellationToken.None);
    Equal(1, history.Count);
    var savedHistory = history[0].Data.GetRawText();
    if (savedHistory.Contains("only-in-run", StringComparison.Ordinal) || savedHistory.Contains("transient-only", StringComparison.Ordinal))
        throw new InvalidOperationException("Workflow variables and transient run input must not be persisted to run history.");
}

static (AutomationCapabilityRegistry Registry, AutomationServicesContext Context) CreateContext(
    WorkflowModule module, MemoryLibraryStore store, FakeWorkflowRunner runner)
{
    var registry = new AutomationCapabilityRegistry(
        libraryStoreFactory: _ => store,
        workflowRunnerFactory: _ => runner);
    return (registry, registry.CreateContext(new AutomationModuleDescriptor(module.Id, module.Definition.Capabilities)));
}

static void Equal<T>(T expected, T? actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
}

static void Contains(string expected, string actual)
{
    if (!actual.Contains(expected, StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException($"Expected '{actual}' to contain '{expected}'.");
}

sealed class MemoryLibraryStore : IAutomationLibraryStore
{
    private readonly Dictionary<(string ModuleId, string Collection, string Id), AutomationLibraryRecord> _records = [];
    public AutomationLibraryRecord? LastWrite { get; private set; }
    public int WriteCountAfterSeed { get; private set; }

    public Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(string moduleId, string collection, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AutomationLibraryRecord>>(_records.Values
            .Where(item => item.ModuleId == moduleId && item.Collection == collection).ToArray());

    public Task<AutomationLibraryRecord?> GetAsync(string moduleId, string collection, string id, CancellationToken cancellationToken) =>
        Task.FromResult(_records.GetValueOrDefault((moduleId, collection, id)));

    public Task UpsertAsync(AutomationLibraryRecord record, CancellationToken cancellationToken)
    {
        LastWrite = record with { Data = record.Data.Clone() };
        _records[(record.ModuleId, record.Collection, record.Id)] = LastWrite;
        WriteCountAfterSeed++;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string moduleId, string collection, string id, CancellationToken cancellationToken) =>
        Task.FromResult(_records.Remove((moduleId, collection, id)));

    public void Seed(string moduleId, string collection, string id, int schemaVersion, JsonElement data) =>
        _records[(moduleId, collection, id)] = new AutomationLibraryRecord(moduleId, collection, id, schemaVersion, data.Clone(), DateTimeOffset.UtcNow);
}

sealed class FakeWorkflowRunner : IAutomationWorkflowRunner
{
    public string? LastWorkflowId { get; private set; }
    public JsonElement? InitialInput { get; private set; }
    public int CatalogCalls { get; private set; }

    public Task<IReadOnlyList<AutomationSavedProfileSummary>> ListSavedProfilesAsync(string moduleId, CancellationToken cancellationToken)
    {
        CatalogCalls++;
        return Task.FromResult<IReadOnlyList<AutomationSavedProfileSummary>>([new("publish", "Publish report")]);
    }

    public Task<AutomationWorkflowRunResult> RunAsync(string workflowId, JsonElement? initialInput, CancellationToken cancellationToken)
    {
        LastWorkflowId = workflowId;
        InitialInput = initialInput?.Clone();
        using var output = JsonDocument.Parse("""{"status":"ready"}""");
        using var stepOutput = JsonDocument.Parse("""{"source":"seed"}""");
        var summary = new AutomationExecutionSummary(AutomationStatus.Success, "completed", 12);
        var steps = new[]
        {
            new AutomationWorkflowStepResult("fetch", "api", "publish", AutomationStatus.Success, stepOutput.RootElement.Clone(), summary),
        };
        return Task.FromResult(new AutomationWorkflowRunResult(output.RootElement.Clone(), steps, summary));
    }
}

sealed class CapturingWorkflowProfileHandler(params string[] profileIds) : IAutomationSavedProfileHandler
{
    public string ModuleId => "api";
    public Dictionary<string, JsonElement?> Inputs { get; } = new(StringComparer.Ordinal);

    public Task<IReadOnlyList<AutomationSavedProfileSummary>> ListProfilesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AutomationSavedProfileSummary>>(profileIds.Select(id => new AutomationSavedProfileSummary(id, id)).ToArray());

    public Task<AutomationProfileExecutionOutput> ExecuteAsync(string requestedProfileId, JsonElement? input,
        AutomationExecutionMetadata metadata, CancellationToken cancellationToken)
    {
        if (!profileIds.Contains(requestedProfileId, StringComparer.Ordinal)) throw new InvalidDataException("Unexpected profile.");
        Inputs[requestedProfileId] = input?.Clone();
        using var output = JsonDocument.Parse("{}");
        return Task.FromResult(new AutomationProfileExecutionOutput(output.RootElement.Clone(),
            new AutomationExecutionSummary(AutomationStatus.Success, "completed", 1)));
    }
}

static class Fixtures
{
    public const string ValidWorkflowJson = """
    {"id":"daily-report","name":"Daily report","steps":[
      {"id":"fetch","moduleId":"api","profileId":"publish","inputs":[{"targetJsonPointer":"/region","literal":"eu","literalPresent":true}]},
      {"id":"summarize","moduleId":"script-runner","profileId":"summarize","inputs":[{"targetJsonPointer":"/data","sourceStepId":"fetch","sourceJsonPointer":"/items","literalPresent":false}]}
    ]}
    """;

    public static JsonElement EmptyObject()
    {
        using var document = JsonDocument.Parse("{}");
        return document.RootElement.Clone();
    }
}
