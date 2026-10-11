---
graph_id: "function-cs-c9994c60"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/activity"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Automation.AutomationRunActivityService.IsObservedAction(string,string)

public static bool IsObservedAction(string moduleId, string actionId)

## Connections

- impact (called by): [[function-23518-a8c83fb0|lambda@src/Automator.Backend/BackendServer.cs:429]] (src/Automator.Backend/BackendServer.cs:431)
- impact (called by): [[function-cs-665cd8f0|Automator.Application.Automation.AutomationRunActivityService.RecordAsync(AutomationRunActivityEntry,CancellationToken)]] (src/Automator.Application/Automation/AutomationRunActivityService.cs:41)
- impact (called by): [[function-cs-ce68fef5|Automator.Application.Automation.AutomationRunActivityService.GetSnapshotAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationRunActivityService.cs:64)

## Source anchors

- `src/Automator.Application/Automation/AutomationRunActivityService.cs#L25`
