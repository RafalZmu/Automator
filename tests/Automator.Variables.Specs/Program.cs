using System.Text.Json;
using Automator.Application.Automation;
using Automator.Core.Plugins;
using Automator.Core.Automation;
using Automator.Application.Logging;
using System.Net;
using Automator.Infrastructure.Automation;

var library = new MemoryLibrary();
var service = new AutomationVariableService(library);
var values = new Dictionary<string, JsonElement> { ["regions"] = JsonSerializer.SerializeToElement(new[] { "eu", "apac" }), ["mode"] = JsonSerializer.SerializeToElement("daily") };
await service.SetAsync(values, default);
var snapshot = await service.GetAsync(default);
Require(snapshot.Values["regions"].GetArrayLength() == 2, "array persisted");
var process = new CaptureProcess();
var script = new ScriptRunnerProfile("echo", "Echo", ScriptRunnerInterpreter.Python, @"C:\python.exe", @"C:\echo.py",
    ["prefix={{variables.mode}}", "{{variables.regions}}"], @"C:\", ScriptRunnerOutputMode.Text, 10);
await library.UpsertAsync(new("script-runner", "profiles", "echo", 1, JsonSerializer.SerializeToElement(script, new JsonSerializerOptions(JsonSerializerDefaults.Web)), DateTimeOffset.UtcNow), default);
await new ScriptRunnerSavedProfileHandler(library, process, service).ExecuteAsync("echo", null, new(AutomationExecutionOrigin.Manual, "test"), default);
Require(process.Last!.Arguments[1] == "prefix=daily" && process.Last.Arguments[2] == "[\"eu\",\"apac\"]", "script argument boundaries preserved");
var httpHandler = new CaptureHttp();
using var http = new SharedHttpService(new HttpClient(httpHandler, false), new HttpClient(httpHandler, false), new PublicResolver(), NullApplicationLog.Instance);
var api = new ApiProfile("request", "Request", "POST", "https://api.example/", false, new Dictionary<string, string>(), new Dictionary<string, string>(),
    "{\"mode\":\"{{variables.mode}}\",\"payload\":{{input}}}", ApiResponseMode.Json, 10);
await library.UpsertAsync(new("api", "profiles", api.Id, 1, JsonSerializer.SerializeToElement(api, new JsonSerializerOptions(JsonSerializerDefaults.Web)), DateTimeOffset.UtcNow), default);
await new AutomationApiProfileRunner(library, new InMemoryAutomationSecretManager(), http, NullApplicationLog.Instance, service)
    .RunProfileAsync(api.Id, JsonSerializer.SerializeToElement("{{variables.mode}}"), default);
Require(httpHandler.Body == "{\"mode\":\"daily\",\"payload\":\"{{variables.mode}}\"}", "API expands saved template but never transient input");
Require(AutomationVariableInterpolation.Expand("--mode={{variables.mode}}", snapshot.Values) == "--mode=daily", "argument interpolation");
Require(AutomationVariableInterpolation.Expand("{{variables.regions}}", snapshot.Values) == "[\"eu\",\"apac\"]", "typed JSON interpolation");
Throws(() => AutomationVariableInterpolation.Expand("{{variables.missing}}", snapshot.Values));
Throws(() => AutomationVariableService.Validate(new Dictionary<string, JsonElement> { ["secret.api-key"] = JsonSerializer.SerializeToElement("x") }));
Throws(() => AutomationVariableService.Validate(new Dictionary<string, JsonElement> { ["x"] = JsonSerializer.SerializeToElement(new string('x', 65536)) }));
Throws(() => AutomationVariableInterpolation.Expand(string.Concat(Enumerable.Repeat("{{variables.regions}}", 100_000)), snapshot.Values));

var workflow = new AutomationWorkflowProfile("daily", "Daily", [new("first", "script-runner", "echo", [])], new Dictionary<string, JsonElement> { ["mode"] = JsonSerializer.SerializeToElement("weekly"), ["regions"] = values["regions"] });
await library.UpsertAsync(new("workflows", "profiles", "daily", 1, JsonSerializer.SerializeToElement(workflow, new JsonSerializerOptions(JsonSerializerDefaults.Web)), DateTimeOffset.UtcNow), default);
snapshot = await service.GetAsync(default);
Require(snapshot.Values["workflow.daily.mode"].GetString() == "weekly", "namespaced migration");
Require(snapshot.MigrationConflicts.Count == 0, "migration no conflict");
var handler = new EchoHandler();
var engine = new AutomationWorkflowEngine(library, new AutomationSavedProfileExecutor([handler]), service);
var result = await engine.RunAsync("daily", null, default);
Require(result.Output.GetProperty("mode").GetString() == "weekly", "legacy alias preserved");
Require(result.Output.GetProperty("variables").GetProperty("regions").GetArrayLength() == 2, "global root context");
await engine.RunScheduledAsync("daily", "scheduled", default);
Require(handler.LastInput!.Value.GetProperty("mode").GetString() == "weekly", "scheduled aliases preserved");

var conflictWorkflow = new AutomationWorkflowProfile("conflict", "Conflict", [], new Dictionary<string, JsonElement> { ["mode"] = JsonSerializer.SerializeToElement("local") });
await library.UpsertAsync(new("workflows", "profiles", "conflict", 1, JsonSerializer.SerializeToElement(conflictWorkflow, new JsonSerializerOptions(JsonSerializerDefaults.Web)), DateTimeOffset.UtcNow), default);
var next = new Dictionary<string, JsonElement>(snapshot.Values) { ["workflow.conflict.mode"] = JsonSerializer.SerializeToElement("global") };
snapshot = await service.SetAsync(next, default);
Require(snapshot.MigrationConflicts.Contains("workflow.conflict.mode"), "collision reported");
Require(snapshot.Values["workflow.conflict.mode"].GetString() == "global", "global collision not overwritten");
var conflicted = await library.GetAsync("workflows", "profiles", "conflict", default);
Require(conflicted!.Data.GetProperty("variables").GetProperty("mode").GetString() == "local", "legacy conflict value retained");

var transfer = new AutomationLibraryTransferService(library);
// Remove the empty-step collision workflow, which is not a portable executable definition.
await library.DeleteAsync("workflows", "profiles", "conflict", default);
var export = await transfer.ExportAsync(default);
var target = new MemoryLibrary();
await new AutomationLibraryTransferService(target).ImportAsync(export, default);
Require((await new AutomationVariableService(target).GetAsync(default)).Values["regions"].GetArrayLength() == 2, "backup round trip");
var targetService = new AutomationVariableService(target);
var incompatible = new Dictionary<string, JsonElement>((await targetService.GetAsync(default)).Values) { ["mode"] = JsonSerializer.SerializeToElement("different") };
await targetService.SetAsync(incompatible, default);
try { await new AutomationLibraryTransferService(target).ImportAsync(export, default); throw new Exception("Expected import conflict"); }
catch (InvalidDataException) { }
Require((await targetService.GetAsync(default)).Values["mode"].GetString() == "different", "import collision preserved");
Console.WriteLine("PASS global variables validation, interpolation, migration, aliases, scheduling and backup conflicts");

static void Require(bool condition, string name) { if (!condition) throw new Exception(name); }
static void Throws(Action action) { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Expected validation failure"); }
sealed class MemoryLibrary : IAutomationLibraryStore
{
    private readonly Dictionary<(string, string, string), AutomationLibraryRecord> _records = [];
    public Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(string moduleId, string collection, CancellationToken ct) => Task.FromResult<IReadOnlyList<AutomationLibraryRecord>>(_records.Values.Where(item => item.ModuleId == moduleId && item.Collection == collection).ToArray());
    public Task<AutomationLibraryRecord?> GetAsync(string moduleId, string collection, string id, CancellationToken ct) => Task.FromResult(_records.GetValueOrDefault((moduleId, collection, id)));
    public Task UpsertAsync(AutomationLibraryRecord record, CancellationToken ct) { _records[(record.ModuleId, record.Collection, record.Id)] = record; return Task.CompletedTask; }
    public Task<bool> DeleteAsync(string moduleId, string collection, string id, CancellationToken ct) => Task.FromResult(_records.Remove((moduleId, collection, id)));
}
sealed class EchoHandler : IAutomationSavedProfileHandler
{
    public string ModuleId => "script-runner";
    public JsonElement? LastInput { get; private set; }
    public Task<IReadOnlyList<AutomationSavedProfileSummary>> ListProfilesAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<AutomationSavedProfileSummary>>([]);
    public Task<AutomationProfileExecutionOutput> ExecuteAsync(string id, JsonElement? input, AutomationExecutionMetadata metadata, CancellationToken ct) { LastInput = input; return Task.FromResult(new AutomationProfileExecutionOutput(input!.Value, new(AutomationStatus.Success, "completed", 0))); }
}
sealed class CaptureProcess : IAutomationProcessService
{
    public AutomationProcessRequest? Last { get; private set; }
    public Task<AutomationProcessResult> ExecuteAsync(AutomationProcessRequest request, CancellationToken cancellationToken)
    { Last = request; return Task.FromResult(new AutomationProcessResult(0, false, "ok", "", false, false, 1)); }
}
sealed class CaptureHttp : HttpMessageHandler
{
    public string? Body { get; private set; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    { Body = await request.Content!.ReadAsStringAsync(cancellationToken); return new(HttpStatusCode.OK) { Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json") }; }
}
sealed class PublicResolver : IHostAddressResolver
{
    public Task<IReadOnlyList<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<IPAddress>>([IPAddress.Parse("93.184.216.34")]);
}
