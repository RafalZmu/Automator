---
graph_id: "function-cs-ce51b226"
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

# Automator.Application.Automation.IAutomationWebsiteLauncher.LaunchAsync(IReadOnlyList<IReadOnlyList<Uri>>,CancellationToken)

Task LaunchAsync(IReadOnlyList groups, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-c1989238|Automator.Application.Automation.WebsiteLauncherModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/WebsiteLauncherModule.cs:57)
- impact (called by): [[function-cs-ea358af5|Automator.Application.Automation.AutomationServicesContext.ContextWebsiteLauncher.LaunchAsync(IReadOnlyList<IReadOnlyList<Uri>>,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:417)

## Source anchors

- `src/Automator.Application/Automation/AutomationServicesContext.cs#L32`
