using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Automator.Application.Launcher;
using Automator.Core.Automation;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

/// <summary>Slot seven captures work intervals and turns stopped intervals into tagged log entries.</summary>
public sealed class FocusSessionsModule : ILauncherTabModuleProvider
{
    public const string IdValue = "focus-sessions";
    /// <summary>Shares the Work Time retention cap; the coordinator stores up to this many entries.</summary>
    public const int MaximumHistoryEntries = AutomationWorkTimeCoordinator.MaximumHistoryEntries;
    /// <summary>Newest history entries sent with each snapshot, and the largest page getHistory returns. Keeps results under the module data limit.</summary>
    public const int SnapshotHistoryPageSize = 250;

    private static readonly AutomationCapabilityRequirement WorkTimeCapability = new(AutomationCapabilityIds.WorkTimeManagement, 1);
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public AutomationModuleDefinition Definition { get; } = new(
        7,
        IdValue,
        "Work Time",
        "activity",
        "focus-sessions",
        false,
        2,
        1,
        [WorkTimeCapability],
        [
            new("getSnapshot", 2, [WorkTimeCapability]),
            new("start", 2, [WorkTimeCapability]),
            new("pause", 2, [WorkTimeCapability]),
            new("resume", 2, [WorkTimeCapability]),
            new("end", 2, [WorkTimeCapability]),
            new("saveEntry", 2, [WorkTimeCapability]),
            new("createEntry", 2, [WorkTimeCapability]),
            new("updateEntry", 2, [WorkTimeCapability]),
            new("deleteEntry", 2, [WorkTimeCapability]),
            new("discardTimer", 2, [WorkTimeCapability]),
            new("discardPending", 2, [WorkTimeCapability]),
            new("getHistory", 2, [WorkTimeCapability]),
        ]);

    public int ContractVersion => Definition.ContractVersion;
    public string Id => Definition.Id;
    public string Title => Definition.Title;

    public LauncherModuleState CreateInitialState() => new(
        Id,
        LauncherTabRegistry.Version,
        new Dictionary<string, string>(StringComparer.Ordinal) { ["status"] = "ready" });

    public JsonElement CreateDefaultSettings() => JsonSerializer.SerializeToElement(new { });

    public JsonElement MigrateSettings(int fromVersion, JsonElement value) =>
        throw new InvalidOperationException($"Work Time settings version {fromVersion} cannot be migrated.");

    /// <summary>Kept for decoding and exercising the legacy Pomodoro records; slot seven no longer exposes them.</summary>
    public static void ValidateSettings(AutomationFocusSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.FocusMinutes is < 1 or > 240)
            throw new InvalidDataException("Focus duration must be between 1 and 240 minutes.");
        if (settings.BreakMinutes is < 1 or > 120)
            throw new InvalidDataException("Break duration must be between 1 and 120 minutes.");
    }

    public async ValueTask<AutomationResult> ExecuteAsync(
        string actionId,
        JsonElement input,
        JsonElement moduleSettings,
        AutomationServicesContext services,
        CancellationToken cancellationToken)
    {
        try
        {
            var coordinator = services.WorkTime
                ?? throw new InvalidOperationException("Work Time service is unavailable.");

            return actionId switch
            {
                "getSnapshot" => SnapshotResult(await coordinator.GetSnapshotAsync(cancellationToken).ConfigureAwait(false), "Work-time log refreshed."),
                "start" => SnapshotResult(await coordinator.StartAsync(ReadDescription(input), cancellationToken).ConfigureAwait(false), "Work interval started."),
                "pause" => SnapshotResult(await coordinator.PauseAsync(ReadEntryId(input), cancellationToken).ConfigureAwait(false), "Work timer paused."),
                "resume" => SnapshotResult(await coordinator.ResumeAsync(ReadEntryId(input), cancellationToken).ConfigureAwait(false), "Work timer resumed."),
                "end" => SnapshotResult(await coordinator.EndAsync(ReadEntryId(input), cancellationToken).ConfigureAwait(false), "Work timer ended. Review and save the entry."),
                "saveEntry" => await SaveEntryAsync(coordinator, input, cancellationToken).ConfigureAwait(false),
                "createEntry" => await CreateEntryAsync(coordinator, input, cancellationToken).ConfigureAwait(false),
                "updateEntry" => await UpdateEntryAsync(coordinator, input, cancellationToken).ConfigureAwait(false),
                "deleteEntry" => await DeleteEntryAsync(coordinator, input, cancellationToken).ConfigureAwait(false),
                "discardTimer" => SnapshotResult(await coordinator.DiscardTimerAsync(ReadEntryId(input), cancellationToken).ConfigureAwait(false), "Paused timer discarded."),
                "discardPending" => SnapshotResult(await coordinator.DiscardPendingAsync(ReadEntryId(input), cancellationToken).ConfigureAwait(false), "Pending interval discarded."),
                "getHistory" => await GetHistoryAsync(coordinator, input, cancellationToken).ConfigureAwait(false),
                _ => Result(AutomationStatus.Error, "Unknown Work Time action.", new { }),
            };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException
            or ArgumentException or JsonException)
        {
            return Result(AutomationStatus.Error, exception.Message, new { });
        }
        catch
        {
            return Result(AutomationStatus.Error, "The Work Time action could not be completed.", new { });
        }
    }

    private static async ValueTask<AutomationResult> SaveEntryAsync(
        IAutomationWorkTimeCoordinator coordinator,
        JsonElement input,
        CancellationToken cancellationToken)
    {
        var (description, tags) = ReadEntryInput(input);
        var snapshot = await coordinator.SaveEntryAsync(ReadEntryId(input), description, tags, cancellationToken).ConfigureAwait(false);
        return SnapshotResult(snapshot, "Work-time entry saved.");
    }

    private static async ValueTask<AutomationResult> UpdateEntryAsync(
        IAutomationWorkTimeCoordinator coordinator,
        JsonElement input,
        CancellationToken cancellationToken)
    {
        var id = ReadEntryId(input);
        var (startedUtc, stoppedUtc) = ReadIntervalInput(input);
        var (description, tags) = ReadEntryInput(input);
        var snapshot = await coordinator.UpdateEntryAsync(id, startedUtc, stoppedUtc, description, tags, cancellationToken).ConfigureAwait(false);
        return SnapshotResult(snapshot, "Work-time entry updated.");
    }

    private static async ValueTask<AutomationResult> CreateEntryAsync(
        IAutomationWorkTimeCoordinator coordinator,
        JsonElement input,
        CancellationToken cancellationToken)
    {
        var (startedUtc, stoppedUtc) = ReadIntervalInput(input);
        var (description, tags) = ReadEntryInput(input);
        var snapshot = await coordinator.CreateEntryAsync(startedUtc, stoppedUtc, description, tags, cancellationToken).ConfigureAwait(false);
        return SnapshotResult(snapshot, "Work-time entry added.");
    }

    private static (DateTimeOffset StartedUtc, DateTimeOffset StoppedUtc) ReadIntervalInput(JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Work-time entry input must be an object.");
        return (ReadTimestamp(input, "startedUtc"), ReadTimestamp(input, "stoppedUtc"));
    }

    private static DateTimeOffset ReadTimestamp(JsonElement input, string name)
    {
        if (!input.TryGetProperty(name, out var value)
            || value.ValueKind != JsonValueKind.String
            || !DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp))
            throw new InvalidDataException("Enter a valid start and end time for the work entry.");
        return timestamp.ToUniversalTime();
    }

    private static async ValueTask<AutomationResult> DeleteEntryAsync(
        IAutomationWorkTimeCoordinator coordinator,
        JsonElement input,
        CancellationToken cancellationToken)
    {
        var id = ReadEntryId(input);
        var snapshot = await coordinator.DeleteEntryAsync(id, cancellationToken).ConfigureAwait(false);
        return SnapshotResult(snapshot, "Work-time entry deleted.");
    }

    private static string ReadEntryId(JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object
            || !input.TryGetProperty("id", out var idValue)
            || idValue.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("A work entry id is required.");
        var id = idValue.GetString()?.Trim() ?? string.Empty;
        if (id.Length is 0 or > 128) throw new InvalidDataException("The work entry id is invalid.");
        return id;
    }

    private static string ReadDescription(JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Work-time entry input must be an object.");
        if (!input.TryGetProperty("description", out var descriptionValue) || descriptionValue.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("A description is required before saving the work entry.");
        var description = descriptionValue.GetString()?.Trim() ?? string.Empty;
        if (description.Length == 0) throw new InvalidDataException("A description is required before saving the work entry.");
        if (description.Length > 500) throw new InvalidDataException("A work description can contain at most 500 characters.");

        return description;
    }

    private static (string Description, IReadOnlyList<string> Tags) ReadEntryInput(JsonElement input)
    {
        var description = ReadDescription(input);

        var tags = new List<string>();
        if (input.TryGetProperty("tags", out var tagsValue))
        {
            if (tagsValue.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("Work-time tags must be a list of text values.");
            foreach (var tagValue in tagsValue.EnumerateArray())
            {
                if (tagValue.ValueKind != JsonValueKind.String)
                    throw new InvalidDataException("Work-time tags must be a list of text values.");
                var tag = tagValue.GetString()?.Trim() ?? string.Empty;
                if (tag.Length == 0) continue;
                if (tag.Length > 40) throw new InvalidDataException("A work tag can contain at most 40 characters.");
                tags.Add(tag);
                if (tags.Count > 30) throw new InvalidDataException("A work entry can contain at most 30 tags.");
            }
        }
        return (description, tags);
    }

    private static AutomationResult SnapshotResult(AutomationWorkTimeSnapshot snapshot, string message) =>
        Result(AutomationStatus.Success, message, new
        {
            timers = snapshot.Timers,
            pendingEntries = snapshot.PendingEntries,
            history = snapshot.History.TakeLast(SnapshotHistoryPageSize).ToArray(),
            historyTotal = snapshot.History.Count,
        });

    private static async ValueTask<AutomationResult> GetHistoryAsync(
        IAutomationWorkTimeCoordinator coordinator,
        JsonElement input,
        CancellationToken cancellationToken)
    {
        var (offset, limit) = ReadHistoryPageInput(input);
        var page = await coordinator.GetHistoryPageAsync(offset, limit, cancellationToken).ConfigureAwait(false);
        return Result(AutomationStatus.Success, "Older work entries loaded.", new { entries = page.Entries, total = page.Total, offset });
    }

    private static (int Offset, int Limit) ReadHistoryPageInput(JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("History page input must be an object.");
        if (!input.TryGetProperty("offset", out var offsetValue) || offsetValue.ValueKind != JsonValueKind.Number
            || !offsetValue.TryGetInt32(out var offset) || offset < 0)
            throw new InvalidDataException("History page offset must be a non-negative whole number.");
        if (!input.TryGetProperty("limit", out var limitValue) || limitValue.ValueKind != JsonValueKind.Number
            || !limitValue.TryGetInt32(out var limit) || limit is < 1 or > SnapshotHistoryPageSize)
            throw new InvalidDataException($"History page limit must be a whole number from 1 to {SnapshotHistoryPageSize}.");
        return (offset, limit);
    }

    private static AutomationResult Result(AutomationStatus status, string message, object data) =>
        new(AutomationTabContract.CurrentVersion, status, message, JsonSerializer.SerializeToElement(data, JsonOptions), []);

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
