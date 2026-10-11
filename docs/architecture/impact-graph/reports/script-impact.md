---
graph_id: "script-impact"
graph_layer: "impact-report"
graph_kind: "report"
tags:
  - "impact-graph"
  - "impact-graph/report"
  - "impact-graph/source-layer/module"
  - "impact-graph/source-kind/feature"
  - "impact-graph/area/script"
generated_by: "scripts/impact-graph.mjs"
---

# Change impact: Script Runner

Starting node: [[architecture/impact-graph/nodes/script|Script Runner]] (src/Automator.Application/Automation/ScriptRunnerModule.cs, ui/modules/ScriptRunnerView.tsx)
Traversal: owned files and functions, then dependency impact edges

Graph edges indicate possible impact propagation. The merged graph currently covers 3482 nodes; static analysis may miss runtime behavior.

## Direct dependents (21)

- [[architecture/impact-graph/nodes/functions/function-cs-32fa0481|Automator.Application.Automation.ScriptRunnerSavedProfileHandler.ExecuteAsync(string,JsonElement?,AutomationExecutionMetadata,CancellationToken)]] — called by
  Evidence: `src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:45`
- [[architecture/impact-graph/nodes/functions/function-cs-9a66600f|Automator.Application.Automation.ScriptRunnerSavedProfileHandler.ReadProfile(JsonElement)]] — called by
  Evidence: `src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:93`
- [[architecture/impact-graph/nodes/functions/function-cs-654dae28|Automator.Application.Automation.ScriptRunnerTemplateInstaller.InstallAsync(string,JsonElement,IAutomationLibrary,CancellationToken)]] — called by
  Evidence: `src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:35`
- [[architecture/impact-graph/nodes/functions/function-cs-4c6162d1|Automator.Application.Launcher.LauncherTabRegistry.CreateProviders(IAutomationVariableProvider?,string?)]] — called by
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94`
- [[architecture/impact-graph/nodes/functions/function-cs-19609f5b|Automator.Infrastructure.Automation.AutomationLibraryTransferService.NormalizeRecord(AutomationLibraryRecord)]] — called by
  Evidence: `src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:202`
- [[architecture/impact-graph/nodes/functions/function-8047-09c167ad|lambda@tests/Automator.Application.Specs/ScriptRunnerTemplateInstallSpecs.cs:81]] — called by
  Evidence: `tests/Automator.Application.Specs/ScriptRunnerTemplateInstallSpecs.cs:81`
- [[architecture/impact-graph/nodes/functions/function-8494-0b500942|lambda@tests/Automator.Application.Specs/ScriptRunnerTemplateInstallSpecs.cs:85]] — called by
  Evidence: `tests/Automator.Application.Specs/ScriptRunnerTemplateInstallSpecs.cs:85`
- [[architecture/impact-graph/nodes/files/file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] — type-reference
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94`
- [[architecture/impact-graph/nodes/functions/function-cs-f5449c57|Program.ScriptRunnerRejectsInvalidProfiles()]] — called by
  Evidence: `tests/Automator.Application.Specs/Program.cs:771`
- [[architecture/impact-graph/nodes/functions/function-cs-f949af1e|Program.ScriptRunnerReportsProcessWarnings()]] — called by
  Evidence: `tests/Automator.Application.Specs/Program.cs:803`
- [[architecture/impact-graph/nodes/functions/function-cs-a8af0d9f|Program.ScriptRunnerRunsPowerShell()]] — called by
  Evidence: `tests/Automator.Application.Specs/Program.cs:741`
- [[architecture/impact-graph/nodes/functions/function-cs-c0269aa8|Program.ScriptRunnerUsesSavedProfiles()]] — called by
  Evidence: `tests/Automator.Application.Specs/Program.cs:683`
- [[architecture/impact-graph/nodes/scheduler-coordinator|Scheduler coordinator]] — Scheduler can list, validate, and run saved Script Runner profiles directly
  Evidence: `src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L111-L114; src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L131-L135; src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L326-L330`
- [[architecture/impact-graph/nodes/files/file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] — type-reference
  Evidence: `src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:13`
- [[architecture/impact-graph/nodes/files/file-script-runner-template-catalog-bf37d81d|ScriptRunnerTemplateCatalog.cs]] — type-reference
  Evidence: `src/Automator.Application/Automation/ScriptRunnerTemplateCatalog.cs:26`
- [[architecture/impact-graph/nodes/files/file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] — type-reference
  Evidence: `src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:22`
- [[architecture/impact-graph/nodes/functions/function-cs-75ed9ff8|ScriptRunnerTemplateInstallSpecs.RunAsync()]] — called by
  Evidence: `tests/Automator.Application.Specs/ScriptRunnerTemplateInstallSpecs.cs:37`
- [[architecture/impact-graph/nodes/functions/function-cs-eda1625c|ScriptRunnerTemplateRunSpecs.RunAsync()]] — called by
  Evidence: `tests/Automator.Application.Specs/ScriptRunnerTemplateRunSpecs.cs:19`
- [[architecture/impact-graph/nodes/functions/function-cs-4af81470|ScriptRunnerTemplateSpecs.RunAsync()]] — called by
  Evidence: `tests/Automator.Application.Specs/ScriptRunnerTemplateSpecs.cs:46`
- [[architecture/impact-graph/nodes/files/file-view-registry-2cb1168b|viewRegistry.tsx]] — imports or re-exports
  Evidence: `ui/viewRegistry.tsx:7`
- [[architecture/impact-graph/nodes/workflow-engine|Workflow engine]] — workflows can execute saved Script Runner profiles as steps
  Evidence: `src/Automator.Application/Automation/AutomationWorkflowEngine.cs#L26-L70; src/Automator.Backend/BackendServer.cs#L923-L927`

## Transitive dependents (41)

- [[architecture/impact-graph/nodes/files/file-api-module-69346cf2|ApiModule.cs]] — type-reference → type-reference
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Application/Automation/ApiModule.cs:32`
- [[architecture/impact-graph/nodes/files/file-app-ed7dde6e|App.tsx]] — imports or re-exports → imports or re-exports
  Evidence: `ui/viewRegistry.tsx:7; ui/App.tsx:17`
- [[architecture/impact-graph/nodes/functions/function-cs-2f0fe1bb|Automator.Application.Launcher.LauncherTabRegistry.CreateAutomationRegistry(AutomationCapabilityRegistry,IAutomationVariableProvider?,string?)]] — called by → called by
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Application/Launcher/LauncherTabRegistry.cs:51`
- [[architecture/impact-graph/nodes/functions/function-cs-18e55154|Automator.Infrastructure.Automation.AutomationLibraryTransferService.ExportAsync(CancellationToken)]] — called by → called by
  Evidence: `src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:202; src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:67`
- [[architecture/impact-graph/nodes/functions/function-cs-bfa77545|Automator.Infrastructure.Automation.AutomationLibraryTransferService.ImportAsync(ReadOnlyMemory<byte>,CancellationToken)]] — called by → called by
  Evidence: `src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:202; src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:107`
- [[architecture/impact-graph/nodes/files/file-backend-server-c9515ea3|BackendServer.cs]] — type-reference → type-reference
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Backend/BackendServer.cs:1020`
- [[architecture/impact-graph/nodes/files/file-backend-ui-state-8ff0ced1|BackendUiState.cs]] — type-reference → type-reference
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Backend/BackendUiState.cs:20`
- [[architecture/impact-graph/nodes/files/file-browser-automation-module-fe90d00b|BrowserAutomationModule.cs]] — type-reference → type-reference
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Application/Automation/BrowserAutomationModule.cs:22`
- [[architecture/impact-graph/nodes/files/file-focus-sessions-module-46cdd4e5|FocusSessionsModule.cs]] — type-reference → type-reference
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Application/Automation/FocusSessionsModule.cs:10`
- [[architecture/impact-graph/nodes/functions/function-835-1294f3b3|lambda@src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:18]] — called by → called by
  Evidence: `src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:93; src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:20`
- [[architecture/impact-graph/nodes/scheduler|Scheduler]] — workflows can execute saved Script Runner profiles as steps → scheduled workflow runs use the workflow runner
  Evidence: `src/Automator.Application/Automation/AutomationWorkflowEngine.cs#L26-L70; src/Automator.Backend/BackendServer.cs#L923-L927; src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs; src/Automator.Application/Automation/AutomationWorkflowEngine.cs`
- [[architecture/impact-graph/nodes/files/file-scheduler-module-b921b3b7|SchedulerModule.cs]] — type-reference → type-reference
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Application/Automation/SchedulerModule.cs:11`
- [[architecture/impact-graph/nodes/files/file-website-launcher-module-395a7694|WebsiteLauncherModule.cs]] — type-reference → type-reference
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Application/Automation/WebsiteLauncherModule.cs:17`
- [[architecture/impact-graph/nodes/files/file-workflow-module-465ef471|WorkflowModule.cs]] — type-reference → type-reference
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Application/Automation/WorkflowModule.cs:13`
- [[architecture/impact-graph/nodes/workflows|Workflows]] — workflows can execute saved Script Runner profiles as steps → the Workflows module starts engine runs
  Evidence: `src/Automator.Application/Automation/AutomationWorkflowEngine.cs#L26-L70; src/Automator.Backend/BackendServer.cs#L923-L927; src/Automator.Application/Automation/WorkflowModule.cs; src/Automator.Application/Automation/AutomationWorkflowEngine.cs`
- [[architecture/impact-graph/nodes/files/file-automation-focus-session-coordinator-f3b9d175|AutomationFocusSessionCoordinator.cs]] — type-reference → type-reference → type-reference
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Application/Automation/FocusSessionsModule.cs:10; src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:100`
- [[architecture/impact-graph/nodes/functions/function-cs-84fabf81|AutomationLibraryTransferSpecs.ExportsOnlyPortableDefinitions()]] — called by → called by → called by
  Evidence: `src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:202; src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:67; tests/Automator.Infrastructure.Specs/AutomationLibraryTransferSpecs.cs:54`
- [[architecture/impact-graph/nodes/functions/function-cs-84f897f0|AutomationLibraryTransferSpecs.ImportsWithRepairWarnings()]] — called by → called by → called by
  Evidence: `src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:202; src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:107; tests/Automator.Infrastructure.Specs/AutomationLibraryTransferSpecs.cs:116`
- [[architecture/impact-graph/nodes/files/file-automation-scheduler-coordinator-5f0f022a|AutomationSchedulerCoordinator.cs]] — type-reference → type-reference → type-reference
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Application/Automation/SchedulerModule.cs:11; src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:125`
- [[architecture/impact-graph/nodes/files/file-automation-schedule-recurrence-calculator-1ae5caaa|AutomationScheduleRecurrenceCalculator.cs]] — type-reference → type-reference → type-reference
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Application/Automation/SchedulerModule.cs:11; src/Automator.Application/Automation/AutomationScheduleRecurrenceCalculator.cs:11`
- [[architecture/impact-graph/nodes/files/file-automation-work-time-coordinator-34a4d8a0|AutomationWorkTimeCoordinator.cs]] — type-reference → type-reference → type-reference
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Application/Automation/FocusSessionsModule.cs:10; src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:11`
- [[architecture/impact-graph/nodes/functions/function-cs-57b6127e|Automator.Backend.BackendServer.ExportBackupAsync(string)]] — called by → called by → called by
  Evidence: `src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:202; src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:67; src/Automator.Backend/BackendServer.cs:1313`
- [[architecture/impact-graph/nodes/functions/function-cs-30f73aac|Automator.Backend.BackendServer.ImportSettingsAsync(string)]] — called by → called by → called by
  Evidence: `src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:202; src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:107; src/Automator.Backend/BackendServer.cs:1267`
- [[architecture/impact-graph/nodes/functions/function-cs-8ad53cb2|Automator.Backend.BackendServer.InitializeAsync(InitializeParams)]] — called by → called by → called by
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Application/Launcher/LauncherTabRegistry.cs:51; src/Automator.Backend/BackendServer.cs:949`
- [[architecture/impact-graph/nodes/functions/function-9818-1196d0ad|lambda@tests/Automator.Infrastructure.Specs/AutomationLibraryTransferSpecs.cs:134]] — called by → called by → called by
  Evidence: `src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:202; src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:107; tests/Automator.Infrastructure.Specs/AutomationLibraryTransferSpecs.cs:134`
- [[architecture/impact-graph/nodes/functions/function-10269-bf96184e|lambda@tests/Automator.Infrastructure.Specs/AutomationLibraryTransferSpecs.cs:140]] — called by → called by → called by
  Evidence: `src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:202; src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:107; tests/Automator.Infrastructure.Specs/AutomationLibraryTransferSpecs.cs:140`
- [[architecture/impact-graph/nodes/functions/function-10890-d66665f1|lambda@tests/Automator.Infrastructure.Specs/AutomationLibraryTransferSpecs.cs:151]] — called by → called by → called by
  Evidence: `src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:202; src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:107; tests/Automator.Infrastructure.Specs/AutomationLibraryTransferSpecs.cs:151`
- [[architecture/impact-graph/nodes/functions/function-11168-7ec4c1c9|lambda@tests/Automator.Infrastructure.Specs/AutomationLibraryTransferSpecs.cs:155]] — called by → called by → called by
  Evidence: `src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:202; src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:107; tests/Automator.Infrastructure.Specs/AutomationLibraryTransferSpecs.cs:155`
- [[architecture/impact-graph/nodes/functions/function-11826-938ac521|lambda@tests/Automator.Infrastructure.Specs/AutomationLibraryTransferSpecs.cs:164]] — called by → called by → called by
  Evidence: `src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:202; src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:107; tests/Automator.Infrastructure.Specs/AutomationLibraryTransferSpecs.cs:164`
- [[architecture/impact-graph/nodes/functions/function-12205-0d2a628b|lambda@tests/Automator.Infrastructure.Specs/AutomationLibraryTransferSpecs.cs:172]] — called by → called by → called by
  Evidence: `src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:202; src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:107; tests/Automator.Infrastructure.Specs/AutomationLibraryTransferSpecs.cs:172`
- [[architecture/impact-graph/nodes/functions/function-12784-9686d509|lambda@tests/Automator.Infrastructure.Specs/AutomationLibraryTransferSpecs.cs:178]] — called by → called by → called by
  Evidence: `src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:202; src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:107; tests/Automator.Infrastructure.Specs/AutomationLibraryTransferSpecs.cs:178`
- [[architecture/impact-graph/nodes/files/file-main-a39e3ca6|main.tsx]] — imports or re-exports → imports or re-exports → imports or re-exports
  Evidence: `ui/viewRegistry.tsx:7; ui/App.tsx:17; ui/main.tsx:3`
- [[architecture/impact-graph/nodes/files/file-program-835ecfee|Program.cs]] — type-reference → type-reference → type-reference
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Backend/BackendServer.cs:1020; src/Automator.Backend/Program.cs:7`
- [[architecture/impact-graph/nodes/functions/function-cs-b8cc281c|Program.TabRegistryPublishesVersionedSlots()]] — called by → called by → called by
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Application/Launcher/LauncherTabRegistry.cs:51; tests/Automator.Application.Specs/Program.cs:315`
- [[architecture/impact-graph/nodes/functions/scheduler-view|SchedulerView]] — workflows can execute saved Script Runner profiles as steps → scheduled workflow runs use the workflow runner → the renderer consumes schedule actions and snapshots
  Evidence: `src/Automator.Application/Automation/AutomationWorkflowEngine.cs#L26-L70; src/Automator.Backend/BackendServer.cs#L923-L927; src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs; src/Automator.Application/Automation/AutomationWorkflowEngine.cs; src/Automator.Application/Automation/SchedulerModule.cs; ui/modules/SchedulerView.tsx`
- [[architecture/impact-graph/nodes/functions/workflows-view|WorkflowsView]] — workflows can execute saved Script Runner profiles as steps → the Workflows module starts engine runs → the renderer consumes workflow actions and run results
  Evidence: `src/Automator.Application/Automation/AutomationWorkflowEngine.cs#L26-L70; src/Automator.Backend/BackendServer.cs#L923-L927; src/Automator.Application/Automation/WorkflowModule.cs; src/Automator.Application/Automation/AutomationWorkflowEngine.cs; src/Automator.Application/Automation/WorkflowModule.cs; ui/modules/WorkflowsView.tsx`
- [[architecture/impact-graph/nodes/functions/backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] — called by → called by → called by → called by
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Application/Launcher/LauncherTabRegistry.cs:51; src/Automator.Backend/BackendServer.cs:949; src/Automator.Backend/BackendServer.cs:188`
- [[architecture/impact-graph/nodes/functions/backend-handle-core|Automator.Backend.BackendServer.HandleLineCoreAsync(string)]] — called by → called by → called by → called by → the request handler consumes the dispatch result
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Application/Launcher/LauncherTabRegistry.cs:51; src/Automator.Backend/BackendServer.cs:949; src/Automator.Backend/BackendServer.cs:188; src/Automator.Backend/BackendServer.cs#L158`
- [[architecture/impact-graph/nodes/functions/backend-handle-line|Automator.Backend.BackendServer.HandleLineAsync(string)]] — called by → called by → called by → called by → the request handler consumes the dispatch result → queued or direct line handling awaits the core request path
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Application/Launcher/LauncherTabRegistry.cs:51; src/Automator.Backend/BackendServer.cs:949; src/Automator.Backend/BackendServer.cs:188; src/Automator.Backend/BackendServer.cs#L158; src/Automator.Backend/BackendServer.cs#L122-L136`
- [[architecture/impact-graph/nodes/functions/function-6128-e1ed8c5a|lambda@src/Automator.Backend/BackendServer.cs:133]] — called by → called by → called by → called by → the request handler consumes the dispatch result → called by
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Application/Launcher/LauncherTabRegistry.cs:51; src/Automator.Backend/BackendServer.cs:949; src/Automator.Backend/BackendServer.cs:188; src/Automator.Backend/BackendServer.cs#L158; src/Automator.Backend/BackendServer.cs:135`
- [[architecture/impact-graph/nodes/functions/backend-run|Automator.Backend.BackendServer.RunAsync()]] — called by → called by → called by → called by → the request handler consumes the dispatch result → queued or direct line handling awaits the core request path → the stdin loop hands each request line to HandleLineAsync
  Evidence: `src/Automator.Application/Launcher/LauncherTabRegistry.cs:94; src/Automator.Application/Launcher/LauncherTabRegistry.cs:51; src/Automator.Backend/BackendServer.cs:949; src/Automator.Backend/BackendServer.cs:188; src/Automator.Backend/BackendServer.cs#L158; src/Automator.Backend/BackendServer.cs#L122-L136; src/Automator.Backend/BackendServer.cs#L88-L92`
