---
graph_id: "function-cs-c9fb5ab5"
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

# Automator.Infrastructure.Automation.BrowserAutomationProxy.UpdatePolicy(IReadOnlyList<string>,bool)

public void UpdatePolicy(IReadOnlyList allowedHosts, bool allowLocalNetwork)

## Connections

- impact (called by): [[function-cs-a07a153b|Automator.Infrastructure.Automation.PlaywrightAutomationBrowserService.WatchProfilePolicyAsync(string,BrowserAutomationProxy,CancellationToken)]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:325)
- impact (called by): [[function-cs-a07a153b|Automator.Infrastructure.Automation.PlaywrightAutomationBrowserService.WatchProfilePolicyAsync(string,BrowserAutomationProxy,CancellationToken)]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:327)
- impact (called by): [[function-cs-a07a153b|Automator.Infrastructure.Automation.PlaywrightAutomationBrowserService.WatchProfilePolicyAsync(string,BrowserAutomationProxy,CancellationToken)]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:333)
- impact (called by): [[function-cs-a8960cc4|BrowserAutomationProxySpecs.RevokesAnEstablishedTunnelWhenTheProfileGrantChangesAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:134)

## Source anchors

- `src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L63`
