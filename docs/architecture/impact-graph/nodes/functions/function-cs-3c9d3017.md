---
graph_id: "function-cs-3c9d3017"
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

# Automator.Application.Automation.IAutomationSecretManager.ExistsAsync(string,string,CancellationToken)

Task ExistsAsync(string profileId, string secretId, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-48733-b3212799|lambda@src/Automator.Backend/BackendServer.cs:917]] (src/Automator.Backend/BackendServer.cs:917)
- impact (called by): [[function-cs-1f6e0b6f|Automator.Application.Automation.ApiModule.ListProfilesAsync(AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:127)
- impact (called by): [[function-cs-6997db1a|Automator.Application.Automation.AutomationServicesContext.ContextSecretManager.ExistsAsync(string,string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:227)

## Source anchors

- `src/Automator.Application/Automation/AutomationApiContracts.cs#L16`
