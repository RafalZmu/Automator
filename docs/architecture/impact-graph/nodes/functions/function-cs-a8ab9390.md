---
graph_id: "function-cs-a8ab9390"
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

# Automator.Application.Automation.IAutomationWorkTimeCoordinator.StopAsync(CancellationToken)

Task StopAsync(CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-0ff7e025|Program.WorkTimeRequiresDescriptionAndStoresTags()]] (tests/Automator.Focus.Specs/Program.cs:257)
- impact (called by): [[function-cs-5ae3f6f0|Program.ActiveWorkTimeSurvivesRestart()]] (tests/Automator.Focus.Specs/Program.cs:339)
- impact (called by): [[function-cs-b4965e3b|Program.WorkTimeEntriesCanBeEditedAndDeleted()]] (tests/Automator.Focus.Specs/Program.cs:303)
- impact (called by): [[function-cs-cb8fbfdc|Automator.Application.Automation.FocusSessionsModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/FocusSessionsModule.cs:79)
- impact (called by): [[function-cs-d493d04a|Automator.Application.Automation.AutomationServicesContext.ContextWorkTimeCoordinator.StopAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:377)
- impact (called by): [[function-cs-e707ba34|Program.WorkTimePendingCanResumeAndDiscard()]] (tests/Automator.Focus.Specs/Program.cs:281)
- impact (called by): [[function-cs-e707ba34|Program.WorkTimePendingCanResumeAndDiscard()]] (tests/Automator.Focus.Specs/Program.cs:286)

## Source anchors

- `src/Automator.Application/Automation/AutomationWorkTimeContracts.cs#L27`
