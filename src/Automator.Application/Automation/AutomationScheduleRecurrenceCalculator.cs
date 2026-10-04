namespace Automator.Application.Automation;

/// <summary>Pure local-time recurrence math shared by the host scheduler and its specifications.</summary>
public static class AutomationScheduleRecurrenceCalculator
{
    public static DateTimeOffset NextOccurrenceUtc(
        AutomationScheduleDefinition schedule,
        DateTimeOffset afterUtc,
        TimeZoneInfo? localTimeZone = null)
    {
        schedule = SchedulerModule.NormalizeAndValidate(schedule);
        var zone = localTimeZone ?? TimeZoneInfo.Local;
        afterUtc = afterUtc.ToUniversalTime();
        var recurrence = schedule.Recurrence;

        if (recurrence.Kind == AutomationScheduleRecurrenceKind.Interval)
        {
            var interval = TimeSpan.FromMinutes(recurrence.IntervalMinutes!.Value);
            var anchor = recurrence.IntervalAnchorUtc?.ToUniversalTime() ?? afterUtc;
            if (anchor > afterUtc) return anchor;
            var elapsedTicks = (afterUtc - anchor).Ticks;
            var periods = elapsedTicks / interval.Ticks + 1;
            return anchor.AddTicks(checked(periods * interval.Ticks));
        }

        var localNow = TimeZoneInfo.ConvertTime(afterUtc, zone).DateTime;
        var daysToScan = recurrence.Kind == AutomationScheduleRecurrenceKind.Daily ? 2 : 8;
        for (var offset = 0; offset < daysToScan; offset++)
        {
            var date = DateOnly.FromDateTime(localNow).AddDays(offset);
            if (recurrence.Kind == AutomationScheduleRecurrenceKind.Weekly
                && !recurrence.DaysOfWeek!.Contains(date.DayOfWeek)) continue;

            var localCandidate = date.ToDateTime(recurrence.LocalTime!.Value, DateTimeKind.Unspecified);
            var candidate = ResolveLocalTime(localCandidate, zone);
            if (candidate > afterUtc) return candidate;
        }

        throw new InvalidOperationException("The recurrence did not produce a future occurrence.");
    }

    public static DateTimeOffset? LatestOccurrenceUtc(
        AutomationScheduleDefinition schedule,
        DateTimeOffset atOrBeforeUtc,
        TimeZoneInfo? localTimeZone = null)
    {
        schedule = SchedulerModule.NormalizeAndValidate(schedule);
        var zone = localTimeZone ?? TimeZoneInfo.Local;
        atOrBeforeUtc = atOrBeforeUtc.ToUniversalTime();
        var recurrence = schedule.Recurrence;

        if (recurrence.Kind == AutomationScheduleRecurrenceKind.Interval)
        {
            var anchor = recurrence.IntervalAnchorUtc?.ToUniversalTime() ?? atOrBeforeUtc;
            if (anchor > atOrBeforeUtc) return null;
            var intervalTicks = TimeSpan.FromMinutes(recurrence.IntervalMinutes!.Value).Ticks;
            var periods = (atOrBeforeUtc - anchor).Ticks / intervalTicks;
            return anchor.AddTicks(checked(periods * intervalTicks));
        }

        var localNow = TimeZoneInfo.ConvertTime(atOrBeforeUtc, zone).DateTime;
        var daysToScan = recurrence.Kind == AutomationScheduleRecurrenceKind.Daily ? 2 : 8;
        for (var offset = 0; offset < daysToScan; offset++)
        {
            var date = DateOnly.FromDateTime(localNow).AddDays(-offset);
            if (recurrence.Kind == AutomationScheduleRecurrenceKind.Weekly
                && !recurrence.DaysOfWeek!.Contains(date.DayOfWeek)) continue;

            var candidate = ResolveLocalTime(date.ToDateTime(recurrence.LocalTime!.Value, DateTimeKind.Unspecified), zone);
            if (candidate <= atOrBeforeUtc) return candidate;
        }

        return null;
    }

    private static DateTimeOffset ResolveLocalTime(DateTime localTime, TimeZoneInfo zone)
    {
        // Move a nonexistent spring-forward wall time to the first real minute on that date.
        var candidate = localTime;
        if (zone.IsInvalidTime(candidate))
            candidate = new DateTime(candidate.Year, candidate.Month, candidate.Day, candidate.Hour, candidate.Minute, 0, DateTimeKind.Unspecified);
        for (var minute = 0; zone.IsInvalidTime(candidate); minute++)
        {
            if (minute >= 180 || candidate.Date != localTime.Date)
                throw new InvalidOperationException("The local time falls in an unsupported daylight-saving gap.");
            candidate = candidate.AddMinutes(1);
        }

        if (zone.IsAmbiguousTime(candidate))
        {
            // The larger UTC offset represents the first instant in the repeated local-time window.
            var offset = zone.GetAmbiguousTimeOffsets(candidate).Max();
            return new DateTimeOffset(candidate, offset).ToUniversalTime();
        }

        return TimeZoneInfo.ConvertTimeToUtc(candidate, zone);
    }
}
