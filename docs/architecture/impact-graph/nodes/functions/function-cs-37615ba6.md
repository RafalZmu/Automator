---
graph_id: "function-cs-37615ba6"
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

# Automator.Infrastructure.Automation.SharedHttpService.NormalizeHost(string)

private static string NormalizeHost(string host)

## Connections

- impact (called by): [[function-3105-d3cbe54c|lambda@src/Automator.Infrastructure/Automation/SharedHttpService.cs:60]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs:68)
- impact (called by): [[function-cs-3a6319d0|Automator.Infrastructure.Automation.SharedHttpService.SendAsync(string,AutomationHttpPolicy,AutomationHttpRequest,TimeSpan?,CancellationToken)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs:122)
- impact (called by): [[function-cs-7dda8c42|Automator.Infrastructure.Automation.SharedHttpService.ValidateAllowedHost(Uri,HashSet<string>)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs:378)
- impact (called by): [[function-cs-be855801|Automator.Infrastructure.Automation.SharedHttpService.NormalizeAllowedHosts(AutomationHttpPolicy)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs:392)
- impact (called by): [[function-cs-eba865db|Automator.Infrastructure.Automation.SharedHttpService.SameOrigin(Uri,Uri)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs:411)

## Source anchors

- `src/Automator.Infrastructure/Automation/SharedHttpService.cs#L398`
