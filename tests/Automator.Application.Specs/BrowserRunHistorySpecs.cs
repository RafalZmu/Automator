using System.Text.Json;
using Automator.Application.Automation;
using Automator.Core.Automation;
using Automator.Core.Plugins;

/// <summary>Playwright test runs are saved to the library so their history survives the window and the app.</summary>
public static class BrowserRunHistorySpecs
{
    private const string List = "Listing tests:\n  tests\\example.spec.ts:3:5 › Automator example test\nTotal: 1 test in 1 file\n";
    private const string Report = "{\"suites\":[{\"file\":\"tests/example.spec.ts\",\"specs\":[{\"title\":\"Automator example test\",\"line\":3,\"tests\":[{\"projectName\":\"\",\"results\":[{\"status\":\"STATUS\"}]}]}]}]}";

    public static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "automator-browser-history-specs", Guid.NewGuid().ToString("N"));
        var project = Path.Combine(root, "managed");
        var module = Path.Combine(root, "playwright");
        var node = Path.Combine(root, "node.exe");
        Directory.CreateDirectory(module);
        await File.WriteAllTextAsync(node, "test node");
        await File.WriteAllTextAsync(Path.Combine(module, "cli.js"), "");
        await File.WriteAllTextAsync(Path.Combine(module, "package.json"), "{\"version\":\"1\"}");
        var reporter = Path.Combine(root, "playwright-run-reporter.cjs");
        await File.WriteAllTextAsync(reporter, "module.exports = class {};");
        var prior = new[] { "AUTOMATOR_PLAYWRIGHT_PROJECT_ROOT", "AUTOMATOR_BROWSER_NODE_PATH", "AUTOMATOR_PLAYWRIGHT_MODULE_ROOT", "AUTOMATOR_PLAYWRIGHT_RUN_REPORTER_PATH" }
            .Select(name => (name, value: Environment.GetEnvironmentVariable(name))).ToArray();
        Environment.SetEnvironmentVariable("AUTOMATOR_PLAYWRIGHT_PROJECT_ROOT", project);
        Environment.SetEnvironmentVariable("AUTOMATOR_BROWSER_NODE_PATH", node);
        Environment.SetEnvironmentVariable("AUTOMATOR_PLAYWRIGHT_MODULE_ROOT", module);
        Environment.SetEnvironmentVariable("AUTOMATOR_PLAYWRIGHT_RUN_REPORTER_PATH", reporter);
        try
        {
            var library = new MemoryLibrary();
            var process = new ScriptedProcess();
            var capabilities = new AutomationCapabilityRegistry(libraryStoreFactory: _ => library, processServiceFactory: _ => process,
                browserServiceFactory: _ => throw new InvalidOperationException("The browser is not used by Playwright test runs."));
            var browser = new BrowserAutomationModule();
            var registry = new AutomationModuleRegistry([browser], capabilities);
            var descriptor = new AutomationModuleDescriptor(browser.Id, browser.Definition.Capabilities
                .Where(capability => capability.Id != AutomationCapabilityIds.BrowserSession).ToArray());
            await using var context = capabilities.CreateContext(descriptor);
            Task<AutomationResult> Dispatch(string action, string json, CancellationToken token = default)
            {
                using var document = JsonDocument.Parse(json);
                return registry.DispatchAsync(browser.Id, action, 1, 1, document.RootElement.Clone(), browser.Id, context, [], token);
            }

            var empty = await Dispatch("listRunHistory", "{}");
            Check.Equal(0, empty.Data.GetProperty("runs").GetArrayLength());

            process.Handler = request => request.Arguments.Contains("--list")
                ? new(0, false, List, "", false, false, 5) : new(0, false, Report.Replace("STATUS", "passed"), "", false, false, 1234);
            var passed = await Dispatch("runTests", "{\"kind\":\"file\",\"file\":\"tests/example.spec.ts\"}");
            Check.Equal(AutomationStatus.Success, passed.Status);
            var runId = passed.Data.GetProperty("runId").GetString()!;

            process.Handler = request => request.Arguments.Contains("--list")
                ? new(0, false, List, "", false, false, 5) : new(1, false, Report.Replace("STATUS", "failed"), "", false, false, 900);
            var failed = await Dispatch("runTests", "{\"kind\":\"file\",\"file\":\"tests/example.spec.ts\"}");
            Check.Equal(AutomationStatus.Error, failed.Status);

            using var cancellation = new CancellationTokenSource();
            process.Handler = request =>
            {
                if (request.Arguments.Contains("--list")) return new(0, false, List, "", false, false, 5);
                cancellation.Cancel();
                throw new OperationCanceledException(cancellation.Token);
            };
            await Check.ThrowsAsync<OperationCanceledException>(() => Dispatch("runTests", "{\"kind\":\"file\",\"file\":\"tests/example.spec.ts\"}", cancellation.Token));

            var reasons = new[]
            {
                ("timeout", "timedOut", "Test timeout of 30000ms exceeded."),
                ("mismatch", "failed", string.Join("\n", "Error: expect(received).toBe(expected) // Object.is equality", "", "Expected: 4", "Received: 5")),
                ("waiting", "failed", string.Join("\n", "Error: expect(locator).toBeVisible() failed", "", "Locator: getByText('x')", "Expected: visible", "Received: <element(s) not found>", "Timeout: 5000ms")),
                ("click", "failed", string.Join("\n", "Error: page.click: Timeout 30000ms exceeded.", "Call log:", "  - waiting for locator('#go')")),
                ("boom", "failed", "TypeError: boom is not a function"),
                ("hook", "timedOut", "Test timeout of 30000ms exceeded while running \"beforeEach\" hook."),
                ("received", "failed", string.Join("\n", "Error: expect(received).toBe(expected) // Object.is equality", "", "Expected: 'Ready'", "Received: 'timed out'")),
            };
            var esc = ((char)27).ToString();
            var specs = string.Join(",", reasons.Select((item, index) => JsonSerializer.Serialize(new
            {
                title = item.Item1, line = index + 1,
                tests = new[] { new { projectName = "", results = new[] { new { status = item.Item2, errors = new[] { new { message = esc + "[31m" + item.Item3 + esc + "[39m" } } } } } },
            })));
            // Playwright marks a test that passed on a retry as flaky; the failure shown is the attempt that failed.
            var flakySpec = "{\"title\":\"flaky\",\"line\":9,\"tests\":[{\"projectName\":\"\",\"status\":\"flaky\",\"results\":[{\"status\":\"timedOut\",\"errors\":[{\"message\":\"Test timeout of 30000ms exceeded.\"}]},{\"status\":\"passed\"}]}]}";
            var reasonReport = "{\"suites\":[{\"file\":\"tests/example.spec.ts\",\"specs\":[" + specs + "," + flakySpec + "]}]}";
            process.Handler = request => request.Arguments.Contains("--list")
                ? new(0, false, List, "", false, false, 5) : new(1, false, reasonReport, "", false, false, 50);
            var reasonRun = await Dispatch("runTests", "{\"kind\":\"file\",\"file\":\"tests/example.spec.ts\"}");
            var outcomes = reasonRun.Data.GetProperty("tests").EnumerateArray().ToDictionary(test => test.GetProperty("title").GetString()!);
            Check.Equal("testTimeout", outcomes["timeout"].GetProperty("reason").GetString());
            Check.Equal("assertion", outcomes["mismatch"].GetProperty("reason").GetString());
            Check.Equal("expectTimeout", outcomes["waiting"].GetProperty("reason").GetString());
            Check.Equal("actionTimeout", outcomes["click"].GetProperty("reason").GetString());
            Check.Equal("error", outcomes["boom"].GetProperty("reason").GetString());
            Check.True(outcomes["mismatch"].GetProperty("error").GetString()!.StartsWith("Error: expect(received)", StringComparison.Ordinal));
            Check.True(!outcomes["mismatch"].GetProperty("error").GetString()!.Contains(esc));
            Check.Equal("Test timeout of 30000ms exceeded.", outcomes["timeout"].GetProperty("error").GetString());
            Check.Equal("hookTimeout", outcomes["hook"].GetProperty("reason").GetString());
            Check.Equal("assertion", outcomes["received"].GetProperty("reason").GetString());
            Check.Equal("flaky", outcomes["flaky"].GetProperty("status").GetString());
            Check.Equal("testTimeout", outcomes["flaky"].GetProperty("reason").GetString());
            Check.Equal(2, outcomes["flaky"].GetProperty("attempts").GetInt32());

            var listed = await Dispatch("listRunHistory", "{}");
            var runs = listed.Data.GetProperty("runs").EnumerateArray().ToArray();
            Check.Equal(4, runs.Length);
            Check.Equal(0, listed.Data.GetProperty("active").GetArrayLength());
            Check.True(runs.Select(run => run.GetProperty("state").GetString()).OrderBy(state => state).SequenceEqual(["canceled", "failed", "failed", "success"]));
            var saved = runs.Single(run => run.GetProperty("id").GetString() == runId);
            Check.Equal("success", saved.GetProperty("state").GetString());
            Check.Equal("tests/example.spec.ts", saved.GetProperty("target").GetString());
            Check.Equal(1234L, saved.GetProperty("durationMilliseconds").GetInt64());
            Check.Equal(1, saved.GetProperty("summary").GetProperty("passed").GetInt32());
            Check.Equal("passed", saved.GetProperty("tests")[0].GetProperty("status").GetString());
            Check.True(library.Count("run-history") == 4);

            var removed = await Dispatch("deleteRunHistory", $"{{\"id\":\"{runId}\"}}");
            Check.Equal(1, removed.Data.GetProperty("removed").GetInt32());
            Check.Equal(3, (await Dispatch("listRunHistory", "{}")).Data.GetProperty("runs").GetArrayLength());
            var cleared = await Dispatch("deleteRunHistory", "{}");
            Check.Equal(3, cleared.Data.GetProperty("removed").GetInt32());
            Check.Equal(0, (await Dispatch("listRunHistory", "{}")).Data.GetProperty("runs").GetArrayLength());

            // With the reporter, the run's events name each test, its failing step and line, flaky retries, and a runner-limit stop.
            var file = Path.Combine(project, "tests", "example.spec.ts");
            string Begin(string id, string title, int line, int retry = 0) => JsonSerializer.Serialize(new { @event = "begin", testId = id, file, line, column = 3, title, project = "", retry });
            string End(string id, string title, int line, string status, int retry = 0, object? failure = null) => JsonSerializer.Serialize(new { @event = "end", testId = id, file, line, column = 3, title, project = "", retry, status, timeoutMs = 30000, durationMs = 20, failure });
            var lines = new List<string>();
            var stopAtLimit = false;
            var eventsPath = string.Empty;
            process.Handler = request =>
            {
                if (request.Arguments.Contains("--list")) return new(0, false, List, "", false, false, 5);
                Check.True(request.Arguments.Contains($"--reporter=json,{reporter}"));
                eventsPath = request.EnvironmentVariables!["AUTOMATOR_PLAYWRIGHT_RUN_EVENTS"];
                File.WriteAllLines(eventsPath, lines);
                return stopAtLimit ? new(null, true, "", "", false, false, 600000) : new(1, false, "", "", false, false, 300);
            };
            lines.AddRange([
                Begin("hook", "Hook timeout", 4),
                End("hook", "Hook timeout", 4, "timedOut", failure: new { message = "Test timeout of 30000ms exceeded while running \"beforeEach\" hook.", snippet = "> 9 | await page.click('#go');", step = "locator.click", stepCategory = "pw:api", line = 9 }),
                Begin("wait", "Expect waits", 12),
                End("wait", "Expect waits", 12, "failed", failure: new { message = esc + "[31m" + "Error: expect(locator).toBeVisible() failed\n\nLocator: getByText('x')\nExpected: visible\nReceived: <element(s) not found>\nTimeout: 5000ms" + esc + "[39m", step = "expect.toBeVisible", stepCategory = "expect", line = 42 }),
                Begin("click", "Click waits", 20),
                End("click", "Click waits", 20, "failed", failure: new { message = "locator.click: Timeout 30000ms exceeded.\nCall log:\n  - waiting for locator('#go')", step = "locator.click", stepCategory = "pw:api", line = 21 }),
                Begin("flaky", "Flaky", 28, 0),
                End("flaky", "Flaky", 28, "failed", 0, failure: new { message = "Error: expect(locator).toHaveText(expected) failed\n\nExpected: 'Ready'\nReceived: 'Loading'\nTimeout: 5000ms", step = "expect.toHaveText", stepCategory = "expect", line = 60 }),
                Begin("flaky", "Flaky", 28, 1),
                End("flaky", "Flaky", 28, "passed", 1),
                Begin("ok", "Passes", 36),
                End("ok", "Passes", 36, "passed"),
            ]);
            var eventRun = await Dispatch("runTests", "{\"kind\":\"file\",\"file\":\"tests/example.spec.ts\"}");
            var byTitle = eventRun.Data.GetProperty("tests").EnumerateArray().ToDictionary(test => test.GetProperty("title").GetString()!);
            Check.Equal("hookTimeout", byTitle["Hook timeout"].GetProperty("reason").GetString());
            Check.Equal("locator.click", byTitle["Hook timeout"].GetProperty("step").GetString());
            Check.Equal(9, byTitle["Hook timeout"].GetProperty("failureLine").GetInt32());
            Check.True(byTitle["Hook timeout"].GetProperty("snippet").GetString()!.Contains("> 9", StringComparison.Ordinal));
            Check.Equal("expectTimeout", byTitle["Expect waits"].GetProperty("reason").GetString());
            Check.Equal("expect.toBeVisible", byTitle["Expect waits"].GetProperty("step").GetString());
            Check.Equal(42, byTitle["Expect waits"].GetProperty("failureLine").GetInt32());
            Check.True(byTitle["Expect waits"].GetProperty("error").GetString()!.Contains("Timeout: 5000ms", StringComparison.Ordinal));
            Check.True(!byTitle["Expect waits"].GetProperty("error").GetString()!.Contains(esc, StringComparison.Ordinal));
            Check.Equal("actionTimeout", byTitle["Click waits"].GetProperty("reason").GetString());
            Check.Equal(21, byTitle["Click waits"].GetProperty("failureLine").GetInt32());
            Check.Equal("flaky", byTitle["Flaky"].GetProperty("status").GetString());
            Check.Equal(2, byTitle["Flaky"].GetProperty("attempts").GetInt32());
            Check.Equal("expectTimeout", byTitle["Flaky"].GetProperty("reason").GetString());
            Check.Equal("expect.toHaveText", byTitle["Flaky"].GetProperty("step").GetString());
            Check.Equal(JsonValueKind.Null, byTitle["Passes"].GetProperty("reason").ValueKind);
            var eventSummary = eventRun.Data.GetProperty("summary");
            Check.Equal(1, eventSummary.GetProperty("passed").GetInt32());
            Check.Equal(1, eventSummary.GetProperty("flaky").GetInt32());
            Check.Equal(2, eventSummary.GetProperty("failed").GetInt32());
            Check.Equal(1, eventSummary.GetProperty("timedOut").GetInt32());
            Check.True(eventRun.Message.Contains("1 flaky", StringComparison.Ordinal) && eventRun.Message.Contains("1 timed out", StringComparison.Ordinal));
            Check.True(!File.Exists(eventsPath));

            // A runner-limit stop keeps the tests that finished and names the one that was still running.
            lines.Clear();
            lines.AddRange([Begin("ok2", "Done", 36), End("ok2", "Done", 36, "passed"), Begin("long", "Still running", 44)]);
            stopAtLimit = true;
            var limited = await Dispatch("runTests", "{\"kind\":\"file\",\"file\":\"tests/example.spec.ts\"}");
            Check.Equal(AutomationStatus.Warning, limited.Status);
            Check.True(limited.Data.GetProperty("timedOut").GetBoolean());
            var limitedTests = limited.Data.GetProperty("tests").EnumerateArray().ToDictionary(test => test.GetProperty("title").GetString()!);
            Check.Equal("timedOut", limitedTests["Still running"].GetProperty("status").GetString());
            Check.Equal("runnerLimit", limitedTests["Still running"].GetProperty("reason").GetString());
            Check.True(limitedTests["Still running"].GetProperty("error").GetString()!.Contains("600-second limit", StringComparison.Ordinal));
            Check.Equal("passed", limitedTests["Done"].GetProperty("status").GetString());
            var eventHistory = (await Dispatch("listRunHistory", "{}")).Data.GetProperty("runs").EnumerateArray().ToArray();
            Check.Equal(2, eventHistory.Length);
            Check.True(eventHistory.Any(run => run.GetProperty("state").GetString() == "timedOut"));
            Check.True(eventHistory.Any(run => run.GetProperty("state").GetString() == "failed"));
            Check.True(!File.Exists(eventsPath));
            Check.Equal(2, (await Dispatch("deleteRunHistory", "{}")).Data.GetProperty("removed").GetInt32());
        }
        finally
        {
            foreach (var (name, value) in prior) Environment.SetEnvironmentVariable(name, value);
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class ScriptedProcess : IAutomationProcessService
    {
        public Func<AutomationProcessRequest, AutomationProcessResult>? Handler { get; set; }
        public Task<AutomationProcessResult> ExecuteAsync(AutomationProcessRequest request, CancellationToken cancellationToken)
        {
            try { return Task.FromResult(Handler!(request)); }
            catch (OperationCanceledException exception) { return Task.FromCanceled<AutomationProcessResult>(exception.CancellationToken); }
        }
    }

    private sealed class MemoryLibrary : IAutomationLibraryStore
    {
        private readonly Dictionary<(string Module, string Collection, string Id), AutomationLibraryRecord> _records = [];
        public int Count(string collection) => _records.Values.Count(record => record.Collection == collection);
        public Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(string moduleId, string collection, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AutomationLibraryRecord>>(_records.Values.Where(record => record.ModuleId == moduleId && record.Collection == collection).ToArray());
        public Task<AutomationLibraryRecord?> GetAsync(string moduleId, string collection, string id, CancellationToken cancellationToken) =>
            Task.FromResult(_records.GetValueOrDefault((moduleId, collection, id)));
        public Task UpsertAsync(AutomationLibraryRecord record, CancellationToken cancellationToken) { _records[(record.ModuleId, record.Collection, record.Id)] = record; return Task.CompletedTask; }
        public Task<bool> DeleteAsync(string moduleId, string collection, string id, CancellationToken cancellationToken) => Task.FromResult(_records.Remove((moduleId, collection, id)));
    }
}
