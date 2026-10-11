---
graph_id: "function-cs-994ec76c"
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

# Automator.Application.Automation.AutomationSchedulerCoordinator.StartAsync(CancellationToken)

public Task StartAsync(CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-10191-6f4a665e|lambda@tests/Automator.Scheduler.Specs/Program.cs:129]] (tests/Automator.Scheduler.Specs/Program.cs:129)
- impact (called by): [[function-10331-2749614a|lambda@tests/Automator.Scheduler.Specs/Program.cs:131]] (tests/Automator.Scheduler.Specs/Program.cs:131)
- impact (called by): [[function-cs-028ec883|Program.WorkerSurvivesFailureAndSlowRuns()]] (tests/Automator.Scheduler.Specs/Program.cs:190)
- impact (called by): [[function-cs-082c2db5|Program.StartupCanRetry()]] (tests/Automator.Scheduler.Specs/Program.cs:135)
- impact (called by): [[function-cs-0ac81f87|RunActivitySpecs.SchedulerHistoryNotificationsAsync()]] (tests/Automator.Application.Specs/RunActivitySpecs.cs:66)
- impact (called by): [[function-cs-0ac81f87|RunActivitySpecs.SchedulerHistoryNotificationsAsync()]] (tests/Automator.Application.Specs/RunActivitySpecs.cs:77)
- impact (called by): [[function-cs-3586d040|Program.ShutdownCancelsManualRun()]] (tests/Automator.Scheduler.Specs/Program.cs:203)
- impact (called by): [[function-cs-3e893542|Program.RecoveryPreservesCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:150)
- impact (called by): [[function-cs-74f78698|Program.CatchupRunsOnce()]] (tests/Automator.Scheduler.Specs/Program.cs:163)
- impact (called by): [[function-cs-74f78698|Program.CatchupRunsOnce()]] (tests/Automator.Scheduler.Specs/Program.cs:168)
- impact (called by): [[function-cs-8ad53cb2|Automator.Backend.BackendServer.InitializeAsync(InitializeParams)]] (src/Automator.Backend/BackendServer.cs:936)
- impact (called by): [[function-cs-9d12dd19|Program.UpdatesRebaselineCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:176)
- impact (called by): [[function-cs-d8aa3ef0|Program.MigratesLegacyWorkflowSchedule()]] (tests/Automator.Scheduler.Specs/Program.cs:100)
- impact (called by): [[function-cs-e55db0ee|Program.RunsSavedScriptProfile()]] (tests/Automator.Scheduler.Specs/Program.cs:80)
- impact (called by): [[function-cs-e5c7e3ca|Program.ClaimFailureDoesNotReplay()]] (tests/Automator.Scheduler.Specs/Program.cs:215)
- impact (called by): [[function-cs-e5c7e3ca|Program.ClaimFailureDoesNotReplay()]] (tests/Automator.Scheduler.Specs/Program.cs:224)
- impact (called by): [[function-cs-f4f2ab89|Program.CoordinatorPersistsAndSerializesRuns()]] (tests/Automator.Scheduler.Specs/Program.cs:54)

## Source anchors

- `src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L60`
