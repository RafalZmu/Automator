using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Automator.Application.Launcher;
using Automator.Core.Automation;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

/// <summary>Slot six is a control surface for the backend-lifetime scheduler.</summary>
public sealed class SchedulerModule : ILauncherTabModuleProvider
{
    public const string IdValue = "scheduler";
    private static readonly Regex IdPattern = new("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant);
    private static readonly AutomationCapabilityRequirement Capability = new(AutomationCapabilityIds.SchedulerManagement, 1);
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    public AutomationModuleDefinition Definition { get; } = new(6, IdValue, "Scheduler", "calendar", "scheduler", false,
        1, 1, [Capability], [new("getSnapshot", 1, [Capability]), new("saveSchedule", 1, [Capability]),
            new("deleteSchedule", 1, [Capability]), new("setEnabled", 1, [Capability]), new("runNow", 1, [Capability])]);
    public int ContractVersion => Definition.ContractVersion;
    public string Id => Definition.Id;
    public string Title => Definition.Title;
    public LauncherModuleState CreateInitialState() => new(Id, LauncherTabRegistry.Version,
        new Dictionary<string, string>(StringComparer.Ordinal) { ["status"] = "ready" });
    public JsonElement CreateDefaultSettings() => JsonSerializer.SerializeToElement(new { });
    public JsonElement MigrateSettings(int fromVersion, JsonElement value) =>
        throw new InvalidOperationException($"Scheduler settings version {fromVersion} cannot be migrated.");

    public async ValueTask<AutomationResult> ExecuteAsync(string actionId, JsonElement input, JsonElement moduleSettings,
        AutomationServicesContext services, CancellationToken cancellationToken)
    {
        try
        {
            var coordinator = services.Scheduler ?? throw new InvalidOperationException("Scheduler service is unavailable.");
            switch (actionId)
            {
                case "getSnapshot":
                    var snapshot = await coordinator.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
                    return Result(AutomationStatus.Success, "Scheduler refreshed.", new
                    {
                        schedules = snapshot.Schedules.Take(256), history = snapshot.History.TakeLast(100),
                        running = snapshot.Running, states = (snapshot.States ?? []).Take(256), workflows = (snapshot.Workflows ?? []).Take(256),
                        scripts = (snapshot.Scripts ?? []).Take(256)
                    });
                case "saveSchedule":
                    var schedule = JsonSerializer.Deserialize<AutomationScheduleDefinition>(input.GetRawText(), JsonOptions)
                        ?? throw new InvalidDataException("Schedule data is invalid.");
                    schedule = NormalizeAndValidate(schedule);
                    await coordinator.SaveAsync(schedule, cancellationToken).ConfigureAwait(false);
                    return Result(AutomationStatus.Success, "Schedule saved.", new { schedule });
                case "deleteSchedule":
                    var id = ReadId(input);
                    var deleted = await coordinator.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
                    return Result(deleted ? AutomationStatus.Success : AutomationStatus.Information,
                        deleted ? "Schedule removed." : "Schedule was already removed.", new { scheduleId = id, deleted });
                case "setEnabled":
                    var enabledId = ReadId(input);
                    if (!input.TryGetProperty("enabled", out var enabled) || enabled.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                        throw new InvalidDataException("Schedule enabled state must be a boolean.");
                    await coordinator.SetEnabledAsync(enabledId, enabled.GetBoolean(), cancellationToken).ConfigureAwait(false);
                    return Result(AutomationStatus.Success, enabled.GetBoolean() ? "Schedule enabled." : "Schedule paused.", new { scheduleId = enabledId, enabled = enabled.GetBoolean() });
                case "runNow":
                    var history = await coordinator.RunNowAsync(ReadId(input), cancellationToken).ConfigureAwait(false);
                    return Result(history.Status, history.Status == AutomationStatus.Error ? "Scheduled automation failed." : "Scheduled automation finished.", new { history });
                default: return Result(AutomationStatus.Error, "Unknown Scheduler action.", new { });
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (InvalidDataException exception) { return Result(AutomationStatus.Error, exception.Message, new { }); }
        catch (JsonException) { return Result(AutomationStatus.Error, "Schedule data is invalid.", new { }); }
        catch { return Result(AutomationStatus.Error, "The Scheduler action could not be completed.", new { }); }
    }

    public static AutomationScheduleDefinition NormalizeAndValidate(AutomationScheduleDefinition schedule)
    {
        if (schedule.Id is null || !IdPattern.IsMatch(schedule.Id) || !Enum.IsDefined(schedule.TargetKind))
            throw new InvalidDataException("The schedule key or target kind is invalid.");
        var profileId = schedule.ProfileId ?? schedule.WorkflowId;
        if (string.IsNullOrWhiteSpace(profileId) || !IdPattern.IsMatch(profileId))
            throw new InvalidDataException("Select a saved workflow or script profile with a short lowercase key.");
        var name = schedule.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 128 || name.Any(char.IsControl))
            throw new InvalidDataException("A schedule name of up to 128 characters is required.");
        var recurrence = schedule.Recurrence ?? throw new InvalidDataException("A recurrence is required.");
        switch (recurrence.Kind)
        {
            case AutomationScheduleRecurrenceKind.Interval:
                if (recurrence.IntervalMinutes is not (>= 1 and <= 525600) || recurrence.LocalTime is not null || recurrence.DaysOfWeek is not null)
                    throw new InvalidDataException("Interval schedules require 1 to 525600 minutes and no local-time or weekday fields.");
                recurrence = recurrence with { IntervalAnchorUtc = recurrence.IntervalAnchorUtc?.ToUniversalTime() };
                break;
            case AutomationScheduleRecurrenceKind.Daily:
                if (recurrence.LocalTime is null || recurrence.IntervalMinutes is not null || recurrence.IntervalAnchorUtc is not null || recurrence.DaysOfWeek is not null)
                    throw new InvalidDataException("Daily schedules require a local time and no interval or weekday fields.");
                break;
            case AutomationScheduleRecurrenceKind.Weekly:
                if (recurrence.LocalTime is null || recurrence.IntervalMinutes is not null || recurrence.IntervalAnchorUtc is not null
                    || recurrence.DaysOfWeek is not { Count: >= 1 and <= 7 } || recurrence.DaysOfWeek.Any(day => !Enum.IsDefined(day))
                    || recurrence.DaysOfWeek.Distinct().Count() != recurrence.DaysOfWeek.Count)
                    throw new InvalidDataException("Weekly schedules require a local time and one to seven unique weekdays.");
                break;
            default: throw new InvalidDataException("The schedule recurrence is unsupported.");
        }
        return schedule with
        {
            Name = name,
            Recurrence = recurrence,
            WorkflowId = schedule.TargetKind == AutomationScheduleTargetKind.Workflow ? profileId : schedule.WorkflowId,
            ProfileId = profileId,
        };
    }

    private static string ReadId(JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object || !input.TryGetProperty("scheduleId", out var id)
            || id.ValueKind != JsonValueKind.String || !IdPattern.IsMatch(id.GetString()!))
            throw new InvalidDataException("A valid schedule key is required.");
        return id.GetString()!;
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
