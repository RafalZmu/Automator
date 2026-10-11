---
graph_id: "function-cs-78a92639"
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

# BrowserAutomationProxySpecs.SendProxyRequestAsync(BrowserAutomationProxy,string,bool,bool)

private static Task SendProxyRequestAsync(BrowserAutomationProxy proxy, string uri, bool expectTunnel, bool includeCredential)

## Connections

- impact (called by): [[function-cs-42e28345|BrowserAutomationProxySpecs.RedirectTargetsAndSubresourcesAreRecheckedAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:155)
- impact (called by): [[function-cs-42e28345|BrowserAutomationProxySpecs.RedirectTargetsAndSubresourcesAreRecheckedAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:156)
- impact (called by): [[function-cs-64415bf8|BrowserAutomationProxySpecs.SendsHttpToTheVettedAddressOnlyWithLocalGrantAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:47)
- impact (called by): [[function-cs-9a94e4a8|BrowserAutomationProxySpecs.RequiresTheSessionCredentialAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:71)
- impact (called by): [[function-cs-cae82b2e|BrowserAutomationProxySpecs.RejectsPrivateDnsAnswersWithoutLocalGrantAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:33)
- impact (called by): [[function-cs-fee3ab5f|BrowserAutomationProxySpecs.RejectsUnlistedHostBeforeConnectingAsync()]] (tests/Automator.Infrastructure.Browser.Specs/Program.cs:60)

## Source anchors

- `tests/Automator.Infrastructure.Browser.Specs/Program.cs#L175`
