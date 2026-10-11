---
graph_id: "function-cs-4d98c25f"
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

# Program.Cursor(TestLibraryStore,string)

Task Cursor(TestLibraryStore store, string id)

## Connections

- impact (called by): [[function-cs-3e893542|Program.RecoveryPreservesCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:151)
- impact (called by): [[function-cs-74f78698|Program.CatchupRunsOnce()]] (tests/Automator.Scheduler.Specs/Program.cs:165)
- impact (called by): [[function-cs-9d12dd19|Program.UpdatesRebaselineCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:180)
- impact (called by): [[function-cs-9d12dd19|Program.UpdatesRebaselineCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:183)
- impact (called by): [[function-cs-e5c7e3ca|Program.ClaimFailureDoesNotReplay()]] (tests/Automator.Scheduler.Specs/Program.cs:224)

## Source anchors

- `tests/Automator.Scheduler.Specs/Program.cs#L116`
