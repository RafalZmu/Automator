---
graph_id: "function-cs-eef7e43e"
graph_layer: "function"
graph_kind: "local-function"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/local-function"
  - "impact-graph/area/scheduler"
  - "impact-graph/language/csharp"
  - "impact-graph/test"
generated_by: "scripts/impact-graph.mjs"
---

# Program.IntervalSchedule(string,string,bool,bool)

AutomationScheduleDefinition IntervalSchedule(string id, string workflowId, bool enabled, bool catchup)

## Connections

- impact (called by): [[function-cs-028ec883|Program.WorkerSurvivesFailureAndSlowRuns()]] (tests/Automator.Scheduler.Specs/Program.cs:191)
- impact (called by): [[function-cs-3586d040|Program.ShutdownCancelsManualRun()]] (tests/Automator.Scheduler.Specs/Program.cs:204)
- impact (called by): [[function-cs-3e893542|Program.RecoveryPreservesCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:142)
- impact (called by): [[function-cs-74f78698|Program.CatchupRunsOnce()]] (tests/Automator.Scheduler.Specs/Program.cs:159)
- impact (called by): [[function-cs-9d12dd19|Program.UpdatesRebaselineCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:176)
- impact (called by): [[function-cs-e5c7e3ca|Program.ClaimFailureDoesNotReplay()]] (tests/Automator.Scheduler.Specs/Program.cs:215)

## Source anchors

- `tests/Automator.Scheduler.Specs/Program.cs#L107`
