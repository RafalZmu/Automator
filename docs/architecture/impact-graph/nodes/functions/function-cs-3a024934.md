---
graph_id: "function-cs-3a024934"
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

# Automator.Infrastructure.Automation.IHostAddressResolver.ResolveAsync(string,CancellationToken)

Task ResolveAsync(string host, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-1d2ec323|Automator.Infrastructure.Automation.SharedHttpService.ResolveAddressesAsync(IHostAddressResolver,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs:250)
- impact (called by): [[function-cs-9a045ef7|Automator.Infrastructure.Automation.BrowserAutomationProxy.ResolveAndValidateAsync(string,NetworkPolicy,CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs:234)

## Source anchors

- `src/Automator.Infrastructure/Automation/HostAddressResolver.cs#L8`
