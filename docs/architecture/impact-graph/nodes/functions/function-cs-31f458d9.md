---
graph_id: "function-cs-31f458d9"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/scheduler-coordinator"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Automation.AutomationSchedulerCoordinator.RunNowAsync(string,CancellationToken)

public Task RunNowAsync(string scheduleId, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-15582-8dde9330|lambda@tests/Automator.Scheduler.Specs/Program.cs:196]] (tests/Automator.Scheduler.Specs/Program.cs:196)
- impact (called by): [[function-5185-a0ad75af|lambda@tests/Automator.Scheduler.Specs/Program.cs:60]] (tests/Automator.Scheduler.Specs/Program.cs:60)
- impact (called by): [[function-cs-0ac81f87|RunActivitySpecs.SchedulerHistoryNotificationsAsync()]] (tests/Automator.Application.Specs/RunActivitySpecs.cs:67)
- impact (called by): [[function-cs-3586d040|Program.ShutdownCancelsManualRun()]] (tests/Automator.Scheduler.Specs/Program.cs:205)
- impact (called by): [[function-cs-e55db0ee|Program.RunsSavedScriptProfile()]] (tests/Automator.Scheduler.Specs/Program.cs:87)
- impact (called by): [[function-cs-f4f2ab89|Program.CoordinatorPersistsAndSerializesRuns()]] (tests/Automator.Scheduler.Specs/Program.cs:58)

## Source anchors

- `src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L193`
