---
graph_id: "function-cs-b1110dd9"
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

# Automator.Infrastructure.Automation.BrowserAutomationProxy.WriteErrorAsync(Stream,int,string,CancellationToken)

private static Task WriteErrorAsync(Stream stream, int status, string reason, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-08ba8630|Automator.Infrastructure.Automation.BrowserAutomationProxy.HandleClientAsync(TcpClient,NetworkPolicy,CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs:146)
- impact (called by): [[function-cs-08ba8630|Automator.Infrastructure.Automation.BrowserAutomationProxy.HandleClientAsync(TcpClient,NetworkPolicy,CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs:151)
- impact (called by): [[function-cs-31abd2e5|Automator.Infrastructure.Automation.BrowserAutomationProxy.HandleConnectAsync(TcpClient,NetworkStream,ProxyRequest,NetworkPolicy,CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs:174)
- impact (called by): [[function-cs-31abd2e5|Automator.Infrastructure.Automation.BrowserAutomationProxy.HandleConnectAsync(TcpClient,NetworkStream,ProxyRequest,NetworkPolicy,CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs:180)
- impact (called by): [[function-cs-c30644e4|Automator.Infrastructure.Automation.BrowserAutomationProxy.HandleHttpAsync(TcpClient,NetworkStream,ProxyRequest,NetworkPolicy,CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs:196)
- impact (called by): [[function-cs-c30644e4|Automator.Infrastructure.Automation.BrowserAutomationProxy.HandleHttpAsync(TcpClient,NetworkStream,ProxyRequest,NetworkPolicy,CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs:202)

## Source anchors

- `src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L339`
