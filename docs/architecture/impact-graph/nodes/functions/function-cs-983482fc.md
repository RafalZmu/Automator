---
graph_id: "function-cs-983482fc"
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

# Automator.Application.Automation.ApiModule.ReadString(JsonElement,string,int)

private static string ReadString(JsonElement input, string propertyName, int maximumLength)

## Connections

- impact (called by): [[function-cs-0dba8912|Automator.Application.Automation.ApiModule.ReadProfileId(JsonElement)]] (src/Automator.Application/Automation/ApiModule.cs:467)
- impact (called by): [[function-cs-3cd05cae|Automator.Application.Automation.ApiModule.ReadSecretInput(JsonElement)]] (src/Automator.Application/Automation/ApiModule.cs:475)
- impact (called by): [[function-cs-3cd05cae|Automator.Application.Automation.ApiModule.ReadSecretInput(JsonElement)]] (src/Automator.Application/Automation/ApiModule.cs:476)
- impact (called by): [[function-cs-6571ede2|Automator.Application.Automation.ApiModule.ClearSecretAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:209)

## Source anchors

- `src/Automator.Application/Automation/ApiModule.cs#L481`
