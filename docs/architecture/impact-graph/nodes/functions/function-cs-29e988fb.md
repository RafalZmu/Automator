---
graph_id: "function-cs-29e988fb"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/work-time"
  - "impact-graph/language/csharp"
  - "impact-graph/test"
generated_by: "scripts/impact-graph.mjs"
---

# FocusLibraryStore.Clone(AutomationLibraryRecord)

private static AutomationLibraryRecord Clone(AutomationLibraryRecord record)

## Connections

- impact (called by): [[function-cs-10d60a0f|FocusLibraryStore.UpsertAsync(AutomationLibraryRecord,CancellationToken)]] (tests/Automator.Focus.Specs/Program.cs:455)
- impact (called by): [[function-cs-a4bdfb6d|FocusLibraryStore.LastRecord(string,string,string)]] (tests/Automator.Focus.Specs/Program.cs:474)
- impact (called by): [[function-cs-e47cd512|FocusLibraryStore.GetAsync(string,string,string,CancellationToken)]] (tests/Automator.Focus.Specs/Program.cs:444)

## Source anchors

- `tests/Automator.Focus.Specs/Program.cs#L477`
