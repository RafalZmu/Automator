---
graph_id: "function-cs-79cd09a4"
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

# Automator.Application.Automation.AutomationSavedProfileExecutor.ListProfilesAsync(string,CancellationToken)

public Task ListProfilesAsync(string moduleId, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-64180-b9436f47|lambda@tests/Automator.Application.Specs/Program.cs:1091]] (tests/Automator.Application.Specs/Program.cs:1091)
- impact (called by): [[function-cs-874970c5|Program.SavedProfileCatalogsExposeSafeMetadata()]] (tests/Automator.Application.Specs/Program.cs:1087)
- impact (called by): [[function-cs-bd5641b2|Automator.Application.Automation.AutomationSchedulerCoordinator.SaveAsync(AutomationScheduleDefinition,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:134)
- impact (called by): [[function-cs-d3affc57|Automator.Application.Automation.AutomationWorkflowEngine.ListSavedProfilesAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:37)
- impact (called by): [[function-cs-ecdcd86d|Automator.Application.Automation.AutomationSchedulerCoordinator.GetSnapshotAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:113)

## Source anchors

- `src/Automator.Application/Automation/AutomationSavedProfileExecution.cs#L92`
