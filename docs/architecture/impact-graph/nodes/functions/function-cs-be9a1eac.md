---
graph_id: "function-cs-be9a1eac"
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

# Check.EventuallyAsync(Func<Task<bool>>)

public static Task EventuallyAsync(Func condition)

## Connections

- impact (called by): [[function-cs-4b429c00|Program.WorkerFailureIsLoggedAndRetried()]] (tests/Automator.Focus.Specs/Program.cs:223)
- impact (called by): [[function-cs-ca2a4f0a|Program.NaturalBoundaryPersistsBeforeNotification()]] (tests/Automator.Focus.Specs/Program.cs:127)
- impact (called by): [[function-cs-ca2a4f0a|Program.NaturalBoundaryPersistsBeforeNotification()]] (tests/Automator.Focus.Specs/Program.cs:144)

## Source anchors

- `tests/Automator.Focus.Specs/Program.cs#L625`
