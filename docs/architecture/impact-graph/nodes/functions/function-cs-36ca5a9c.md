---
graph_id: "function-cs-36ca5a9c"
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

# Automator.Application.Automation.AutomationServicesContext.Link(AutomationServicesContext,CancellationToken)

private static CancellationTokenSource Link(AutomationServicesContext context, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-0898c152|Automator.Application.Automation.AutomationServicesContext.ContextLibrary.UpsertAsync(string,string,int,JsonElement,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:193)
- impact (called by): [[function-cs-0fdb7fc3|Automator.Application.Automation.AutomationServicesContext.ContextFocusCoordinator.SaveSettingsAsync(AutomationFocusSettings,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:325)
- impact (called by): [[function-cs-10ac7464|Automator.Application.Automation.AutomationServicesContext.ContextFocusCoordinator.EndAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:355)
- impact (called by): [[function-cs-17046cda|Automator.Application.Automation.AutomationServicesContext.ContextApiProfileRunner.RunProfileAsync(string,JsonElement?,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:241)
- impact (called by): [[function-cs-1b7d95c5|Automator.Application.Automation.AutomationServicesContext.ContextBrowserService.GetRuntimeStatusAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:250)
- impact (called by): [[function-cs-1d18f52e|Automator.Application.Automation.AutomationServicesContext.ContextFocusCoordinator.ResumeAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:343)
- impact (called by): [[function-cs-23533d85|Automator.Application.Automation.AutomationServicesContext.ContextWorkflowRunner.RunAsync(string,JsonElement?,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:277)
- impact (called by): [[function-cs-2ecbf29d|Automator.Application.Automation.AutomationServicesContext.ContextSchedulerCoordinator.SaveAsync(AutomationScheduleDefinition,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:292)
- impact (called by): [[function-cs-3f816fcb|Automator.Application.Automation.AutomationServicesContext.ContextSecretManager.SetAsync(string,string,string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:220)
- impact (called by): [[function-cs-4954a330|Automator.Application.Automation.AutomationServicesContext.ContextLibrary.ListAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:181)
- impact (called by): [[function-cs-4aa4d901|Automator.Application.Automation.AutomationServicesContext.ContextSchedulerCoordinator.DeleteAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:298)
- impact (called by): [[function-cs-58f8d0dc|Automator.Application.Automation.AutomationServicesContext.ContextFocusCoordinator.StartAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:331)
- impact (called by): [[function-cs-591409c3|Automator.Application.Automation.AutomationServicesContext.ContextFocusCoordinator.GetSnapshotAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:319)
- impact (called by): [[function-cs-63b1fc8a|Automator.Application.Automation.AutomationServicesContext.ContextWorkTimeCoordinator.SaveEntryAsync(string,IReadOnlyList<string>,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:382)
- impact (called by): [[function-cs-6997db1a|Automator.Application.Automation.AutomationServicesContext.ContextSecretManager.ExistsAsync(string,string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:226)
- impact (called by): [[function-cs-6c4c06ab|Automator.Application.Automation.AutomationServicesContext.ContextWorkTimeCoordinator.DeleteEntryAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:394)
- impact (called by): [[function-cs-7742fe65|Automator.Application.Automation.AutomationServicesContext.ContextWorkflowRunner.ListSavedProfilesAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:271)
- impact (called by): [[function-cs-84c04236|Automator.Application.Automation.AutomationServicesContext.ContextSchedulerCoordinator.GetSnapshotAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:286)
- impact (called by): [[function-cs-91770645|Automator.Application.Automation.AutomationServicesContext.ContextWorkTimeCoordinator.UpdateEntryAsync(string,string,IReadOnlyList<string>,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:388)
- impact (called by): [[function-cs-92c7d9e3|Automator.Application.Automation.AutomationServicesContext.ContextWorkTimeCoordinator.ResumePendingAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:400)
- impact (called by): [[context-process|Automator.Application.Automation.AutomationServicesContext.ContextProcessService.ExecuteAsync(AutomationProcessRequest,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:211)
- impact (called by): [[function-cs-97c1f5b7|Automator.Application.Automation.AutomationServicesContext.ContextBrowserService.CloseSessionAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:262)
- impact (called by): [[function-cs-98f45dba|Automator.Application.Automation.AutomationServicesContext.ContextLibrary.GetAsync(string,string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:187)
- impact (called by): [[function-cs-b4cc9dc5|Automator.Application.Automation.AutomationServicesContext.ContextSecretManager.DeleteAsync(string,string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:232)
- impact (called by): [[function-cs-b59d7abf|Automator.Application.Automation.AutomationServicesContext.ContextLibrary.DeleteAsync(string,string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:200)
- impact (called by): [[function-cs-ba7a205c|Automator.Application.Automation.AutomationServicesContext.ContextFocusCoordinator.SkipAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:349)
- impact (called by): [[function-cs-c20211fb|Automator.Application.Automation.AutomationServicesContext.ContextFocusCoordinator.PauseAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:337)
- impact (called by): [[function-cs-c57cd836|Automator.Application.Automation.AutomationServicesContext.ContextBrowserService.ExecuteAsync(string,AutomationBrowserAction,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:256)
- impact (called by): [[function-cs-c67974de|Automator.Application.Automation.AutomationServicesContext.ContextWorkTimeCoordinator.DiscardPendingAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:406)
- impact (called by): [[function-cs-d493d04a|Automator.Application.Automation.AutomationServicesContext.ContextWorkTimeCoordinator.StopAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:376)
- impact (called by): [[function-cs-e322f9ea|Automator.Application.Automation.AutomationServicesContext.ContextSchedulerCoordinator.RunNowAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:310)
- impact (called by): [[function-cs-ea358af5|Automator.Application.Automation.AutomationServicesContext.ContextWebsiteLauncher.LaunchAsync(IReadOnlyList<IReadOnlyList<Uri>>,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:416)
- impact (called by): [[function-cs-f0e7da14|Automator.Application.Automation.AutomationServicesContext.ContextWorkTimeCoordinator.GetSnapshotAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:364)
- impact (called by): [[function-cs-f6be4d0d|Automator.Application.Automation.AutomationServicesContext.ContextWorkTimeCoordinator.StartAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:370)
- impact (called by): [[function-cs-fa231359|Automator.Application.Automation.AutomationServicesContext.ContextHttpClient.SendAsync(AutomationHttpRequest,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:172)
- impact (called by): [[function-cs-fd9cd7e0|Automator.Application.Automation.AutomationServicesContext.ContextSchedulerCoordinator.SetEnabledAsync(string,bool,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:304)

## Source anchors

- `src/Automator.Application/Automation/AutomationServicesContext.cs#L98`
