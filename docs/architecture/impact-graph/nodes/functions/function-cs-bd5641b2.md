---
graph_id: "function-cs-bd5641b2"
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

# Automator.Application.Automation.AutomationSchedulerCoordinator.SaveAsync(AutomationScheduleDefinition,CancellationToken)

public Task SaveAsync(AutomationScheduleDefinition schedule, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-028ec883|Program.WorkerSurvivesFailureAndSlowRuns()]] (tests/Automator.Scheduler.Specs/Program.cs:191)
- impact (called by): [[function-cs-3586d040|Program.ShutdownCancelsManualRun()]] (tests/Automator.Scheduler.Specs/Program.cs:204)
- impact (called by): [[function-cs-9d12dd19|Program.UpdatesRebaselineCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:176)
- impact (called by): [[function-cs-9d12dd19|Program.UpdatesRebaselineCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:179)
- impact (called by): [[function-cs-9d12dd19|Program.UpdatesRebaselineCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:183)
- impact (called by): [[function-cs-e55db0ee|Program.RunsSavedScriptProfile()]] (tests/Automator.Scheduler.Specs/Program.cs:86)
- impact (called by): [[function-cs-e5c7e3ca|Program.ClaimFailureDoesNotReplay()]] (tests/Automator.Scheduler.Specs/Program.cs:215)
- impact (called by): [[function-cs-f4f2ab89|Program.CoordinatorPersistsAndSerializesRuns()]] (tests/Automator.Scheduler.Specs/Program.cs:57)

## Source anchors

- `src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L122`
