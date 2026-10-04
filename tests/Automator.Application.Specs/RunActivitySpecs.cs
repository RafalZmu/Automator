using System.Text.Json;
using Automator.Application.Automation;
using Automator.Core.Plugins;

internal static class RunActivitySpecs
{
    public static async Task RunAsync()
    {
        var library = new MemoryLibrary();
        var service = new AutomationRunActivityService(library);
        var start = DateTimeOffset.Parse("2026-10-04T12:00:00Z");
        for (var index = 0; index < 301; index++)
            await service.RecordAsync(new($"run-{index}", "script-runner", "runProfile", "report", start.AddSeconds(index),
                start.AddSeconds(index + 1), AutomationStatus.Success, 1000, AutomationExecutionOrigin.Manual), CancellationToken.None);
        var stored = await library.ListAsync(AutomationRunActivityService.ModuleId, AutomationRunActivityService.Collection, CancellationToken.None);
        if (stored.Count != 300 || stored.Any(record => record.Id == "run-0")) throw new Exception("Activity must prune old entries.");
        foreach (var record in stored)
        {
            var keys = record.Data.EnumerateObject().Select(property => property.Name).ToHashSet();
            if (!keys.SetEquals(["id", "moduleId", "actionId", "profileId", "startedUtc", "finishedUtc", "status", "durationMilliseconds", "origin"]))
                throw new Exception("Activity storage must contain only metadata fields.");
        }
        await library.UpsertAsync(new("workflows", "run-history", "workflow-run", 1, JsonSerializer.SerializeToElement(new {
            workflowId = "report-workflow", origin = 0, correlationId = "workflow-run", startedUtc = start.AddMinutes(10), finishedUtc = start.AddMinutes(11),
            summary = new { Status = 0, DurationMilliseconds = 60000, output = "private content" }, steps = new[] { new { output = "private content" } }
        }), start.AddMinutes(11)), CancellationToken.None);
        await library.UpsertAsync(new("workflows", "run-history", "scheduled-workflow", 1, JsonSerializer.SerializeToElement(new {
            workflowId = "report-workflow", origin = 2, startedUtc = start.AddMinutes(12), finishedUtc = start.AddMinutes(13), summary = new { status = 0, durationMilliseconds = 60000 }
        }), start.AddMinutes(13)), CancellationToken.None);
        await library.UpsertAsync(new("scheduler", "run-history", "schedule-run", 1,
            JsonSerializer.SerializeToElement(new AutomationScheduleHistoryEntry("schedule-run", "daily", start.AddMinutes(12), start.AddMinutes(13), AutomationStatus.Success, "completed", 60000)), start.AddMinutes(13)), CancellationToken.None);
        await library.UpsertAsync(new("scheduler", "run-history", "invalid-schedule", 1,
            JsonSerializer.SerializeToElement(new { scheduleId = "daily", startedUtc = start, finishedUtc = start.AddSeconds(1), status = 0, durationMilliseconds = 1000 }), start.AddSeconds(1)), CancellationToken.None);
        var snapshot = await service.GetSnapshotAsync(CancellationToken.None);
        if (snapshot.Entries.Count != 300 || !snapshot.Entries.Any(entry => entry.Id == "workflow:workflow-run")
            || !snapshot.Entries.Any(entry => entry.Id == "schedule:schedule-run")
            || snapshot.Entries.Any(entry => entry.Id == "workflow:scheduled-workflow")) throw new Exception("Existing history must be projected without duplicate scheduled workflows.");
        if (JsonSerializer.Serialize(snapshot).Contains("private content")) throw new Exception("Historical output leaked into activity.");
        if (AutomationRunActivityService.ReadProfileId(JsonSerializer.SerializeToElement(new { id = "https://secret/body" })) is not null)
            throw new Exception("Activity target IDs must be bounded metadata keys.");
        try
        {
            await service.RecordAsync(new("bad", "api", "saveProfile", "x", start, start, AutomationStatus.Success, 0, AutomationExecutionOrigin.Manual), CancellationToken.None);
            throw new Exception("Non-run writes must not be accepted by the host history.");
        }
        catch (ArgumentException) { }
        await SchedulerHistoryNotificationsAsync();
    }

    private static async Task SchedulerHistoryNotificationsAsync()
    {
        var library = new MemoryLibrary();
        var now = DateTimeOffset.UtcNow;
        var schedule = new AutomationScheduleDefinition("notify", "Notification test", "report", false,
            new(AutomationScheduleRecurrenceKind.Interval, IntervalMinutes: 60, IntervalAnchorUtc: now), false);
        await library.UpsertAsync(new("scheduler", "profiles", schedule.Id, 1, JsonSerializer.SerializeToElement(schedule), now), CancellationToken.None);
        var notifications = 0;
        await using (var scheduler = new AutomationSchedulerCoordinator(library, new SuccessfulWorkflow(), historyChanged: async () =>
        {
            var history = await library.ListAsync("scheduler", "run-history", CancellationToken.None);
            if (history.Count == 0) throw new Exception("A completion notification preceded metadata persistence.");
            notifications++;
            throw new IOException("Renderer unavailable");
        }))
        {
            await scheduler.StartAsync(CancellationToken.None);
            var completed = await scheduler.RunNowAsync(schedule.Id, CancellationToken.None);
            if (completed.Status != AutomationStatus.Success || notifications != 1) throw new Exception("Saved schedule completions must notify without changing the run outcome.");
        }
        await library.UpsertAsync(new("scheduler", "occurrence-claims", "manual-recovery", 1,
            JsonSerializer.SerializeToElement(new { id = "manual-recovery", scheduleId = schedule.Id, occurrenceUtc = now, claimedUtc = now }), now), CancellationToken.None);
        await using var restarted = new AutomationSchedulerCoordinator(library, new SuccessfulWorkflow(), historyChanged: () =>
        {
            notifications++;
            return Task.CompletedTask;
        });
        await restarted.StartAsync(CancellationToken.None);
        if (notifications != 2) throw new Exception("Recovered schedule metadata must notify the activity index.");
    }

    private sealed class SuccessfulWorkflow : IAutomationScheduledWorkflowExecutor
    {
        public Task<AutomationExecutionSummary> RunScheduledAsync(string workflowId, string correlationId, CancellationToken cancellationToken) =>
            Task.FromResult(new AutomationExecutionSummary(AutomationStatus.Success, "completed", 1));
    }

    private sealed class MemoryLibrary : IAutomationLibraryStore
    {
        private readonly Dictionary<(string Module, string Collection, string Id), AutomationLibraryRecord> _records = [];
        public Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(string moduleId, string collection, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AutomationLibraryRecord>>(_records.Values.Where(record => record.ModuleId == moduleId && record.Collection == collection).ToArray());
        public Task<AutomationLibraryRecord?> GetAsync(string moduleId, string collection, string id, CancellationToken cancellationToken) =>
            Task.FromResult(_records.GetValueOrDefault((moduleId, collection, id)));
        public Task UpsertAsync(AutomationLibraryRecord record, CancellationToken cancellationToken) { _records[(record.ModuleId, record.Collection, record.Id)] = record; return Task.CompletedTask; }
        public Task<bool> DeleteAsync(string moduleId, string collection, string id, CancellationToken cancellationToken) => Task.FromResult(_records.Remove((moduleId, collection, id)));
    }
}
