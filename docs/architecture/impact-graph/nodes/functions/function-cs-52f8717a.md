---
graph_id: "function-cs-52f8717a"
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

# Automator.Infrastructure.Automation.BrowserAutomationProxy.StartAsync(CancellationToken)

public Task StartAsync(CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-059bbb3f|BrowserAutomationProxySpecs.PinsAndAuthorizesConnectTunnelsAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:81)
- impact (called by): [[function-cs-059bbb3f|BrowserAutomationProxySpecs.PinsAndAuthorizesConnectTunnelsAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:91)
- impact (called by): [[function-cs-0fd78bb1|BrowserAutomationProxySpecs.RejectsMismatchedConnectAuthorityAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:118)
- impact (called by): [[function-cs-1b6c0f9c|BrowserAutomationProxySpecs.PreservesTheWebSocketUpgradeAfterHostValidationAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:104)
- impact (called by): [[function-cs-21393ae7|Automator.Infrastructure.Automation.PlaywrightAutomationBrowserService.StartSessionAsync(BrowserAutomationProfile,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:262)
- impact (called by): [[function-cs-42e28345|BrowserAutomationProxySpecs.RedirectTargetsAndSubresourcesAreRecheckedAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:153)
- impact (called by): [[function-cs-64415bf8|BrowserAutomationProxySpecs.SendsHttpToTheVettedAddressOnlyWithLocalGrantAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:45)
- impact (called by): [[function-cs-9a94e4a8|BrowserAutomationProxySpecs.RequiresTheSessionCredentialAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:69)
- impact (called by): [[function-cs-a8960cc4|BrowserAutomationProxySpecs.RevokesAnEstablishedTunnelWhenTheProfileGrantChangesAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:131)
- impact (called by): [[function-cs-cae82b2e|BrowserAutomationProxySpecs.RejectsPrivateDnsAnswersWithoutLocalGrantAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:31)
- impact (called by): [[function-cs-fee3ab5f|BrowserAutomationProxySpecs.RejectsUnlistedHostBeforeConnectingAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:58)

## Source anchors

- `src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L46`
