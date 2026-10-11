---
graph_id: "function-cs-624cbd1c"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/playwright"
  - "impact-graph/language/csharp"
  - "impact-graph/test"
generated_by: "scripts/impact-graph.mjs"
---

# BrowserAutomationProxySpecs.StartListener(IPAddress)

private static TcpListener StartListener(IPAddress address)

## Connections

- impact (called by): [[function-cs-059bbb3f|BrowserAutomationProxySpecs.PinsAndAuthorizesConnectTunnelsAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:78)
- impact (called by): [[function-cs-0fd78bb1|BrowserAutomationProxySpecs.RejectsMismatchedConnectAuthorityAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:116)
- impact (called by): [[function-cs-1b6c0f9c|BrowserAutomationProxySpecs.PreservesTheWebSocketUpgradeAfterHostValidationAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:99)
- impact (called by): [[function-cs-42e28345|BrowserAutomationProxySpecs.RedirectTargetsAndSubresourcesAreRecheckedAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:149)
- impact (called by): [[function-cs-64415bf8|BrowserAutomationProxySpecs.SendsHttpToTheVettedAddressOnlyWithLocalGrantAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:41)
- impact (called by): [[function-cs-a8960cc4|BrowserAutomationProxySpecs.RevokesAnEstablishedTunnelWhenTheProfileGrantChangesAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:128)
- impact (called by): [[function-cs-cae82b2e|BrowserAutomationProxySpecs.RejectsPrivateDnsAnswersWithoutLocalGrantAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:29)
- impact (called by): [[function-cs-fee3ab5f|BrowserAutomationProxySpecs.RejectsUnlistedHostBeforeConnectingAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:56)

## Source anchors

- `tests/Automator.Infrastructure.Browser.Specs/Program.cs#L168`
