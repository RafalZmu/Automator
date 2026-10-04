using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Automator.Application.Automation;
using Automator.Infrastructure.Automation;

internal static class AutomationLibraryTransferSpecs
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static async Task ExportsOnlyPortableDefinitions()
    {
        var store = new MemoryLibraryStore();
        var root = Path.Combine(Path.GetTempPath(), "AutomatorTransfer", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var scriptPath = Path.Combine(root, "job.py");
        await File.WriteAllTextAsync(scriptPath, "print('ok')");
        var interpreter = Environment.ProcessPath ?? throw new InvalidOperationException("Test process path unavailable.");
        await store.UpsertAsync(Record("script-runner", "profiles", "python-job", new ScriptRunnerProfile(
            "python-job", "Python job", ScriptRunnerInterpreter.Python, interpreter, scriptPath, [], root,
            ScriptRunnerOutputMode.Text, 30)), CancellationToken.None);

        using var apiDefaultInput = JsonDocument.Parse("{\"query\":\"status\",\"limit\":5}");
        var api = new ApiProfile("api-check", "API check", "GET", "https://api.example/v1", false,
            new Dictionary<string, string> { ["X-Client"] = "automator" },
            new Dictionary<string, string> { ["Authorization"] = "opaque-ref" }, null, ApiResponseMode.Json, 20, apiDefaultInput.RootElement.Clone());
        var apiNode = JsonSerializer.SerializeToNode(api, JsonOptions)!;
        apiNode["unexpectedSecretValue"] = "must-not-export";
        await store.UpsertAsync(new AutomationLibraryRecord("api", "profiles", api.Id, 1,
            JsonSerializer.SerializeToElement(apiNode), DateTimeOffset.UtcNow), CancellationToken.None);
        await store.UpsertAsync(Record("browser-automation", "profiles", "browser-check",
            new BrowserAutomationProfile("browser-check", "Browser", "https://example.com", ["example.com"], false)), CancellationToken.None);
        using var workflowVariables = JsonDocument.Parse("""{"regions":["eu","apac"],"enabled":true}""");
        await store.UpsertAsync(Record("workflows", "profiles", "workflow-check", new AutomationWorkflowProfile(
            "workflow-check", "Workflow", [new AutomationWorkflowStep("step-one", "script-runner", "python-job", [])],
            new Dictionary<string, JsonElement>
            {
                ["regions"] = workflowVariables.RootElement.GetProperty("regions").Clone(),
                ["enabled"] = workflowVariables.RootElement.GetProperty("enabled").Clone(),
            })), CancellationToken.None);
        await store.UpsertAsync(Record("scheduler", "profiles", "schedule-check", new AutomationScheduleDefinition(
            "schedule-check", "Schedule", "workflow-check", true,
            new AutomationScheduleRecurrence(AutomationScheduleRecurrenceKind.Daily, LocalTime: new TimeOnly(9, 30)), false)), CancellationToken.None);

        await store.UpsertAsync(Record("scheduler", "occurrence-cursors", "schedule-check", new { lastOccurrenceUtc = DateTimeOffset.UtcNow }), CancellationToken.None);
        await store.UpsertAsync(Record("scheduler", "run-history", "run-one", new { output = "private" }), CancellationToken.None);
        await store.UpsertAsync(Record("focus-sessions", "settings", "current", new { duration = 25 }), CancellationToken.None);
        await store.UpsertAsync(Record("focus-sessions", "history", "past", new { started = true }), CancellationToken.None);

        try
        {
            var service = new AutomationLibraryTransferService(store);
            var payload = await service.ExportAsync(CancellationToken.None);
            var text = Encoding.UTF8.GetString(payload);
            using var document = JsonDocument.Parse(payload);
            var records = document.RootElement.GetProperty("records");
            Check.Equal(5, records.GetArrayLength());
            Check.False(text.Contains("must-not-export", StringComparison.Ordinal));
            Check.False(text.Contains("private", StringComparison.Ordinal));
            Check.False(text.Contains("focus-sessions", StringComparison.Ordinal));
            Check.True(text.Contains("opaque-ref", StringComparison.Ordinal));
            Check.False(text.Contains("secretValue", StringComparison.OrdinalIgnoreCase));
            Check.True(text.Contains("\"regions\":[\"eu\",\"apac\"]", StringComparison.Ordinal));

            var restored = new MemoryLibraryStore();
            await restored.UpsertAsync(Record("script-runner", "profiles", "python-job", new ScriptRunnerProfile(
                "python-job", "Old name", ScriptRunnerInterpreter.Python, interpreter, scriptPath, [], root,
                ScriptRunnerOutputMode.Text, 30)), CancellationToken.None);
            var importResult = await new AutomationLibraryTransferService(restored).ImportAsync(payload, CancellationToken.None);
            Check.Equal(5, importResult.ImportedCount);
            Check.Equal(1, (await restored.ListAsync("browser-automation", "profiles", CancellationToken.None)).Count);
            Check.Equal(1, (await restored.ListAsync("workflows", "profiles", CancellationToken.None)).Count);
            var restoredWorkflow = await restored.GetAsync("workflows", "profiles", "workflow-check", CancellationToken.None);
            Check.Equal(JsonValueKind.Array, restoredWorkflow!.Data.GetProperty("variables").GetProperty("regions").ValueKind);
            Check.Equal(true, restoredWorkflow.Data.GetProperty("variables").GetProperty("enabled").GetBoolean());
            Check.Equal(1, (await restored.ListAsync("scheduler", "profiles", CancellationToken.None)).Count);
            Check.Equal(0, (await restored.ListAsync("scheduler", "occurrence-cursors", CancellationToken.None)).Count);
            Check.Equal("Python job", (await restored.GetAsync("script-runner", "profiles", "python-job", CancellationToken.None))!
                .Data.GetProperty("name").GetString());
            Check.False((await restored.GetAsync("api", "profiles", "api-check", CancellationToken.None))!
                .Data.GetRawText().Contains("must-not-export", StringComparison.Ordinal));
            Check.Equal("status", (await restored.GetAsync("api", "profiles", "api-check", CancellationToken.None))!
                .Data.GetProperty("defaultInput").GetProperty("query").GetString());
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    public static async Task ImportsWithRepairWarnings()
    {
        var store = new MemoryLibraryStore();
        var missingRoot = Path.Combine(Path.GetTempPath(), "AutomatorMissing", Guid.NewGuid().ToString("N"));
        var profiles = new object[]
        {
            new ScriptRunnerProfile("python-missing", "Python", ScriptRunnerInterpreter.Python,
                Path.Combine(missingRoot, "python.exe"), Path.Combine(missingRoot, "job.py"), [], missingRoot, ScriptRunnerOutputMode.Text, 30),
            new ScriptRunnerProfile("bash-missing", "Bash", ScriptRunnerInterpreter.Bash,
                Path.Combine(missingRoot, "bash.exe"), Path.Combine(missingRoot, "job.sh"), [], missingRoot, ScriptRunnerOutputMode.Text, 30),
            new ScriptRunnerProfile("powershell-missing", "PowerShell", ScriptRunnerInterpreter.Powershell,
                Path.Combine(missingRoot, "pwsh.exe"), Path.Combine(missingRoot, "job.ps1"), [], missingRoot, ScriptRunnerOutputMode.Text, 30),
            new ApiProfile("api-missing", "API", "GET", "https://api.example", false,
                new Dictionary<string, string>(), new Dictionary<string, string> { ["Authorization"] = "missing-ref" }, null, ApiResponseMode.Text, 20),
        };
        var input = Envelope(profiles.Select(profile => profile switch
        {
            ScriptRunnerProfile script => PackageRecord("script-runner", "profiles", script.Id, script),
            ApiProfile api => PackageRecord("api", "profiles", api.Id, api),
            _ => throw new InvalidOperationException()
        }).ToArray());

        var service = new AutomationLibraryTransferService(store,
            static (_, _, _) => Task.FromResult(false));
        var result = await service.ImportAsync(input, CancellationToken.None);

        Check.Equal(4, result.ImportedCount);
        Check.True(result.Warnings.Any(warning => warning.Code == "missing-script-path" && warning.RecordId == "python-missing"));
        Check.True(result.Warnings.Any(warning => warning.Code == "missing-interpreter-path" && warning.RecordId == "bash-missing"));
        Check.True(result.Warnings.Any(warning => warning.Code == "missing-script-path" && warning.RecordId == "powershell-missing"));
        Check.True(result.Warnings.Any(warning => warning.Code == "missing-secret-reference" && warning.RecordId == "api-missing"));
        Check.True(result.Warnings.Count >= 7);
        var importedApi = await store.GetAsync("api", "profiles", "api-missing", CancellationToken.None);
        Check.True(importedApi is not null);
        Check.False(importedApi!.Data.GetRawText().Contains("missing-ref-value", StringComparison.Ordinal));
    }

    public static async Task RejectsInvalidEnvelopesWithoutPartialWrites()
    {
        var store = new MemoryLibraryStore();
        var service = new AutomationLibraryTransferService(store);
        var wrongVersion = Encoding.UTF8.GetBytes("{\"format\":\"automator-library\",\"version\":99,\"records\":[]}");
        await Check.ThrowsAsync<InvalidDataException>(() => service.ImportAsync(wrongVersion, CancellationToken.None));

        var duplicate = PackageRecord("api", "profiles", "duplicate", new ApiProfile("duplicate", "API", "GET",
            "https://api.example", false, new Dictionary<string, string>(),
            new Dictionary<string, string>(), null, ApiResponseMode.Text, 10));
        var payload = Envelope([duplicate, duplicate]);
        await Check.ThrowsAsync<InvalidDataException>(() => service.ImportAsync(payload, CancellationToken.None));
        Check.Equal(0, store.Records.Count);
    }

    public static async Task RejectsInvalidDefinitions()
    {
        var store = new MemoryLibraryStore();
        var service = new AutomationLibraryTransferService(store);
        var wrongId = PackageRecord("api", "profiles", "api-key", new ApiProfile("different-id", "API", "GET",
            "https://api.example", false, new Dictionary<string, string>(),
            new Dictionary<string, string>(), null, ApiResponseMode.Text, 10));
        await Check.ThrowsAsync<InvalidDataException>(() => service.ImportAsync(Envelope([wrongId]), CancellationToken.None));
        Check.Equal(0, store.Records.Count);

        var excluded = PackageRecord("focus-sessions", "history", "history-one", new { value = 1 });
        await Check.ThrowsAsync<InvalidDataException>(() => service.ImportAsync(Envelope([excluded]), CancellationToken.None));
        Check.Equal(0, store.Records.Count);

        var unsupportedSchema = PackageRecord("api", "profiles", "schema-two", new ApiProfile("schema-two", "API", "GET",
            "https://api.example", false, new Dictionary<string, string>(),
            new Dictionary<string, string>(), null, ApiResponseMode.Text, 10));
        var schemaNode = JsonNode.Parse(unsupportedSchema.GetRawText())!;
        schemaNode["schemaVersion"] = 2;
        using var schemaDocument = JsonDocument.Parse(schemaNode.ToJsonString());
        await Check.ThrowsAsync<InvalidDataException>(() => service.ImportAsync(Envelope([schemaDocument.RootElement.Clone()]), CancellationToken.None));
        Check.Equal(0, store.Records.Count);
    }

    public static async Task EnforcesBounds()
    {
        var store = new MemoryLibraryStore();
        var service = new AutomationLibraryTransferService(store);
        await Check.ThrowsAsync<InvalidDataException>(() => service.ImportAsync(new byte[AutomationLibraryTransferService.MaximumImportBytes + 1], CancellationToken.None));

        var records = Enumerable.Range(0, AutomationLibraryTransferService.MaximumImportRecords + 1)
            .Select(index => PackageRecord("api", "profiles", $"api-{index}", new ApiProfile($"api-{index}", "API", "GET",
                "https://api.example", false, new Dictionary<string, string>(),
                new Dictionary<string, string>(), null, ApiResponseMode.Text, 10))).ToArray();
        await Check.ThrowsAsync<InvalidDataException>(() => service.ImportAsync(Envelope(records), CancellationToken.None));
        Check.Equal(0, store.Records.Count);
    }

    private static AutomationLibraryRecord Record<T>(string module, string collection, string id, T value) =>
        new(module, collection, id, 1, JsonSerializer.SerializeToElement(value, JsonOptions), DateTimeOffset.UtcNow);

    private static JsonElement PackageRecord<T>(string module, string collection, string id, T value) =>
        JsonSerializer.SerializeToElement(new
        {
            moduleId = module,
            collection,
            id,
            schemaVersion = 1,
            data = JsonSerializer.SerializeToElement(value, JsonOptions)
        }, JsonOptions);

    private static byte[] Envelope(IReadOnlyList<JsonElement> records) => JsonSerializer.SerializeToUtf8Bytes(new
    {
        format = AutomationLibraryTransferService.Format,
        version = AutomationLibraryTransferService.CurrentFormatVersion,
        exportedUtc = DateTimeOffset.UtcNow,
        records
    }, JsonOptions);

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }

    private sealed class MemoryLibraryStore : IAutomationLibraryStore
    {
        private readonly Dictionary<(string Module, string Collection, string Id), AutomationLibraryRecord> _records = [];
        public IReadOnlyCollection<AutomationLibraryRecord> Records => _records.Values;

        public Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(string moduleId, string collection, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<AutomationLibraryRecord>>(_records.Values
                .Where(record => record.ModuleId == moduleId && record.Collection == collection).ToArray());
        }

        public Task<AutomationLibraryRecord?> GetAsync(string moduleId, string collection, string id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _records.TryGetValue((moduleId, collection, id), out var record);
            return Task.FromResult(record);
        }

        public Task UpsertAsync(AutomationLibraryRecord record, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _records[(record.ModuleId, record.Collection, record.Id)] = record;
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(string moduleId, string collection, string id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_records.Remove((moduleId, collection, id)));
        }
    }
}
