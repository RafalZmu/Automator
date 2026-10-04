using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Automator.Application.Automation;

namespace Automator.Infrastructure.Automation;

public sealed record AutomationLibraryImportWarning(
    string Code,
    string ModuleId,
    string RecordId,
    string Field,
    string Message);

public sealed record AutomationLibraryImportResult(
    int ImportedCount,
    IReadOnlyList<AutomationLibraryImportWarning> Warnings);

/// <summary>
/// Transfers only portable profile definitions. The allowlist deliberately excludes run/session
/// history, focus state, schedule cursors/claims, and all browser runtime storage.
/// </summary>
public sealed class AutomationLibraryTransferService
{
    public const string Format = "automator-library";
    public const int CurrentFormatVersion = 1;
    public const int MaximumImportBytes = 16 * 1024 * 1024;
    public const int MaximumImportRecords = 2_000;
    private const int MaximumRecordBytes = 1024 * 1024;

    private static readonly Regex StableProfileId = new("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private static readonly (string ModuleId, string Collection)[] PortableCollections =
    [
        (ScriptRunnerModule.IdValue, ScriptRunnerModule.ProfileCollection),
        (ApiModule.IdValue, ApiModule.ProfileCollection),
        (BrowserAutomationModule.IdValue, BrowserAutomationModule.ProfileCollection),
        (WorkflowModule.IdValue, WorkflowModule.WorkflowCollection),
        (AutomationSchedulerCoordinator.ModuleId, AutomationSchedulerCoordinator.ScheduleCollection),
        (AutomationVariableService.ModuleId, AutomationVariableService.Collection),
    ];

    private readonly IAutomationLibraryStore _store;
    private readonly Func<string, string, CancellationToken, Task<bool>>? _secretExists;
    private readonly TimeProvider _clock;

    public AutomationLibraryTransferService(
        IAutomationLibraryStore store,
        Func<string, string, CancellationToken, Task<bool>>? secretExists = null,
        TimeProvider? clock = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _secretExists = secretExists;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<byte[]> ExportAsync(CancellationToken cancellationToken)
    {
        var portable = new List<PortableRecord>();
        foreach (var (moduleId, collection) in PortableCollections)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var records = await _store.ListAsync(moduleId, collection, cancellationToken).ConfigureAwait(false);
            foreach (var record in records)
            {
                var safe = NormalizeRecord(record);
                if (safe is not null) portable.Add(safe);
                if (portable.Count > MaximumImportRecords)
                    throw new InvalidDataException("The automation library contains too many portable records to export.");
            }
        }

        var payload = new ExportEnvelope(Format, CurrentFormatVersion, _clock.GetUtcNow(), portable);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        if (bytes.Length > MaximumImportBytes)
            throw new InvalidDataException("The automation library is too large to export.");
        return bytes;
    }

    public async Task<AutomationLibraryImportResult> ImportAsync(
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (payload.Length is 0 or > MaximumImportBytes)
            throw new InvalidDataException("The automation library import is empty or exceeds the 16 MiB limit.");

        List<PortableRecord> records;
        try
        {
            using var document = JsonDocument.Parse(payload, new JsonDocumentOptions { MaxDepth = 64 });
            records = ParseEnvelope(document.RootElement);
        }
        catch (JsonException)
        {
            throw new InvalidDataException("The automation library import is not valid JSON or exceeds the nesting limit.");
        }

        var normalized = new List<PortableRecord>(records.Count);
        var uniqueKeys = new HashSet<(string ModuleId, string Collection, string Id)>();
        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!uniqueKeys.Add((record.ModuleId, record.Collection, record.Id)))
                throw new InvalidDataException("The automation library import contains duplicate records.");
            var safe = NormalizeRecord(record.ToLibraryRecord(_clock.GetUtcNow()));
            if (safe is null)
                throw new InvalidDataException("The automation library import contains an unsupported record or schema.");
            normalized.Add(safe);
        }

        var warnings = new List<AutomationLibraryImportWarning>();
        // Imported variables may add names or repeat equal values, but never overwrite a different value.
        var incomingVariables = normalized.SingleOrDefault(record => record.ModuleId == AutomationVariableService.ModuleId);
        if (incomingVariables is not null)
        {
            var existing = await _store.GetAsync(AutomationVariableService.ModuleId, AutomationVariableService.Collection,
                AutomationVariableService.RecordId, cancellationToken).ConfigureAwait(false);
            var merged = existing?.Data.Deserialize<Dictionary<string, JsonElement>>() ?? new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var (name, value) in incomingVariables.Data.Deserialize<Dictionary<string, JsonElement>>()!)
            {
                if (merged.TryGetValue(name, out var prior) && !System.Text.Json.Nodes.JsonNode.DeepEquals(
                    System.Text.Json.Nodes.JsonNode.Parse(prior.GetRawText()), System.Text.Json.Nodes.JsonNode.Parse(value.GetRawText())))
                    throw new InvalidDataException("Imported global variables conflict with existing values. Rename or remove the conflicting variable in Options first.");
                merged[name] = value;
            }
            AutomationVariableService.Validate(merged);
            var index = normalized.IndexOf(incomingVariables);
            normalized[index] = incomingVariables with { Data = JsonSerializer.SerializeToElement(merged) };
        }
        foreach (var record in normalized)
            await CollectRepairWarningsAsync(record, warnings, cancellationToken).ConfigureAwait(false);

        // Validate the complete document and resolve repair diagnostics before the first write.
        foreach (var record in normalized)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _store.UpsertAsync(record.ToLibraryRecord(_clock.GetUtcNow()), cancellationToken).ConfigureAwait(false);
        }

        return new AutomationLibraryImportResult(normalized.Count, Array.AsReadOnly(warnings.ToArray()));
    }

    private static List<PortableRecord> ParseEnvelope(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("format", out var format) || format.ValueKind != JsonValueKind.String
            || format.GetString() != Format
            || !root.TryGetProperty("version", out var version) || version.ValueKind != JsonValueKind.Number
            || !version.TryGetInt32(out var formatVersion) || formatVersion != CurrentFormatVersion
            || !root.TryGetProperty("records", out var records) || records.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("The automation library format or version is unsupported.");

        var result = new List<PortableRecord>();
        foreach (var item in records.EnumerateArray())
        {
            if (result.Count >= MaximumImportRecords)
                throw new InvalidDataException("The automation library import contains too many records.");
            if (item.ValueKind != JsonValueKind.Object
                || !TryReadString(item, "moduleId", out var moduleId)
                || !TryReadString(item, "collection", out var collection)
                || !TryReadString(item, "id", out var id)
                || !item.TryGetProperty("schemaVersion", out var schema) || schema.ValueKind != JsonValueKind.Number
                || !schema.TryGetInt32(out var schemaVersion)
                || !item.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("The automation library import contains an invalid record.");

            if (Encoding.UTF8.GetByteCount(data.GetRawText()) > MaximumRecordBytes)
                throw new InvalidDataException("An automation library record exceeds the 1 MiB limit.");
            result.Add(new PortableRecord(moduleId, collection, id, schemaVersion, data.Clone(), DateTimeOffset.MinValue));
        }
        return result;
    }

    private static bool TryReadString(JsonElement element, string property, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(property, out var propertyValue) || propertyValue.ValueKind != JsonValueKind.String)
            return false;
        value = propertyValue.GetString() ?? string.Empty;
        return value.Length > 0;
    }

    private static PortableRecord? NormalizeRecord(AutomationLibraryRecord record)
    {
        if (!PortableCollections.Contains((record.ModuleId, record.Collection))
            || record.SchemaVersion != 1
            || !StableProfileId.IsMatch(record.Id)
            || record.Data.ValueKind != JsonValueKind.Object
            || Encoding.UTF8.GetByteCount(record.Data.GetRawText()) > MaximumRecordBytes)
            return null;

        try
        {
            object definition;
            switch (record.ModuleId)
            {
                case "script-runner":
                    var script = record.Data.Deserialize<ScriptRunnerProfile>(JsonOptions);
                    if (script is null || script.Id != record.Id) return null;
                    ScriptRunnerModule.Validate(script);
                    definition = script;
                    break;
                case "api":
                    var api = ApiModule.TryReadProfile(record);
                    if (api is null) return null;
                    definition = api;
                    break;
                case "browser-automation":
                    var browser = BrowserAutomationModule.TryReadProfile(record);
                    if (browser is null) return null;
                    definition = browser;
                    break;
                case "workflows":
                    var workflow = record.Data.Deserialize<AutomationWorkflowProfile>(JsonOptions);
                    if (workflow is null || workflow.Id != record.Id
                        || WorkflowModule.ValidateProfile(workflow) is not null) return null;
                    definition = workflow;
                    break;
                case "scheduler":
                    var schedule = record.Data.Deserialize<AutomationScheduleDefinition>(JsonOptions);
                    if (schedule is null || schedule.Id != record.Id) return null;
                    definition = SchedulerModule.NormalizeAndValidate(schedule);
                    break;
                case AutomationVariableService.ModuleId:
                    if (record.Id != AutomationVariableService.RecordId) return null;
                    var variables = record.Data.Deserialize<Dictionary<string, JsonElement>>();
                    if (variables is null) return null;
                    AutomationVariableService.Validate(variables);
                    definition = variables;
                    break;
                default:
                    return null;
            }

            var data = JsonSerializer.SerializeToElement(definition, JsonOptions);
            if (Encoding.UTF8.GetByteCount(data.GetRawText()) > MaximumRecordBytes) return null;
            return new PortableRecord(record.ModuleId, record.Collection, record.Id, 1, data, record.UpdatedUtc);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or ArgumentException or InvalidOperationException or NullReferenceException)
        {
            return null;
        }
    }

    private async Task CollectRepairWarningsAsync(
        PortableRecord record,
        List<AutomationLibraryImportWarning> warnings,
        CancellationToken cancellationToken)
    {
        if (record.ModuleId == ScriptRunnerModule.IdValue)
        {
            var profile = record.Data.Deserialize<ScriptRunnerProfile>(JsonOptions)!;
            if (!File.Exists(profile.ScriptPath))
                warnings.Add(new("missing-script-path", record.ModuleId, record.Id, "scriptPath", "The script file is missing and needs to be relinked."));
            if (!File.Exists(profile.InterpreterPath))
                warnings.Add(new("missing-interpreter-path", record.ModuleId, record.Id, "interpreterPath", "The interpreter executable is missing and needs to be relinked."));
            if (!Directory.Exists(profile.WorkingDirectory))
                warnings.Add(new("missing-working-directory", record.ModuleId, record.Id, "workingDirectory", "The working directory is missing and needs to be relinked."));
        }
        else if (record.ModuleId == ApiModule.IdValue && _secretExists is not null)
        {
            var profile = record.Data.Deserialize<ApiProfile>(JsonOptions)!;
            foreach (var (headerName, secretId) in profile.SecretHeaders)
            {
                if (!await _secretExists(profile.Id, secretId, cancellationToken).ConfigureAwait(false))
                    warnings.Add(new("missing-secret-reference", record.ModuleId, record.Id,
                        $"secretHeaders.{headerName}", "The referenced credential is unavailable and needs to be configured."));
            }
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }

    private sealed record ExportEnvelope(string Format, int Version, DateTimeOffset ExportedUtc, IReadOnlyList<PortableRecord> Records);
    private sealed record PortableRecord(
        string ModuleId,
        string Collection,
        string Id,
        int SchemaVersion,
        JsonElement Data,
        DateTimeOffset UpdatedUtc)
    {
        public AutomationLibraryRecord ToLibraryRecord(DateTimeOffset updatedUtc) =>
            new(ModuleId, Collection, Id, SchemaVersion, Data.Clone(), updatedUtc);
    }
}
