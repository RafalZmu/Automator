---
graph_id: "function-cs-025392ad"
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

# Automator.Infrastructure.Automation.BrowserAutomationProxy.TryParseAuthority(string,string,int)

private static bool TryParseAuthority(string authority, string host, int port)

## Connections

- impact (called by): [[function-cs-31abd2e5|Automator.Infrastructure.Automation.BrowserAutomationProxy.HandleConnectAsync(TcpClient,NetworkStream,ProxyRequest,NetworkPolicy,CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs:170)
- impact (called by): [[function-cs-31abd2e5|Automator.Infrastructure.Automation.BrowserAutomationProxy.HandleConnectAsync(TcpClient,NetworkStream,ProxyRequest,NetworkPolicy,CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs:171)
- impact (called by): [[function-cs-8871a4c6|Automator.Infrastructure.Automation.BrowserAutomationProxy.RequestHostMatches(IReadOnlyDictionary<string, string>,Uri)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs:301)

## Source anchors

- `src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L305`
