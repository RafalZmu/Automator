---
graph_id: "function-cs-056a2eb0"
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

# ManualTimeProvider.GetUtcNow()

public override DateTimeOffset GetUtcNow()

## Connections

- impact (called by): [[function-cs-1ea7ed37|Program.ControlsPersistStateAndHistory()]] (tests/Automator.Focus.Specs/Program.cs:75)
- impact (called by): [[function-cs-b59418f4|ManualTimeProvider.ManualTimer.Change(TimeSpan,TimeSpan)]] (tests/Automator.Focus.Specs/Program.cs:578)
- impact (called by): [[function-cs-fd7d9082|ManualTimeProvider.ManualTimer.Fire()]] (tests/Automator.Focus.Specs/Program.cs:600)

## Source anchors

- `tests/Automator.Focus.Specs/Program.cs#L522`
