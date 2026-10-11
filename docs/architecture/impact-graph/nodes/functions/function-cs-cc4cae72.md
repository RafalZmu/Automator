---
graph_id: "function-cs-cc4cae72"
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

# Automator.Application.Automation.IAutomationWorkTimeCoordinator.DeleteEntryAsync(string,CancellationToken)

Task DeleteEntryAsync(string id, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-17790-6514fd87|lambda@tests/Automator.Focus.Specs/Program.cs:321]] (tests/Automator.Focus.Specs/Program.cs:321)
- impact (called by): [[function-cs-334570ae|Automator.Application.Automation.FocusSessionsModule.DeleteEntryAsync(IAutomationWorkTimeCoordinator,JsonElement,CancellationToken)]] (src/Automator.Application/Automation/FocusSessionsModule.cs:127)
- impact (called by): [[function-cs-6c4c06ab|Automator.Application.Automation.AutomationServicesContext.ContextWorkTimeCoordinator.DeleteEntryAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:395)
- impact (called by): [[function-cs-b4965e3b|Program.WorkTimeEntriesCanBeEditedAndDeleted()]] (tests/Automator.Focus.Specs/Program.cs:318)

## Source anchors

- `src/Automator.Application/Automation/AutomationWorkTimeContracts.cs#L30`
