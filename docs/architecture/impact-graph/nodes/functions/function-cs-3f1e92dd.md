---
graph_id: "function-cs-3f1e92dd"
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

# Program.SeedWorkflow(TestLibraryStore,string)

Task SeedWorkflow(TestLibraryStore store, string id)

## Connections

- impact (called by): [[function-cs-028ec883|Program.WorkerSurvivesFailureAndSlowRuns()]] (tests/Automator.Scheduler.Specs/Program.cs:186)
- impact (called by): [[function-cs-082c2db5|Program.StartupCanRetry()]] (tests/Automator.Scheduler.Specs/Program.cs:125)
- impact (called by): [[function-cs-3586d040|Program.ShutdownCancelsManualRun()]] (tests/Automator.Scheduler.Specs/Program.cs:201)
- impact (called by): [[function-cs-3e893542|Program.RecoveryPreservesCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:140)
- impact (called by): [[function-cs-74f78698|Program.CatchupRunsOnce()]] (tests/Automator.Scheduler.Specs/Program.cs:157)
- impact (called by): [[function-cs-9d12dd19|Program.UpdatesRebaselineCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:173)
- impact (called by): [[function-cs-d8aa3ef0|Program.MigratesLegacyWorkflowSchedule()]] (tests/Automator.Scheduler.Specs/Program.cs:95)
- impact (called by): [[function-cs-e5c7e3ca|Program.ClaimFailureDoesNotReplay()]] (tests/Automator.Scheduler.Specs/Program.cs:211)

## Source anchors

- `tests/Automator.Scheduler.Specs/Program.cs#L105`
