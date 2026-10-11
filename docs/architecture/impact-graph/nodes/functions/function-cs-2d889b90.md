---
graph_id: "function-cs-2d889b90"
graph_layer: "function"
graph_kind: "local-function"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/local-function"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
  - "impact-graph/test"
generated_by: "scripts/impact-graph.mjs"
---

# Program.Policy(string[])

AutomationHttpPolicy Policy(string[] hosts)

## Connections

- impact (called by): [[function-cs-0ee6ee36|Program.EnforcesResolvedAddressPolicy()]] (tests/Automator.Infrastructure.Specs/Program.cs:320)
- impact (called by): [[function-cs-0ee6ee36|Program.EnforcesResolvedAddressPolicy()]] (tests/Automator.Infrastructure.Specs/Program.cs:327)
- impact (called by): [[function-cs-0ee6ee36|Program.EnforcesResolvedAddressPolicy()]] (tests/Automator.Infrastructure.Specs/Program.cs:333)
- impact (called by): [[function-cs-0ee6ee36|Program.EnforcesResolvedAddressPolicy()]] (tests/Automator.Infrastructure.Specs/Program.cs:350)
- impact (called by): [[function-cs-2e383eda|Program.RejectsUnsafeUri()]] (tests/Automator.Infrastructure.Specs/Program.cs:234)
- impact (called by): [[function-cs-60923b88|Program.MapsCancellationAndTimeout()]] (tests/Automator.Infrastructure.Specs/Program.cs:270)
- impact (called by): [[function-cs-60923b88|Program.MapsCancellationAndTimeout()]] (tests/Automator.Infrastructure.Specs/Program.cs:279)
- impact (called by): [[function-cs-632676fe|Program.DeniesCrossHostRedirect()]] (tests/Automator.Infrastructure.Specs/Program.cs:311)
- impact (called by): [[function-cs-6d98fe9f|Program.ReturnsNonSuccessResult()]] (tests/Automator.Infrastructure.Specs/Program.cs:212)
- impact (called by): [[function-cs-83a98a88|Program.MapsRequestFields()]] (tests/Automator.Infrastructure.Specs/Program.cs:196)
- impact (called by): [[function-cs-840644f2|Program.BoundsResponseReading()]] (tests/Automator.Infrastructure.Specs/Program.cs:261)
- impact (called by): [[function-cs-886e1885|Program.RejectsOversizedRequest()]] (tests/Automator.Infrastructure.Specs/Program.cs:245)
- impact (called by): [[function-cs-b811e19d|Program.RedactsSecrets()]] (tests/Automator.Infrastructure.Specs/Program.cs:453)
- impact (called by): [[function-cs-c58707a3|Program.LimitsRedirects()]] (tests/Automator.Infrastructure.Specs/Program.cs:294)
- impact (called by): [[function-cs-c58707a3|Program.LimitsRedirects()]] (tests/Automator.Infrastructure.Specs/Program.cs:301)
- impact (called by): [[function-cs-e2e26f33|Program.EnforcesExactHostAllowlist()]] (tests/Automator.Infrastructure.Specs/Program.cs:222)
- impact (called by): [[function-cs-e366ca00|Program.PartitionsNetworkPolicyPools()]] (tests/Automator.Infrastructure.Specs/Program.cs:378)
- impact (called by): [[function-cs-f6df1e39|Program.RejectsBinaryResponse()]] (tests/Automator.Infrastructure.Specs/Program.cs:439)

## Source anchors

- `tests/Automator.Infrastructure.Specs/Program.cs#L586`
