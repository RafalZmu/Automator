---
graph_id: "function-cs-8e8e2b93"
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

# Check.ThrowsServiceAsync(Func<Task>)

public static Task ThrowsServiceAsync(Func action)

## Connections

- impact (called by): [[function-cs-0ee6ee36|Program.EnforcesResolvedAddressPolicy()]] (tests/Automator.Infrastructure.Specs/Program.cs:321)
- impact (called by): [[function-cs-0ee6ee36|Program.EnforcesResolvedAddressPolicy()]] (tests/Automator.Infrastructure.Specs/Program.cs:328)
- impact (called by): [[function-cs-0ee6ee36|Program.EnforcesResolvedAddressPolicy()]] (tests/Automator.Infrastructure.Specs/Program.cs:334)
- impact (called by): [[function-cs-0ee6ee36|Program.EnforcesResolvedAddressPolicy()]] (tests/Automator.Infrastructure.Specs/Program.cs:343)
- impact (called by): [[function-cs-2e383eda|Program.RejectsUnsafeUri()]] (tests/Automator.Infrastructure.Specs/Program.cs:235)
- impact (called by): [[function-cs-2e383eda|Program.RejectsUnsafeUri()]] (tests/Automator.Infrastructure.Specs/Program.cs:237)
- impact (called by): [[function-cs-60923b88|Program.MapsCancellationAndTimeout()]] (tests/Automator.Infrastructure.Specs/Program.cs:274)
- impact (called by): [[function-cs-60923b88|Program.MapsCancellationAndTimeout()]] (tests/Automator.Infrastructure.Specs/Program.cs:280)
- impact (called by): [[function-cs-632676fe|Program.DeniesCrossHostRedirect()]] (tests/Automator.Infrastructure.Specs/Program.cs:312)
- impact (called by): [[function-cs-840644f2|Program.BoundsResponseReading()]] (tests/Automator.Infrastructure.Specs/Program.cs:262)
- impact (called by): [[function-cs-886e1885|Program.RejectsOversizedRequest()]] (tests/Automator.Infrastructure.Specs/Program.cs:247)
- impact (called by): [[function-cs-b892b765|Program.LocalGrantDoesNotBypassHostAllowlist()]] (tests/Automator.Infrastructure.Specs/Program.cs:360)
- impact (called by): [[function-cs-c58707a3|Program.LimitsRedirects()]] (tests/Automator.Infrastructure.Specs/Program.cs:295)
- impact (called by): [[function-cs-c58707a3|Program.LimitsRedirects()]] (tests/Automator.Infrastructure.Specs/Program.cs:302)
- impact (called by): [[function-cs-e2e26f33|Program.EnforcesExactHostAllowlist()]] (tests/Automator.Infrastructure.Specs/Program.cs:226)
- impact (called by): [[function-cs-f6df1e39|Program.RejectsBinaryResponse()]] (tests/Automator.Infrastructure.Specs/Program.cs:440)

## Source anchors

- `tests/Automator.Infrastructure.Specs/Program.cs#L690`
