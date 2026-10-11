---
graph_id: "process-read-bounded"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/process"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Infrastructure.Automation.LocalProcessExecutionService.ReadBoundedAsync(StreamReader)

private static Task ReadBoundedAsync(StreamReader reader)

## Connections

- impact (process output capture is used by process execution): [[process-execute|Automator.Infrastructure.Automation.LocalProcessExecutionService.ExecuteAsync(AutomationProcessRequest,CancellationToken)]] (src/Automator.Infrastructure/Automation/LocalProcessExecutionService.cs#L37)
- impact (called by): [[process-execute|Automator.Infrastructure.Automation.LocalProcessExecutionService.ExecuteAsync(AutomationProcessRequest,CancellationToken)]] (src/Automator.Infrastructure/Automation/LocalProcessExecutionService.cs:37)
- impact (called by): [[process-execute|Automator.Infrastructure.Automation.LocalProcessExecutionService.ExecuteAsync(AutomationProcessRequest,CancellationToken)]] (src/Automator.Infrastructure/Automation/LocalProcessExecutionService.cs:38)

## Source anchors

- `src/Automator.Infrastructure/Automation/LocalProcessExecutionService.cs#L73`
