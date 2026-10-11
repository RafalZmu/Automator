---
graph_id: "function-cs-645589c3"
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

# Program.Context(FakeCoordinator)

AutomationServicesContext Context(FakeCoordinator coordinator)

## Connections

- impact (called by): [[function-cs-b9cd4b3f|Program.ReadsSafeSnapshot()]] (tests/Automator.Scheduler.Specs/Program.cs:260)
- impact (called by): [[function-cs-cc528a68|Program.ControlsHostCoordinator()]] (tests/Automator.Scheduler.Specs/Program.cs:239)
- impact (called by): [[function-cs-dd304ddc|Program.RejectsInvalidRecurrence()]] (tests/Automator.Scheduler.Specs/Program.cs:251)
- impact (called by): [[function-cs-e4fe6157|Program.HandlesFailureAndCancellation()]] (tests/Automator.Scheduler.Specs/Program.cs:267)

## Source anchors

- `tests/Automator.Scheduler.Specs/Program.cs#L235`
