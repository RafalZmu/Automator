---
graph_id: "explorer-discover"
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

# Automator.Application.Automation.PlaywrightTestExplorer.DiscoverTestsAsync(string,RunnerState,CancellationToken)

private Task DiscoverTestsAsync(string root, RunnerState runner, CancellationToken cancellationToken)

## Connections

- impact (RunAsync discovers tests before choosing the run target): [[explorer-run|Automator.Application.Automation.PlaywrightTestExplorer.RunAsync(JsonElement,CancellationToken)]] (src/Automator.Application/Automation/PlaywrightTestExplorer.cs#L177)
- impact (called by): [[function-cs-0c537ff9|Automator.Application.Automation.PlaywrightTestExplorer.SaveTagsAsync(JsonElement,CancellationToken)]] (src/Automator.Application/Automation/PlaywrightTestExplorer.cs:139)
- impact (called by): [[function-cs-96bdb175|Automator.Application.Automation.PlaywrightTestExplorer.RemoveStaleTagsAsync(CancellationToken)]] (src/Automator.Application/Automation/PlaywrightTestExplorer.cs:163)
- impact (called by): [[explorer-run|Automator.Application.Automation.PlaywrightTestExplorer.RunAsync(JsonElement,CancellationToken)]] (src/Automator.Application/Automation/PlaywrightTestExplorer.cs:177)

## Source anchors

- `src/Automator.Application/Automation/PlaywrightTestExplorer.cs#L256`
