---
graph_id: "function-cs-4b629b78"
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

# Program.Json(string)

JsonElement Json(string value)

## Connections

- impact (called by): [[function-cs-082c2db5|Program.StartupCanRetry()]] (tests/Automator.Scheduler.Specs/Program.cs:134)
- impact (called by): [[function-cs-3f1e92dd|Program.SeedWorkflow(TestLibraryStore,string)]] (tests/Automator.Scheduler.Specs/Program.cs:106)
- impact (called by): [[function-cs-b9cd4b3f|Program.ReadsSafeSnapshot()]] (tests/Automator.Scheduler.Specs/Program.cs:261)
- impact (called by): [[function-cs-cc528a68|Program.ControlsHostCoordinator()]] (tests/Automator.Scheduler.Specs/Program.cs:241)
- impact (called by): [[function-cs-cc528a68|Program.ControlsHostCoordinator()]] (tests/Automator.Scheduler.Specs/Program.cs:243)
- impact (called by): [[function-cs-cc528a68|Program.ControlsHostCoordinator()]] (tests/Automator.Scheduler.Specs/Program.cs:245)
- impact (called by): [[function-cs-cc528a68|Program.ControlsHostCoordinator()]] (tests/Automator.Scheduler.Specs/Program.cs:247)
- impact (called by): [[function-cs-d8aa3ef0|Program.MigratesLegacyWorkflowSchedule()]] (tests/Automator.Scheduler.Specs/Program.cs:97)
- impact (called by): [[function-cs-dd304ddc|Program.RejectsInvalidRecurrence()]] (tests/Automator.Scheduler.Specs/Program.cs:253)
- impact (called by): [[function-cs-dd304ddc|Program.RejectsInvalidRecurrence()]] (tests/Automator.Scheduler.Specs/Program.cs:254)
- impact (called by): [[function-cs-dd304ddc|Program.RejectsInvalidRecurrence()]] (tests/Automator.Scheduler.Specs/Program.cs:257)
- impact (called by): [[function-cs-e4fe6157|Program.HandlesFailureAndCancellation()]] (tests/Automator.Scheduler.Specs/Program.cs:268)
- impact (called by): [[function-cs-e4fe6157|Program.HandlesFailureAndCancellation()]] (tests/Automator.Scheduler.Specs/Program.cs:271)
- impact (called by): [[function-cs-f4f2ab89|Program.CoordinatorPersistsAndSerializesRuns()]] (tests/Automator.Scheduler.Specs/Program.cs:50)

## Source anchors

- `tests/Automator.Scheduler.Specs/Program.cs#L27`
