---
graph_id: "function-cs-01581948"
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

# Automator.Application.Automation.IAutomationSecretManager.DeleteAsync(string,string,CancellationToken)

Task DeleteAsync(string profileId, string secretId, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-52c61dc6|Automator.Application.Automation.ApiModule.DeleteSecretsAsync(IAutomationSecretManager,string,IEnumerable<string>,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:372)
- impact (called by): [[function-cs-6571ede2|Automator.Application.Automation.ApiModule.ClearSecretAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:222)
- impact (called by): [[function-cs-b4cc9dc5|Automator.Application.Automation.AutomationServicesContext.ContextSecretManager.DeleteAsync(string,string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:233)

## Source anchors

- `src/Automator.Application/Automation/AutomationApiContracts.cs#L17`
