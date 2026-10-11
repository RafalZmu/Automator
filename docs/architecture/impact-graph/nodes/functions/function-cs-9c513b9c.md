---
graph_id: "function-cs-9c513b9c"
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

# Check.EventuallyAsync(Func<bool>)

public static Task EventuallyAsync(Func condition)

## Connections

- impact (called by): [[function-cs-4b429c00|Program.WorkerFailureIsLoggedAndRetried()]] (tests/Automator.Focus.Specs/Program.cs:215)
- impact (called by): [[function-cs-e61d8d09|ManualTimeProvider.WaitForTimerAsync()]] (tests/Automator.Focus.Specs/Program.cs:534)

## Source anchors

- `tests/Automator.Focus.Specs/Program.cs#L636`
