---
graph_id: "function-cs-52c61dc6"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/api"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Automation.ApiModule.DeleteSecretsAsync(IAutomationSecretManager,string,IEnumerable<string>,CancellationToken)

private static Task DeleteSecretsAsync(IAutomationSecretManager secrets, string profileId, IEnumerable secretIds, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-93e0cb83|Automator.Application.Automation.ApiModule.SaveProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:158)
- impact (called by): [[function-cs-dd7d1a9e|Automator.Application.Automation.ApiModule.DeleteProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:175)

## Source anchors

- `src/Automator.Application/Automation/ApiModule.cs#L363`
