using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

public sealed record AutomationRunActivityEntry(
    string Id, string ModuleId, string ActionId, string? ProfileId,
    DateTimeOffset StartedUtc, DateTimeOffset FinishedUtc, AutomationStatus Status,
    long DurationMilliseconds, AutomationExecutionOrigin Origin);

public sealed record AutomationRunActivitySnapshot(int ContractVersion, IReadOnlyList<AutomationRunActivityEntry> Entries);

/// <summary>Host-owned metadata index. It cannot accept provider result text or structured output.</summary>
public sealed class AutomationRunActivityService(IAutomationLibraryStore library)
{
    public const string ModuleId = "host-activity";
    public const string Collection = "runs";
    public const int MaximumEntries = 300;
    private static readonly Regex MetadataId = new("^[a-zA-Z0-9._:-]{1,128}$", RegexOptions.CultureInvariant);
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public static bool IsObservedAction(string moduleId, string actionId) => (moduleId, actionId) switch
    {
        ("script-runner", "runProfile" or "runAgain") or ("api", "runProfile") => true,
        ("browser-automation", "runTests" or "runAction" or "repeatAction") => true,
        _ => false,
    };

    public static string? ReadProfileId(JsonElement input)
    {
        if (input.ValueKind == JsonValueKind.Object && input.TryGetProperty("id", out var id)
            && id.ValueKind == JsonValueKind.String && id.GetString() is { } value && MetadataId.IsMatch(value)) return value;
        return null;
    }

    public async Task RecordAsync(AutomationRunActivityEntry entry, CancellationToken cancellationToken)
    {
        if (!IsObservedAction(entry.ModuleId, entry.ActionId) || !Valid(entry))
            throw new ArgumentException("The run activity metadata is invalid.", nameof(entry));
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await library.UpsertAsync(new AutomationLibraryRecord(ModuleId, Collection, entry.Id, 1,
                JsonSerializer.SerializeToElement(entry, JsonOptions), entry.FinishedUtc), cancellationToken).ConfigureAwait(false);
            var records = await library.ListAsync(ModuleId, Collection, cancellationToken).ConfigureAwait(false);
            foreach (var old in records.OrderByDescending(record => record.UpdatedUtc).Skip(MaximumEntries))
                await library.DeleteAsync(ModuleId, Collection, old.Id, cancellationToken).ConfigureAwait(false);
        }
        finally { _writeGate.Release(); }
    }

    public async Task<AutomationRunActivitySnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var entries = new List<AutomationRunActivityEntry>();
        foreach (var record in await library.ListAsync(ModuleId, Collection, cancellationToken).ConfigureAwait(false))
        {
            if (record.SchemaVersion != 1) continue;
            try
            {
                var entry = record.Data.Deserialize<AutomationRunActivityEntry>(JsonOptions);
                if (entry is not null && entry.Id == record.Id && IsObservedAction(entry.ModuleId, entry.ActionId) && Valid(entry)) entries.Add(entry);
            }
            catch (JsonException) { }
        }
        // Workflows and schedules already retain safe metadata. Project that history instead of storing a second copy.
        foreach (var record in await library.ListAsync("workflows", "run-history", cancellationToken).ConfigureAwait(false))
        {
            try
            {
                var data = record.Data;
                if (record.SchemaVersion != 1 || !data.TryGetProperty("summary", out var summary)) continue;
                var origin = ReadOrigin(data.GetProperty("origin"));
                // Scheduled workflows are represented by their scheduler occurrence, avoiding duplicate top-level rows.
                if (origin == AutomationExecutionOrigin.Scheduled) continue;
                var entry = new AutomationRunActivityEntry($"workflow:{record.Id}", "workflows", "runWorkflow",
                    data.GetProperty("workflowId").GetString(), data.GetProperty("startedUtc").GetDateTimeOffset(),
                    data.GetProperty("finishedUtc").GetDateTimeOffset(), ReadStatus(Property(summary, "status", "Status")),
                    Property(summary, "durationMilliseconds", "DurationMilliseconds").GetInt64(), AutomationExecutionOrigin.Manual);
                if (Valid(entry)) entries.Add(entry);
            }
            catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or ArgumentException or OverflowException) { }
        }
        foreach (var record in await library.ListAsync("scheduler", "run-history", cancellationToken).ConfigureAwait(false))
        {
            try
            {
                if (record.SchemaVersion != 1) continue;
                var history = record.Data.Deserialize<AutomationScheduleHistoryEntry>(JsonOptions);
                if (history?.FinishedUtc is not { } finished || history.Id is null || history.ScheduleId is null) continue;
                var entry = new AutomationRunActivityEntry($"schedule:{record.Id}", "scheduler", "runSchedule", history.ScheduleId,
                    history.StartedUtc, finished, history.Status, history.DurationMilliseconds,
                    history.Id.StartsWith("manual-", StringComparison.Ordinal) ? AutomationExecutionOrigin.Manual : AutomationExecutionOrigin.Scheduled);
                if (Valid(entry)) entries.Add(entry);
            }
            catch (JsonException) { }
        }
        return new(1, entries.OrderByDescending(entry => entry.StartedUtc).ThenBy(entry => entry.Id, StringComparer.Ordinal)
            .Take(MaximumEntries).ToArray());
    }

    private static bool Valid(AutomationRunActivityEntry entry) => MetadataId.IsMatch(entry.Id ?? "")
        && entry.ModuleId is "script-runner" or "api" or "browser-automation" or "workflows" or "scheduler"
        && MetadataId.IsMatch(entry.ActionId ?? "") && (entry.ProfileId is null || MetadataId.IsMatch(entry.ProfileId))
        && Enum.IsDefined(entry.Status) && entry.Origin is AutomationExecutionOrigin.Manual or AutomationExecutionOrigin.Scheduled
        && entry.FinishedUtc >= entry.StartedUtc && entry.DurationMilliseconds is >= 0 and <= 9007199254740991;

    private static AutomationStatus ReadStatus(JsonElement value) => value.ValueKind == JsonValueKind.Number
        ? (AutomationStatus)value.GetInt32() : Enum.Parse<AutomationStatus>(value.GetString()!, true);
    private static JsonElement Property(JsonElement value, string camelCase, string pascalCase) =>
        value.TryGetProperty(camelCase, out var property) ? property : value.GetProperty(pascalCase);
    private static AutomationExecutionOrigin ReadOrigin(JsonElement value) => value.ValueKind == JsonValueKind.Number
        ? (AutomationExecutionOrigin)value.GetInt32() : Enum.Parse<AutomationExecutionOrigin>(value.GetString()!, true);
    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
