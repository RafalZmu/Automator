---
graph_id: "function-cs-989df30a"
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

# Automator.Application.Automation.IAutomationLibraryStore.GetAsync(string,string,string,CancellationToken)

Task GetAsync(string moduleId, string collection, string id, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-20febace|Automator.Application.Automation.AutomationWorkTimeCoordinator.InitializeAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:46)
- impact (called by): [[function-cs-2d99adb2|Automator.Application.Automation.AutomationVariableService.ReadAndMigrateAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationVariableService.cs:71)
- impact (called by): [[function-cs-2ed359b5|Automator.Infrastructure.Automation.AutomationApiProfileRunner.RunProfileAsync(string,JsonElement?,CancellationToken)]] (src/Automator.Infrastructure/Automation/AutomationApiProfileRunner.cs:30)
- impact (called by): [[function-cs-32fa0481|Automator.Application.Automation.ScriptRunnerSavedProfileHandler.ExecuteAsync(string,JsonElement?,AutomationExecutionMetadata,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:33)
- impact (called by): [[function-cs-3a2e47b2|Automator.Infrastructure.Automation.PlaywrightAutomationBrowserService.RequireProfileAsync(string,CancellationToken)]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:193)
- impact (called by): [[function-cs-3bc99e91|Automator.Application.Automation.BrowserSavedProfileHandler.ExecuteAsync(string,JsonElement?,AutomationExecutionMetadata,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:522)
- impact (called by): [[function-cs-6948733d|Automator.Application.Automation.AutomationFocusSessionCoordinator.LoadSettingsAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:334)
- impact (called by): [[function-cs-788d91e6|Automator.Application.Automation.AutomationWorkflowEngine.LoadWorkflowAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:192)
- impact (called by): [[function-cs-87fdc7c1|Automator.Application.Automation.AutomationFocusSessionCoordinator.RecoverCurrentSessionAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:357)
- impact (called by): [[function-cs-880c7664|Automator.Application.Automation.AutomationSchedulerCoordinator.RecoverClaimsAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:392)
- impact (called by): [[function-cs-98f45dba|Automator.Application.Automation.AutomationServicesContext.ContextLibrary.GetAsync(string,string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:188)
- impact (called by): [[function-cs-a07a153b|Automator.Infrastructure.Automation.PlaywrightAutomationBrowserService.WatchProfilePolicyAsync(string,BrowserAutomationProxy,CancellationToken)]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:321)
- impact (called by): [[function-cs-bfa77545|Automator.Infrastructure.Automation.AutomationLibraryTransferService.ImportAsync(ReadOnlyMemory<byte>,CancellationToken)]] (src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:118)
- impact (called by): [[function-cs-c406b5d1|Automator.Application.Automation.ApiSavedProfileHandler.ExecuteAsync(string,JsonElement?,AutomationExecutionMetadata,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:577)
- impact (called by): [[function-cs-c7c40973|Automator.Application.Automation.AutomationSchedulerCoordinator.ReadScheduleAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:418)
- impact (called by): [[function-cs-d58668eb|Automator.Application.Automation.AutomationSchedulerCoordinator.ReadCursorAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:457)
- impact (called by): [[function-cs-db1673b5|Automator.Application.Automation.AutomationSchedulerCoordinator.RunScheduleCoreAsync(AutomationScheduleDefinition,DateTimeOffset,bool,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:300)
- impact (called by): [[function-cs-db1673b5|Automator.Application.Automation.AutomationSchedulerCoordinator.RunScheduleCoreAsync(AutomationScheduleDefinition,DateTimeOffset,bool,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:301)

## Source anchors

- `src/Automator.Application/Automation/AutomationLibraryStore.cs#L18`
