---
graph_id: "context-process"
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

# Automator.Application.Automation.AutomationServicesContext.ContextProcessService.ExecuteAsync(AutomationProcessRequest,CancellationToken)

public Task ExecuteAsync(AutomationProcessRequest request, CancellationToken cancellationToken)

## Connections

- impact (test discovery runs the process capability): [[explorer-discover|Automator.Application.Automation.PlaywrightTestExplorer.DiscoverTestsAsync(string,RunnerState,CancellationToken)]] (src/Automator.Application/Automation/PlaywrightTestExplorer.cs#L259)
- impact (test execution runs the process capability): [[explorer-run|Automator.Application.Automation.PlaywrightTestExplorer.RunAsync(JsonElement,CancellationToken)]] (src/Automator.Application/Automation/PlaywrightTestExplorer.cs#L228)

## Source anchors

- `src/Automator.Application/Automation/AutomationServicesContext.cs#L208`
