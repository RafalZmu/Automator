---
graph_id: "function-cs-0a463b8f"
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

# BrowserAutomationProxySpecs.CreateProxy(IPAddress[],bool)

private static BrowserAutomationProxy CreateProxy(IPAddress[] addresses, bool allowLocalNetwork)

## Connections

- impact (called by): [[function-cs-059bbb3f|BrowserAutomationProxySpecs.PinsAndAuthorizesConnectTunnelsAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:79)
- impact (called by): [[function-cs-0fd78bb1|BrowserAutomationProxySpecs.RejectsMismatchedConnectAuthorityAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:117)
- impact (called by): [[function-cs-42e28345|BrowserAutomationProxySpecs.RedirectTargetsAndSubresourcesAreRecheckedAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:152)
- impact (called by): [[function-cs-64415bf8|BrowserAutomationProxySpecs.SendsHttpToTheVettedAddressOnlyWithLocalGrantAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:44)
- impact (called by): [[function-cs-9a94e4a8|BrowserAutomationProxySpecs.RequiresTheSessionCredentialAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:68)
- impact (called by): [[function-cs-a8960cc4|BrowserAutomationProxySpecs.RevokesAnEstablishedTunnelWhenTheProfileGrantChangesAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:130)
- impact (called by): [[function-cs-cae82b2e|BrowserAutomationProxySpecs.RejectsPrivateDnsAnswersWithoutLocalGrantAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:30)
- impact (called by): [[function-cs-fee3ab5f|BrowserAutomationProxySpecs.RejectsUnlistedHostBeforeConnectingAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:57)

## Source anchors

- `tests/Automator.Infrastructure.Browser.Specs/Program.cs#L165`
