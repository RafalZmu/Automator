---
graph_id: "function-cs-24e42ed0"
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

# Automator.Application.Automation.IAutomationWorkTimeCoordinator.UpdateEntryAsync(string,string,IReadOnlyList<string>,CancellationToken)

Task UpdateEntryAsync(string id, string description, IReadOnlyList tags, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-587646e6|Automator.Application.Automation.FocusSessionsModule.UpdateEntryAsync(IAutomationWorkTimeCoordinator,JsonElement,CancellationToken)]] (src/Automator.Application/Automation/FocusSessionsModule.cs:117)
- impact (called by): [[function-cs-91770645|Automator.Application.Automation.AutomationServicesContext.ContextWorkTimeCoordinator.UpdateEntryAsync(string,string,IReadOnlyList<string>,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:389)
- impact (called by): [[function-cs-b4965e3b|Program.WorkTimeEntriesCanBeEditedAndDeleted()]] (tests/Automator.Focus.Specs/Program.cs:308)

## Source anchors

- `src/Automator.Application/Automation/AutomationWorkTimeContracts.cs#L29`
