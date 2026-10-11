---
graph_id: "file-automation-variable-service-13e259f8"
graph_layer: "file"
graph_kind: "csharp"
tags:
  - "impact-graph"
  - "impact-graph/file"
  - "impact-graph/kind/csharp"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# AutomationVariableService.cs

Source file src/Automator.Application/Automation/AutomationVariableService.cs.

## Connections

- contains (declares function): [[function-cs-3f2c66da|Automator.Application.Automation.IAutomationVariableProvider.GetAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationVariableService.cs#L13)
- contains (declares function): [[function-cs-d2615b68|Automator.Application.Automation.AutomationVariableService.GetAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationVariableService.cs#L26)
- contains (declares function): [[function-cs-cd26a675|Automator.Application.Automation.AutomationVariableService.SetAsync(IReadOnlyDictionary<string, JsonElement>,CancellationToken)]] (src/Automator.Application/Automation/AutomationVariableService.cs#L33)
- contains (declares function): [[function-cs-2cdc215a|Automator.Application.Automation.AutomationVariableService.Validate(IReadOnlyDictionary<string, JsonElement>)]] (src/Automator.Application/Automation/AutomationVariableService.cs#L45)
- contains (declares function): [[function-cs-ca6679c5|Automator.Application.Automation.AutomationVariableService.ValidateReferences(IReadOnlyDictionary<string, string>?)]] (src/Automator.Application/Automation/AutomationVariableService.cs#L59)
- contains (declares function): [[function-cs-2d99adb2|Automator.Application.Automation.AutomationVariableService.ReadAndMigrateAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationVariableService.cs#L69)
- contains (declares function): [[function-cs-5fe4fafe|Automator.Application.Automation.AutomationVariableService.SaveAsync(IReadOnlyDictionary<string, JsonElement>,CancellationToken)]] (src/Automator.Application/Automation/AutomationVariableService.cs#L110)
- contains (declares function): [[function-cs-32094b98|Automator.Application.Automation.AutomationVariableInterpolation.Expand(string,IReadOnlyDictionary<string, JsonElement>)]] (src/Automator.Application/Automation/AutomationVariableService.cs#L117)
- contains (declares function): [[function-cs-33d53e7f|Automator.Application.Automation.AutomationVariableInterpolation.Append(string)]] (src/Automator.Application/Automation/AutomationVariableService.cs#L122)
- impact (type-reference): [[file-automation-workflow-engine-92eda817|AutomationWorkflowEngine.cs]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:134)
- impact (type-reference): [[file-automation-workflow-engine-92eda817|AutomationWorkflowEngine.cs]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:136)
- impact (type-reference): [[file-automation-workflow-engine-92eda817|AutomationWorkflowEngine.cs]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:17)
- impact (type-reference): [[file-automation-workflow-engine-92eda817|AutomationWorkflowEngine.cs]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:280)
- impact (type-reference): [[file-automation-workflow-engine-92eda817|AutomationWorkflowEngine.cs]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:435)
- impact (type-reference): [[file-automation-workflow-engine-92eda817|AutomationWorkflowEngine.cs]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:442)
- impact (type-reference): [[file-automation-workflow-engine-92eda817|AutomationWorkflowEngine.cs]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:444)
- impact (type-reference): [[file-automation-workflow-engine-92eda817|AutomationWorkflowEngine.cs]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:445)
- impact (type-reference): [[file-automation-workflow-engine-92eda817|AutomationWorkflowEngine.cs]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:447)
- impact (type-reference): [[file-automation-workflow-engine-92eda817|AutomationWorkflowEngine.cs]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:47)
- impact (type-reference): [[file-automation-workflow-engine-92eda817|AutomationWorkflowEngine.cs]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:49)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:1278)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:1312)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:489)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:49)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:492)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:495)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:500)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:502)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:898)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:915)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:925)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:927)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:929)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:949)
- impact (type-reference): [[file-browser-automation-module-fe90d00b|BrowserAutomationModule.cs]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:203)
- impact (type-reference): [[file-browser-automation-module-fe90d00b|BrowserAutomationModule.cs]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:205)
- impact (type-reference): [[file-browser-automation-module-fe90d00b|BrowserAutomationModule.cs]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:206)
- impact (type-reference): [[file-browser-automation-module-fe90d00b|BrowserAutomationModule.cs]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:24)
- impact (type-reference): [[file-browser-automation-module-fe90d00b|BrowserAutomationModule.cs]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:25)
- impact (type-reference): [[file-browser-automation-module-fe90d00b|BrowserAutomationModule.cs]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:495)
- impact (type-reference): [[file-browser-automation-module-fe90d00b|BrowserAutomationModule.cs]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:531)
- impact (type-reference): [[file-browser-automation-module-fe90d00b|BrowserAutomationModule.cs]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:533)
- impact (type-reference): [[file-browser-automation-module-fe90d00b|BrowserAutomationModule.cs]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:534)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:49)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:51)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:80)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:94)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:97)
- impact (type-reference): [[file-script-runner-module-9e82267a|ScriptRunnerModule.cs]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:187)
- impact (type-reference): [[file-script-runner-module-9e82267a|ScriptRunnerModule.cs]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:189)
- impact (type-reference): [[file-script-runner-module-9e82267a|ScriptRunnerModule.cs]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:190)
- impact (type-reference): [[file-script-runner-module-9e82267a|ScriptRunnerModule.cs]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:41)
- impact (type-reference): [[file-script-runner-module-9e82267a|ScriptRunnerModule.cs]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:43)
- impact (type-reference): [[file-script-runner-module-9e82267a|ScriptRunnerModule.cs]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:45)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:11)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:43)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:44)
- impact (type-reference): [[file-workflow-module-465ef471|WorkflowModule.cs]] (src/Automator.Application/Automation/WorkflowModule.cs:248)

## Source anchors

- `src/Automator.Application/Automation/AutomationVariableService.cs`
