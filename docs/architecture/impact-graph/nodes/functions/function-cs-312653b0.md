---
graph_id: "function-cs-312653b0"
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

# Automator.Application.Automation.ApiModule.TruncateUtf8(string,int)

private static string TruncateUtf8(string value, int maximumBytes)

## Connections

- impact (called by): [[function-15614-ccfa9d11|lambda@src/Automator.Application/Automation/ApiModule.cs:273]] (src/Automator.Application/Automation/ApiModule.cs:273)
- impact (called by): [[function-cs-1366ad1c|Automator.Application.Automation.ApiModule.CreateBoundedOutput(AutomationApiProfileResponse,IReadOnlyDictionary<string, string>,string,ApiResponseMode,DecodedBody,bool,bool,DecodedBody)]] (src/Automator.Application/Automation/ApiModule.cs:312)
- impact (called by): [[function-cs-1366ad1c|Automator.Application.Automation.ApiModule.CreateBoundedOutput(AutomationApiProfileResponse,IReadOnlyDictionary<string, string>,string,ApiResponseMode,DecodedBody,bool,bool,DecodedBody)]] (src/Automator.Application/Automation/ApiModule.cs:331)
- impact (called by): [[function-cs-1366ad1c|Automator.Application.Automation.ApiModule.CreateBoundedOutput(AutomationApiProfileResponse,IReadOnlyDictionary<string, string>,string,ApiResponseMode,DecodedBody,bool,bool,DecodedBody)]] (src/Automator.Application/Automation/ApiModule.cs:338)
- impact (called by): [[function-cs-96497c80|Automator.Application.Automation.ApiModule.ExecuteProfileAsync(string,ApiResponseMode,JsonElement?,IAutomationApiProfileRunner,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:274)

## Source anchors

- `src/Automator.Application/Automation/ApiModule.cs#L500`
