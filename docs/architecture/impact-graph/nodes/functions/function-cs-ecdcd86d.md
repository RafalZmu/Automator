---
graph_id: "function-cs-ecdcd86d"
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

# Automator.Application.Automation.AutomationSchedulerCoordinator.GetSnapshotAsync(CancellationToken)

public Task GetSnapshotAsync(CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-10421-07bedc77|lambda@tests/Automator.Scheduler.Specs/Program.cs:132]] (tests/Automator.Scheduler.Specs/Program.cs:132)
- impact (called by): [[function-cs-028ec883|Program.WorkerSurvivesFailureAndSlowRuns()]] (tests/Automator.Scheduler.Specs/Program.cs:195)
- impact (called by): [[function-cs-082c2db5|Program.StartupCanRetry()]] (tests/Automator.Scheduler.Specs/Program.cs:136)
- impact (called by): [[function-cs-082c2db5|Program.StartupCanRetry()]] (tests/Automator.Scheduler.Specs/Program.cs:137)
- impact (called by): [[function-cs-3e893542|Program.RecoveryPreservesCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:152)
- impact (called by): [[function-cs-74f78698|Program.CatchupRunsOnce()]] (tests/Automator.Scheduler.Specs/Program.cs:169)
- impact (called by): [[function-cs-9d12dd19|Program.UpdatesRebaselineCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:181)
- impact (called by): [[function-cs-d8aa3ef0|Program.MigratesLegacyWorkflowSchedule()]] (tests/Automator.Scheduler.Specs/Program.cs:101)
- impact (called by): [[function-cs-e55db0ee|Program.RunsSavedScriptProfile()]] (tests/Automator.Scheduler.Specs/Program.cs:81)
- impact (called by): [[function-cs-e55db0ee|Program.RunsSavedScriptProfile()]] (tests/Automator.Scheduler.Specs/Program.cs:91)
- impact (called by): [[function-cs-e5c7e3ca|Program.ClaimFailureDoesNotReplay()]] (tests/Automator.Scheduler.Specs/Program.cs:225)
- impact (called by): [[function-cs-f4f2ab89|Program.CoordinatorPersistsAndSerializesRuns()]] (tests/Automator.Scheduler.Specs/Program.cs:67)

## Source anchors

- `src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L103`
