---
graph_id: "function-cs-f4aef918"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/scheduler"
  - "impact-graph/language/csharp"
  - "impact-graph/test"
generated_by: "scripts/impact-graph.mjs"
---

# TestLibraryStore.UpsertAsync(AutomationLibraryRecord,CancellationToken)

public Task UpsertAsync(AutomationLibraryRecord record, CancellationToken ct)

## Connections

- impact (called by): [[function-cs-082c2db5|Program.StartupCanRetry()]] (tests/Automator.Scheduler.Specs/Program.cs:133)
- impact (called by): [[function-cs-3e893542|Program.RecoveryPreservesCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:146)
- impact (called by): [[function-cs-3f1e92dd|Program.SeedWorkflow(TestLibraryStore,string)]] (tests/Automator.Scheduler.Specs/Program.cs:105)
- impact (called by): [[function-cs-644066e3|Program.SeedSchedule(TestLibraryStore,AutomationScheduleDefinition)]] (tests/Automator.Scheduler.Specs/Program.cs:111)
- impact (called by): [[function-cs-d8aa3ef0|Program.MigratesLegacyWorkflowSchedule()]] (tests/Automator.Scheduler.Specs/Program.cs:96)
- impact (called by): [[function-cs-f4f2ab89|Program.CoordinatorPersistsAndSerializesRuns()]] (tests/Automator.Scheduler.Specs/Program.cs:49)
- impact (called by): [[function-cs-fb2f8495|Program.SeedCursor(TestLibraryStore,string,DateTimeOffset)]] (tests/Automator.Scheduler.Specs/Program.cs:114)

## Source anchors

- `tests/Automator.Scheduler.Specs/Program.cs#L296`
