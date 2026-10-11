---
graph_id: "function-cs-da1c21e5"
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

# Automator.Application.Automation.IAutomationWorkTimeCoordinator.StartAsync(CancellationToken)

Task StartAsync(CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-0ff7e025|Program.WorkTimeRequiresDescriptionAndStoresTags()]] (tests/Automator.Focus.Specs/Program.cs:255)
- impact (called by): [[function-cs-5ae3f6f0|Program.ActiveWorkTimeSurvivesRestart()]] (tests/Automator.Focus.Specs/Program.cs:331)
- impact (called by): [[function-cs-b4965e3b|Program.WorkTimeEntriesCanBeEditedAndDeleted()]] (tests/Automator.Focus.Specs/Program.cs:301)
- impact (called by): [[function-cs-cb8fbfdc|Automator.Application.Automation.FocusSessionsModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/FocusSessionsModule.cs:78)
- impact (called by): [[function-cs-e707ba34|Program.WorkTimePendingCanResumeAndDiscard()]] (tests/Automator.Focus.Specs/Program.cs:279)
- impact (called by): [[function-cs-f6be4d0d|Automator.Application.Automation.AutomationServicesContext.ContextWorkTimeCoordinator.StartAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:371)

## Source anchors

- `src/Automator.Application/Automation/AutomationWorkTimeContracts.cs#L26`
