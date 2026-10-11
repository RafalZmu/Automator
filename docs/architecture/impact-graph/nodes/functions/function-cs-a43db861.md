---
graph_id: "function-cs-a43db861"
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

# Automator.Application.Automation.IAutomationSecretManager.SetAsync(string,string,string,CancellationToken)

Task SetAsync(string profileId, string secretId, string value, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-0c472db4|Automator.Application.Automation.ApiModule.SetSecretAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:194)
- impact (called by): [[function-cs-3f816fcb|Automator.Application.Automation.AutomationServicesContext.ContextSecretManager.SetAsync(string,string,string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:221)

## Source anchors

- `src/Automator.Application/Automation/AutomationApiContracts.cs#L15`
