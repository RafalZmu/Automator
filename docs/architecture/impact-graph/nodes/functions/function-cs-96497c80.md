---
graph_id: "function-cs-96497c80"
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

# Automator.Application.Automation.ApiModule.ExecuteProfileAsync(string,ApiResponseMode,JsonElement?,IAutomationApiProfileRunner,CancellationToken)

internal static Task ExecuteProfileAsync(string profileId, ApiResponseMode responseMode, JsonElement? input, IAutomationApiProfileRunner runner, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-977a1c24|Automator.Application.Automation.ApiModule.RunProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:244)
- impact (called by): [[function-cs-c406b5d1|Automator.Application.Automation.ApiSavedProfileHandler.ExecuteAsync(string,JsonElement?,AutomationExecutionMetadata,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:581)

## Source anchors

- `src/Automator.Application/Automation/ApiModule.cs#L260`
