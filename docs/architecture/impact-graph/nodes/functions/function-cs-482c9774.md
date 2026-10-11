---
graph_id: "function-cs-482c9774"
graph_layer: "function"
graph_kind: "constructor"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/constructor"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Infrastructure.Automation.AutomationLibraryTransferService.AutomationLibraryTransferService(IAutomationLibraryStore,Func<string, string, CancellationToken, Task<bool>>?,TimeProvider?)

public AutomationLibraryTransferService(IAutomationLibraryStore store, Func secretExists, TimeProvider clock)

## Connections

- impact (called by): [[function-cs-8ad53cb2|Automator.Backend.BackendServer.InitializeAsync(InitializeParams)]] (src/Automator.Backend/BackendServer.cs:916)

## Source anchors

- `src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs#L48`
