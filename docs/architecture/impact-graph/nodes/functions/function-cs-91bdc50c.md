---
graph_id: "function-cs-91bdc50c"
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

# Automator.Application.Automation.IAutomationLibraryStore.DeleteAsync(string,string,string,CancellationToken)

Task DeleteAsync(string moduleId, string collection, string id, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-0f26fb22|Automator.Application.Automation.AutomationFocusSessionCoordinator.EndAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:238)
- impact (called by): [[function-cs-29453081|Automator.Application.Automation.AutomationWorkTimeCoordinator.DeleteEntryAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:195)
- impact (called by): [[function-cs-38f8b515|Automator.Application.Automation.AutomationSchedulerCoordinator.DeleteAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:167)
- impact (called by): [[function-cs-38f8b515|Automator.Application.Automation.AutomationSchedulerCoordinator.DeleteAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:168)
- impact (called by): [[function-cs-5211ef65|Automator.Application.Automation.AutomationWorkflowEngine.PersistHistoryAsync(AutomationWorkflowProfile,AutomationWorkflowRunResult,DateTimeOffset,DateTimeOffset,AutomationExecutionOrigin,string,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:237)
- impact (called by): [[function-cs-58974c42|Automator.Application.Automation.AutomationWorkTimeCoordinator.PruneHistoryAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:271)
- impact (called by): [[function-cs-665cd8f0|Automator.Application.Automation.AutomationRunActivityService.RecordAsync(AutomationRunActivityEntry,CancellationToken)]] (src/Automator.Application/Automation/AutomationRunActivityService.cs:50)
- impact (called by): [[function-cs-880c7664|Automator.Application.Automation.AutomationSchedulerCoordinator.RecoverClaimsAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:388)
- impact (called by): [[function-cs-880c7664|Automator.Application.Automation.AutomationSchedulerCoordinator.RecoverClaimsAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:402)
- impact (called by): [[function-cs-afeef435|Automator.Application.Automation.AutomationSchedulerCoordinator.PruneHistoryAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:485)
- impact (called by): [[function-cs-b12a8229|Automator.Application.Automation.AutomationSchedulerCoordinator.CompleteRunAsync(AutomationScheduleHistoryEntry,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:366)
- impact (called by): [[function-cs-b59d7abf|Automator.Application.Automation.AutomationServicesContext.ContextLibrary.DeleteAsync(string,string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:201)
- impact (called by): [[function-cs-d0669245|Automator.Application.Automation.AutomationSchedulerCoordinator.RebaselineCursorAsync(AutomationScheduleDefinition,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:478)
- impact (called by): [[function-cs-f7577e69|Automator.Application.Automation.AutomationFocusSessionCoordinator.PruneHistoryAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:404)

## Source anchors

- `src/Automator.Application/Automation/AutomationLibraryStore.cs#L20`
