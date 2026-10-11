---
graph_id: "function-cs-b09c397d"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Launcher.LauncherSession.SelectTab(int)

public bool SelectTab(int tab)

## Connections

- impact (called by): [[function-cs-2dfdfcb0|Program.StaleCatalogCompletionIsIgnored()]] (tests/Automator.Application.Specs/Program.cs:199)
- impact (called by): [[function-cs-481685e2|Program.GlobalTabKeysRequireLauncherContext()]] (tests/Automator.Application.Specs/Program.cs:109)
- impact (called by): [[function-cs-6d31402f|Program.OpeningResetsStateAndCapturesContext()]] (tests/Automator.Application.Specs/Program.cs:87)
- impact (called by): [[function-cs-71105874|Program.AliasMatchingIsContextual()]] (tests/Automator.Application.Specs/Program.cs:246)
- impact (called by): [[function-cs-71105874|Program.AliasMatchingIsContextual()]] (tests/Automator.Application.Specs/Program.cs:248)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:230)
- impact (called by): [[function-cs-f79b54b9|Program.NavigationKeyPolicyRequiresFocusedUnmodifiedLauncher()]] (tests/Automator.Application.Specs/Program.cs:152)

## Source anchors

- `src/Automator.Application/Launcher/LauncherSession.cs#L117`
