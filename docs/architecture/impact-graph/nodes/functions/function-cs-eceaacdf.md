---
graph_id: "function-cs-eceaacdf"
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

# Automator.Infrastructure.Automation.BrowserAutomationProxy.NormalizeHost(string)

private static string NormalizeHost(string host)

## Connections

- impact (called by): [[function-cs-31abd2e5|Automator.Infrastructure.Automation.BrowserAutomationProxy.HandleConnectAsync(TcpClient,NetworkStream,ProxyRequest,NetworkPolicy,CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs:172)
- impact (called by): [[function-cs-8871a4c6|Automator.Infrastructure.Automation.BrowserAutomationProxy.RequestHostMatches(IReadOnlyDictionary<string, string>,Uri)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs:302)
- impact (called by): [[function-cs-a7c582e1|Automator.Infrastructure.Automation.BrowserAutomationProxy.IsAllowedHost(string,NetworkPolicy)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs:246)

## Source anchors

- `src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L346`
