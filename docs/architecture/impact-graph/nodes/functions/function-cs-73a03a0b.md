---
graph_id: "function-cs-73a03a0b"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
  - "impact-graph/test"
generated_by: "scripts/impact-graph.mjs"
---

# Check.EventuallyAsync(Func<bool>)

public static Task EventuallyAsync(Func condition)

## Connections

- impact (called by): [[function-cs-02b16009|Program.KeyboardSubscribersAreRevokedAndOverflowIsDetached()]] (tests/Automator.Application.Specs/Program.cs:960)
- impact (called by): [[function-cs-13052aaf|Program.KeyboardDeliveryRequiresTrustedContext()]] (tests/Automator.Application.Specs/Program.cs:923)

## Source anchors

- `tests/Automator.Application.Specs/Program.cs#L1291`
