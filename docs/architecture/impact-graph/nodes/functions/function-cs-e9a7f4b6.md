---
graph_id: "function-cs-e9a7f4b6"
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

# Automator.Application.Automation.IAutomationWorkTimeCoordinator.SaveEntryAsync(string,IReadOnlyList<string>,CancellationToken)

Task SaveEntryAsync(string description, IReadOnlyList tags, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-14364-bd375d2d|lambda@tests/Automator.Focus.Specs/Program.cs:261]] (tests/Automator.Focus.Specs/Program.cs:261)
- impact (called by): [[function-cs-0ff7e025|Program.WorkTimeRequiresDescriptionAndStoresTags()]] (tests/Automator.Focus.Specs/Program.cs:263)
- impact (called by): [[function-cs-63b1fc8a|Automator.Application.Automation.AutomationServicesContext.ContextWorkTimeCoordinator.SaveEntryAsync(string,IReadOnlyList<string>,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:383)
- impact (called by): [[function-cs-b4965e3b|Program.WorkTimeEntriesCanBeEditedAndDeleted()]] (tests/Automator.Focus.Specs/Program.cs:304)
- impact (called by): [[function-cs-bc621fae|Automator.Application.Automation.FocusSessionsModule.SaveEntryAsync(IAutomationWorkTimeCoordinator,JsonElement,CancellationToken)]] (src/Automator.Application/Automation/FocusSessionsModule.cs:106)

## Source anchors

- `src/Automator.Application/Automation/AutomationWorkTimeContracts.cs#L28`
