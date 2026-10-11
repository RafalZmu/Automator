---
graph_id: "backend-context"
graph_layer: "module"
graph_kind: "shared-service"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/shared-service"
  - "impact-graph/area/shared-services"
generated_by: "scripts/impact-graph.mjs"
---

# Backend capability context

Wraps host-granted services with module identity, capabilities, and cancellation lifetime.

## Connections

- contains (scoped process wrapper): [[context-process|Automator.Application.Automation.AutomationServicesContext.ContextProcessService.ExecuteAsync(AutomationProcessRequest,CancellationToken)]]
- impact (active module actions receive a scoped service context): [[script|Script Runner]] (src/Automator.Backend/BackendServer.cs#L731; src/Automator.Application/Automation/AutomationServicesContext.cs)
- impact (active module actions receive a scoped service context): [[api|API]] (src/Automator.Backend/BackendServer.cs#L731; src/Automator.Application/Automation/AutomationServicesContext.cs)
- impact (active module actions receive a scoped service context): [[browser|Browser Automation]] (src/Automator.Backend/BackendServer.cs#L731; src/Automator.Application/Automation/AutomationServicesContext.cs)
- impact (active module actions receive a scoped service context): [[workflows|Workflows]] (src/Automator.Backend/BackendServer.cs#L731; src/Automator.Application/Automation/AutomationServicesContext.cs)
- impact (active module actions receive a scoped service context): [[scheduler|Scheduler]] (src/Automator.Backend/BackendServer.cs#L731; src/Automator.Application/Automation/AutomationServicesContext.cs)
- impact (active module actions receive a scoped service context): [[work-time|Work Time]] (src/Automator.Backend/BackendServer.cs#L731; src/Automator.Application/Automation/AutomationServicesContext.cs)
- impact (active module actions receive a scoped service context): [[website|Website Launcher]] (src/Automator.Backend/BackendServer.cs#L731; src/Automator.Application/Automation/AutomationServicesContext.cs)
- contains (owns source file): [[file-automation-api-contracts-fefb5a52|AutomationApiContracts.cs]]
- contains (owns source file): [[file-automation-browser-contracts-e99a31e6|AutomationBrowserContracts.cs]]
- contains (owns source file): [[file-automation-capability-registry-6cceea15|AutomationCapabilityRegistry.cs]]
- contains (owns source file): [[file-automation-focus-contracts-f64e9320|AutomationFocusContracts.cs]]
- contains (owns source file): [[file-automation-focus-session-coordinator-f3b9d175|AutomationFocusSessionCoordinator.cs]]
- contains (owns source file): [[file-automation-keyboard-event-hub-6cbb478d|AutomationKeyboardEventHub.cs]]
- contains (owns source file): [[file-automation-library-store-7f40b921|AutomationLibraryStore.cs]]
- contains (owns source file): [[file-automation-request-executor-ee35da0c|AutomationRequestExecutor.cs]]
- contains (owns source file): [[file-automation-saved-profile-execution-5951044c|AutomationSavedProfileExecution.cs]]
- contains (owns source file): [[file-automation-scheduler-contracts-9eda5e8f|AutomationSchedulerContracts.cs]]
- contains (owns source file): [[file-automation-schedule-recurrence-calculator-1ae5caaa|AutomationScheduleRecurrenceCalculator.cs]]
- contains (owns source file): [[file-automation-service-authorization-39aef482|AutomationServiceAuthorization.cs]]
- contains (owns source file): [[file-automation-services-context-7c5c05bd|AutomationServicesContext.cs]]
- contains (owns source file): [[file-automation-variable-service-13e259f8|AutomationVariableService.cs]]
- contains (owns source file): [[file-automation-workflow-contracts-df155b9a|AutomationWorkflowContracts.cs]]
- contains (owns source file): [[file-automation-work-time-contracts-1575187b|AutomationWorkTimeContracts.cs]]
- contains (owns source file): [[file-playwright-test-explorer-1b38acd6|PlaywrightTestExplorer.cs]]
- contains (owns source file): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]]
- contains (owns source file): [[file-script-runner-template-catalog-bf37d81d|ScriptRunnerTemplateCatalog.cs]]
- contains (owns source file): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]]
- contains (owns source file): [[file-alias-launch-scheduler-201f5afa|AliasLaunchScheduler.cs]]
- contains (owns source file): [[file-hotkey-recording-policy-004ab323|HotkeyRecordingPolicy.cs]]
- contains (owns source file): [[file-launcher-mode-coordinator-9dbe8734|LauncherModeCoordinator.cs]]
- contains (owns source file): [[file-launcher-session-80f755ef|LauncherSession.cs]]
- contains (owns source file): [[file-navigation-key-policy-b2e03bd1|NavigationKeyPolicy.cs]]
- contains (owns source file): [[file-serialized-command-queue-fed3b7f8|SerializedCommandQueue.cs]]
- contains (owns source file): [[file-iapplication-log-bd7a00da|IApplicationLog.cs]]
- contains (owns source file): [[file-automation-capability-567bd200|AutomationCapability.cs]]
- contains (owns source file): [[file-automation-http-contracts-a72f0d04|AutomationHttpContracts.cs]]
- contains (owns source file): [[file-automation-process-contracts-ba8d153f|AutomationProcessContracts.cs]]
- contains (owns source file): [[file-key-input-event-a3922b90|KeyInputEvent.cs]]
- contains (owns source file): [[file-launcher-settings-0dc27d4e|LauncherSettings.cs]]
- contains (owns source file): [[file-hotkey-key-name-validator-ff5791e3|HotkeyKeyNameValidator.cs]]
- contains (owns source file): [[file-hotkey-tap-recognizer-b6636929|HotkeyTapRecognizer.cs]]
- contains (owns source file): [[file-hotkey-virtual-key-mapper-3a4a469e|HotkeyVirtualKeyMapper.cs]]
- contains (owns source file): [[file-launcher-domain-b0c442ae|LauncherDomain.cs]]
- contains (owns source file): [[file-automation-result-acbb0512|AutomationResult.cs]]
- contains (owns source file): [[file-automation-api-profile-runner-5fd1dcea|AutomationApiProfileRunner.cs]]
- contains (owns source file): [[file-automation-library-transfer-service-6afe8174|AutomationLibraryTransferService.cs]]
- contains (owns source file): [[file-browser-automation-proxy-f27cfe26|BrowserAutomationProxy.cs]]
- contains (owns source file): [[file-host-address-resolver-f8f5a331|HostAddressResolver.cs]]
- contains (owns source file): [[file-in-memory-automation-secret-manager-3a9cf48b|InMemoryAutomationSecretManager.cs]]
- contains (owns source file): [[file-program-58a35e31|Program.cs]]
- contains (owns source file): [[file-run-activity-specs-71c76b0a|RunActivitySpecs.cs]]
- contains (owns source file): [[file-script-runner-template-install-specs-93dbc2be|ScriptRunnerTemplateInstallSpecs.cs]]
- contains (owns source file): [[file-script-runner-template-run-specs-088f9e3f|ScriptRunnerTemplateRunSpecs.cs]]
- contains (owns source file): [[file-script-runner-template-specs-d82f1140|ScriptRunnerTemplateSpecs.cs]]
- contains (owns source file): [[file-program-68666576|Program.cs]]
- contains (owns source file): [[file-automation-library-transfer-specs-a04e6851|AutomationLibraryTransferSpecs.cs]]
- contains (owns source file): [[file-program-33216e44|Program.cs]]

## Source anchors

- `src/Automator.Application/Automation/AutomationServicesContext.cs`
