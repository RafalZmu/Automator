---
graph_id: "function-cs-b32f5cd2"
graph_layer: "function"
graph_kind: "constructor"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/constructor"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Infrastructure.Automation.BrowserAutomationProxy.BrowserAutomationProxy(IHostAddressResolver,IReadOnlyList<string>,bool,string,string)

public BrowserAutomationProxy(IHostAddressResolver resolver, IReadOnlyList allowedHosts, bool allowLocalNetwork, string username, string password)

## Connections

- impact (called by): [[function-cs-059bbb3f|BrowserAutomationProxySpecs.PinsAndAuthorizesConnectTunnelsAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:89)
- impact (called by): [[function-cs-1b6c0f9c|BrowserAutomationProxySpecs.PreservesTheWebSocketUpgradeAfterHostValidationAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:102)
- impact (called by): [[function-cs-21393ae7|Automator.Infrastructure.Automation.PlaywrightAutomationBrowserService.StartSessionAsync(BrowserAutomationProfile,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:261)

## Source anchors

- `src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L29`
