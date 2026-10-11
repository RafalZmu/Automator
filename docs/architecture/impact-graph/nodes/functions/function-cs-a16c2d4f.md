---
graph_id: "function-cs-a16c2d4f"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/http"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Infrastructure.Automation.SharedHttpService.CreateProductionClient(IHostAddressResolver,bool)

public static HttpClient CreateProductionClient(IHostAddressResolver resolver, bool allowLocalNetwork)

## Connections

- impact (called by): [[function-cs-16473a16|Program.DoesNotShareCookiesAcrossModules()]] (tests/Automator.Infrastructure.Specs/Program.cs:397)
- impact (called by): [[function-cs-16473a16|Program.DoesNotShareCookiesAcrossModules()]] (tests/Automator.Infrastructure.Specs/Program.cs:398)
- impact (called by): [[function-cs-8ad53cb2|Automator.Backend.BackendServer.InitializeAsync(InitializeParams)]] (src/Automator.Backend/BackendServer.cs:893)
- impact (called by): [[function-cs-8ad53cb2|Automator.Backend.BackendServer.InitializeAsync(InitializeParams)]] (src/Automator.Backend/BackendServer.cs:894)

## Source anchors

- `src/Automator.Infrastructure/Automation/SharedHttpService.cs#L50`
