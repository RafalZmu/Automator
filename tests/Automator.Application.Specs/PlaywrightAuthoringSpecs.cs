using System.Text.Json;
using Automator.Application.Automation;
using Automator.Application.Launcher;
using Automator.Core.Automation;
using Automator.Core.Plugins;

public static class PlaywrightAuthoringSpecs
{
    private const string ProposedSource = "import { test, expect } from '@playwright/test';\n\ntest('user can sign in', async ({ page }) => {\n  await page.goto('/login');\n  await expect(page).toHaveURL(/login/);\n});\n";

    public static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "automator-playwright-authoring", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "tests"));
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "tests", "auth.setup.ts"), "export const loginHelper = 1;\n");
            var cli = new FakeAuthoringCli();
            var process = new CodegenProcess();
            var (module, context, settings) = await CreateAsync(root, cli, process);
            await using var _ = context;

            // New file: the proposal includes project helper context and the user's notes, and does not touch the disk.
            var proposal = await module.ExecuteAsync("proposeTest",
                Json("{\"file\":\"tests/login.spec.ts\",\"recording\":\"await page.goto('/login')\",\"notes\":\"login is in the shared setup\",\"provider\":\"claude\"}"),
                settings, context, CancellationToken.None);
            Check.Equal(AutomationStatus.Success, proposal.Status);
            Check.Equal(false, proposal.Data.GetProperty("exists").GetBoolean());
            Check.True(cli.LastPrompt!.Contains("login is in the shared setup"));
            Check.True(cli.LastPrompt!.Contains("tests/auth.setup.ts"));
            Check.False(File.Exists(Path.Combine(root, "tests", "login.spec.ts")));

            var apply = await module.ExecuteAsync("applyTest", Json(JsonSerializer.Serialize(new { file = "tests/login.spec.ts", source = ProposedSource })),
                settings, context, CancellationToken.None);
            Check.Equal(AutomationStatus.Success, apply.Status);
            Check.Equal(ProposedSource, await File.ReadAllTextAsync(Path.Combine(root, "tests", "login.spec.ts")));

            // Creating the same file again must not overwrite it.
            var duplicate = await module.ExecuteAsync("applyTest", Json(JsonSerializer.Serialize(new { file = "tests/login.spec.ts", source = ProposedSource })),
                settings, context, CancellationToken.None);
            Check.Equal(AutomationStatus.Error, duplicate.Status);

            // Existing file: the proposal returns the previous source and a base hash; a stale base is rejected.
            var edit = await module.ExecuteAsync("proposeTest",
                Json("{\"file\":\"tests/login.spec.ts\",\"recording\":\"await page.getByRole('button').click()\"}"), settings, context, CancellationToken.None);
            Check.True(edit.Data.GetProperty("exists").GetBoolean());
            Check.Equal(ProposedSource, edit.Data.GetProperty("previousSource").GetString());
            var baseHash = edit.Data.GetProperty("baseHash").GetString();
            await File.AppendAllTextAsync(Path.Combine(root, "tests", "login.spec.ts"), "// edited by hand\n");
            var stale = await module.ExecuteAsync("applyTest", Json(JsonSerializer.Serialize(new { file = "tests/login.spec.ts", source = ProposedSource + "// ai\n", baseHash })),
                settings, context, CancellationToken.None);
            Check.Equal(AutomationStatus.Error, stale.Status);
            Check.True((await File.ReadAllTextAsync(Path.Combine(root, "tests", "login.spec.ts"))).Contains("edited by hand"));

            // Unsafe paths and non-test output are rejected before anything is written.
            foreach (var bad in new[] { "../escape.spec.ts", "tests/readme.md", "C:/abs.spec.ts" })
            {
                var rejected = await module.ExecuteAsync("applyTest", Json(JsonSerializer.Serialize(new { file = bad, source = ProposedSource })), settings, context, CancellationToken.None);
                Check.Equal(AutomationStatus.Error, rejected.Status);
            }
            var notATest = await module.ExecuteAsync("applyTest", Json(JsonSerializer.Serialize(new { file = "tests/x.spec.ts", source = "console.log(1)" })), settings, context, CancellationToken.None);
            Check.Equal(AutomationStatus.Error, notATest.Status);
            Check.False(File.Exists(Path.Combine(root, "tests", "x.spec.ts")));

            // Recording: codegen runs through the project's Playwright CLI and its output file becomes the recording.
            var fakeNode = Path.Combine(root, "node.exe");
            await File.WriteAllTextAsync(fakeNode, "");
            var cliDirectory = Path.Combine(root, "node_modules", "@playwright", "test");
            Directory.CreateDirectory(cliDirectory);
            await File.WriteAllTextAsync(Path.Combine(cliDirectory, "cli.js"), "");
            await File.WriteAllTextAsync(Path.Combine(cliDirectory, "package.json"), "{\"version\":\"1.50.0\"}");
            var previousNode = Environment.GetEnvironmentVariable("AUTOMATOR_BROWSER_NODE_PATH");
            Environment.SetEnvironmentVariable("AUTOMATOR_BROWSER_NODE_PATH", fakeNode);
            try
            {
                var recorded = await module.ExecuteAsync("recordTest", Json("{\"url\":\"https://app.example.com/login\"}"), settings, context, CancellationToken.None);
                Check.Equal(AutomationStatus.Success, recorded.Status);
                Check.Equal("await page.goto('https://app.example.com/login');", recorded.Data.GetProperty("code").GetString());
                Check.True(process.Last!.Arguments.Contains("codegen"));
                Check.Equal("https://app.example.com/login", process.Last.Arguments[^1]);
                Check.False(File.Exists(process.LastOutput!));

                var rejectedUrl = await module.ExecuteAsync("recordTest", Json("{\"url\":\"file:///C:/secret.html\"}"), settings, context, CancellationToken.None);
                Check.Equal(AutomationStatus.Error, rejectedUrl.Status);

                // Installing the browser only runs the project's own CLI with fixed arguments.
                var installed = await module.ExecuteAsync("installBrowsers", Json("{}"), settings, context, CancellationToken.None);
                Check.Equal(AutomationStatus.Success, installed.Status);
                Check.True(process.Last!.Arguments.SequenceEqual([process.Last.Arguments[0], "install", "chromium"]));

                process.WriteOutput = false;
                var empty = await module.ExecuteAsync("recordTest", Json("{}"), settings, context, CancellationToken.None);
                Check.Equal(AutomationStatus.Warning, empty.Status);
            }
            finally { Environment.SetEnvironmentVariable("AUTOMATOR_BROWSER_NODE_PATH", previousNode); }

            // CLI failures surface their fixed message instead of a generic one.
            cli.Failure = new InvalidOperationException("Claude CLI is signed out.");
            var failed = await module.ExecuteAsync("proposeTest", Json("{\"file\":\"tests/new.spec.ts\",\"recording\":\"x\",\"provider\":\"claude\"}"), settings, context, CancellationToken.None);
            Check.Equal(AutomationStatus.Error, failed.Status);
            Check.Equal("Claude CLI is signed out.", failed.Message);
        }
        finally { try { Directory.Delete(root, recursive: true); } catch (IOException) { } }
    }

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static async Task<(BrowserAutomationModule Module, AutomationServicesContext Context, JsonElement Settings)> CreateAsync(string root, FakeAuthoringCli cli, IAutomationProcessService process)
    {
        var library = new MemoryLibrary();
        await library.UpsertAsync(new AutomationLibraryRecord(BrowserAutomationModule.IdValue, "test-explorer", "active-project", 1,
            JsonSerializer.SerializeToElement(new { projectRoot = root }), DateTimeOffset.UtcNow), CancellationToken.None);
        var module = new BrowserAutomationModule(authoringClientFactory: (_, _) => cli);
        var context = new AutomationCapabilityRegistry(libraryStoreFactory: _ => library, processServiceFactory: _ => process)
            .CreateContext(new AutomationModuleDescriptor(BrowserAutomationModule.IdValue, [new(AutomationCapabilityIds.LibraryStorage, 1), new(AutomationCapabilityIds.ProcessExecution, 1)]));
        return (module, context, module.CreateDefaultSettings());
    }

    private sealed class FakeAuthoringCli : IAuthoringCliClient
    {
        public string? LastPrompt { get; private set; }
        public Exception? Failure { get; set; }
        public bool IsAvailable => true;
        public Task<CodexTaskStatus> GetStatusAsync(CancellationToken cancellationToken) => Task.FromResult(new CodexTaskStatus(true, "ready", "ready"));
        public Task<JsonElement> GenerateAsync(string prompt, CancellationToken cancellationToken)
        {
            LastPrompt = prompt;
            if (Failure is not null) throw Failure;
            var json = JsonSerializer.Serialize(new { plan = "Converted the recording.", format = "playwright", source = ProposedSource, scope = Array.Empty<string>(), inputs = Array.Empty<object>(), effects = Array.Empty<object>() });
            return Task.FromResult(CodexCliClient.ParseStructuredResult(json, 128 * 1024));
        }
    }

    private sealed class CodegenProcess : IAutomationProcessService
    {
        public AutomationProcessRequest? Last { get; private set; }
        public string? LastOutput { get; private set; }
        public bool WriteOutput { get; set; } = true;
        public async Task<AutomationProcessResult> ExecuteAsync(AutomationProcessRequest request, CancellationToken cancellationToken)
        {
            Last = request;
            var index = request.Arguments.ToList().IndexOf("--output");
            LastOutput = request.Arguments[index + 1];
            if (WriteOutput) await File.WriteAllTextAsync(LastOutput, request.Arguments[^1].StartsWith("http") ? $"await page.goto('{request.Arguments[^1]}');" : "await page.goto('about:blank');", cancellationToken);
            return new AutomationProcessResult(0, false, "", "", false, false, 1);
        }
    }

    private sealed class MemoryLibrary : IAutomationLibraryStore
    {
        private readonly Dictionary<(string, string, string), AutomationLibraryRecord> _records = [];
        public Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(string moduleId, string collection, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AutomationLibraryRecord>>(_records.Values.Where(r => r.ModuleId == moduleId && r.Collection == collection).ToArray());
        public Task<AutomationLibraryRecord?> GetAsync(string moduleId, string collection, string id, CancellationToken cancellationToken) =>
            Task.FromResult(_records.GetValueOrDefault((moduleId, collection, id)));
        public Task UpsertAsync(AutomationLibraryRecord record, CancellationToken cancellationToken) { _records[(record.ModuleId, record.Collection, record.Id)] = record; return Task.CompletedTask; }
        public Task<bool> DeleteAsync(string moduleId, string collection, string id, CancellationToken cancellationToken) => Task.FromResult(_records.Remove((moduleId, collection, id)));
    }
}
