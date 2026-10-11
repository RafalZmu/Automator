---
graph_id: "function-cs-586d18f3"
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

# Automator.Application.Automation.AutomationFocusSessionCoordinator.WriteRecordAsync(string,string,T,CancellationToken)

private Task WriteRecordAsync(string collection, string id, T value, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-1319513e|Automator.Application.Automation.AutomationFocusSessionCoordinator.SaveSettingsAsync(AutomationFocusSettings,CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:106)
- impact (called by): [[function-cs-429c4826|Automator.Application.Automation.AutomationFocusSessionCoordinator.PersistSessionAsync(AutomationFocusSessionSnapshot,DateTimeOffset,CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:412)
- impact (called by): [[function-cs-6948733d|Automator.Application.Automation.AutomationFocusSessionCoordinator.LoadSettingsAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:338)
- impact (called by): [[function-cs-daa1bc80|Automator.Application.Automation.AutomationFocusSessionCoordinator.WriteHistoryAsync(AutomationFocusSessionHistoryEntry,CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:391)

## Source anchors

- `src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs#L415`
