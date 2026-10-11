---
graph_id: "function-cs-1366ad1c"
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

# Automator.Application.Automation.ApiModule.CreateBoundedOutput(AutomationApiProfileResponse,IReadOnlyDictionary<string, string>,string,ApiResponseMode,DecodedBody,bool,bool,DecodedBody)

private static JsonElement CreateBoundedOutput(AutomationApiProfileResponse response, IReadOnlyDictionary headers, string profileId, ApiResponseMode responseMode, DecodedBody decoded, bool bodyTruncated, bool finalBodyTruncated, DecodedBody finalDecoded)

## Connections

- impact (called by): [[function-cs-96497c80|Automator.Application.Automation.ApiModule.ExecuteProfileAsync(string,ApiResponseMode,JsonElement?,IAutomationApiProfileRunner,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:277)

## Source anchors

- `src/Automator.Application/Automation/ApiModule.cs#L292`
