---
graph_id: "function-cs-9473e23d"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/playwright"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Infrastructure.Automation.PlaywrightAutomationBrowserService.TryKill(Process)

private static void TryKill(Process process)

## Connections

- impact (called by): [[function-cs-080c537d|Automator.Infrastructure.Automation.PlaywrightAutomationBrowserService.EnsureBrowserInstalledAsync(CancellationToken)]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:220)
- impact (called by): [[function-cs-21393ae7|Automator.Infrastructure.Automation.PlaywrightAutomationBrowserService.StartSessionAsync(BrowserAutomationProfile,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:283)
- impact (called by): [[function-cs-21393ae7|Automator.Infrastructure.Automation.PlaywrightAutomationBrowserService.StartSessionAsync(BrowserAutomationProfile,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:294)
- impact (called by): [[function-cs-9023397b|Automator.Infrastructure.Automation.PlaywrightAutomationBrowserService.BrowserSession.DisposeAsync()]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:458)
- impact (called by): [[function-cs-cecd0e1e|Automator.Infrastructure.Automation.PlaywrightAutomationBrowserService.ReadWorkerStatusAsync(CancellationToken)]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:251)

## Source anchors

- `src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs#L420`
