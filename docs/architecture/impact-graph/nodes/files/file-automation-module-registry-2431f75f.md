---
graph_id: "file-automation-module-registry-2431f75f"
graph_layer: "file"
graph_kind: "csharp"
tags:
  - "impact-graph"
  - "impact-graph/file"
  - "impact-graph/kind/csharp"
  - "impact-graph/area/module-registry"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# AutomationModuleRegistry.cs

Source file src/Automator.Application/Automation/AutomationModuleRegistry.cs.

## Connections

- contains (declares function): [[function-cs-3e5e1c63|Automator.Application.Automation.IAutomationModule.CreateDefaultSettings()]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L41)
- contains (declares function): [[function-cs-37e3767e|Automator.Application.Automation.IAutomationModule.MigrateSettings(int,JsonElement)]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L44)
- contains (declares function): [[function-cs-12c332c2|Automator.Application.Automation.IAutomationModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L46)
- contains (declares function): [[function-cs-2c7f5dd0|Automator.Application.Automation.AutomationModuleRegistry.AutomationModuleRegistry(IEnumerable<IAutomationModule>,AutomationCapabilityRegistry)]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L62)
- contains (declares function): [[function-2886-ff513e4a|lambda@src/Automator.Application/Automation/AutomationModuleRegistry.cs:68]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L68)
- contains (declares function): [[function-2983-d50bfabf|lambda@src/Automator.Application/Automation/AutomationModuleRegistry.cs:69]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L69)
- contains (declares function): [[function-3020-984bbc55|lambda@src/Automator.Application/Automation/AutomationModuleRegistry.cs:69]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L69)
- contains (declares function): [[registry-dispatch|Automator.Application.Automation.AutomationModuleRegistry.DispatchAsync(string,string,int,int,JsonElement,string,AutomationServicesContext,IReadOnlyList<ModuleSettingsEntry>,CancellationToken)]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L74)
- contains (declares function): [[function-4431-aa77ba29|lambda@src/Automator.Application/Automation/AutomationModuleRegistry.cs:95]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L95)
- contains (declares function): [[function-5140-5d78c997|lambda@src/Automator.Application/Automation/AutomationModuleRegistry.cs:104]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L104)
- contains (declares function): [[function-cs-b5dd0d53|Automator.Application.Automation.AutomationModuleRegistry.MigrateModuleSettings(IReadOnlyList<ModuleSettingsEntry>)]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L119)
- contains (declares function): [[function-cs-7ef4ad68|Automator.Application.Automation.AutomationModuleRegistry.GetSettings(string,IReadOnlyList<ModuleSettingsEntry>)]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L148)
- contains (declares function): [[function-7836-fc41224b|lambda@src/Automator.Application/Automation/AutomationModuleRegistry.cs:152]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L152)
- contains (declares function): [[function-cs-cf21966d|Automator.Application.Automation.AutomationModuleRegistry.GetActiveSettings(string,int,int,string,IReadOnlyList<ModuleSettingsEntry>)]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L158)
- contains (declares function): [[function-cs-5b45c4de|Automator.Application.Automation.AutomationModuleRegistry.CreateUpdatedModuleSettings(string,int,int,JsonElement,string,IReadOnlyList<ModuleSettingsEntry>)]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L178)
- contains (declares function): [[function-10701-81b1f54a|lambda@src/Automator.Application/Automation/AutomationModuleRegistry.cs:199]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L199)
- contains (declares function): [[function-10799-034aea4d|lambda@src/Automator.Application/Automation/AutomationModuleRegistry.cs:200]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L200)
- contains (declares function): [[function-cs-6a22f085|Automator.Application.Automation.AutomationModuleRegistry.Validate(IReadOnlyList<IAutomationModule>)]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L206)
- contains (declares function): [[function-13869-e9f304ac|lambda@src/Automator.Application/Automation/AutomationModuleRegistry.cs:238]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L238)
- contains (declares function): [[function-cs-3570abe6|Automator.Application.Automation.AutomationModuleRegistry.ValidateResult(AutomationResult)]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L244)
- impact (type-reference): [[file-api-module-69346cf2|ApiModule.cs]] (src/Automator.Application/Automation/ApiModule.cs:53)
- impact (type-reference): [[file-api-module-69346cf2|ApiModule.cs]] (src/Automator.Application/Automation/ApiModule.cs:72)
- impact (type-reference): [[file-api-module-69346cf2|ApiModule.cs]] (src/Automator.Application/Automation/ApiModule.cs:73)
- impact (type-reference): [[file-api-module-69346cf2|ApiModule.cs]] (src/Automator.Application/Automation/ApiModule.cs:74)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:1022)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:1026)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:420)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:437)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:52)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:579)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:583)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:600)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:613)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:617)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:621)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:719)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:721)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:722)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:724)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:725)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:726)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:728)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:949)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:959)
- impact (type-reference): [[file-browser-automation-module-fe90d00b|BrowserAutomationModule.cs]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:40)
- impact (type-reference): [[file-browser-automation-module-fe90d00b|BrowserAutomationModule.cs]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:67)
- impact (type-reference): [[file-browser-automation-module-fe90d00b|BrowserAutomationModule.cs]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:68)
- impact (type-reference): [[file-browser-automation-module-fe90d00b|BrowserAutomationModule.cs]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:69)
- impact (type-reference): [[file-focus-sessions-module-46cdd4e5|FocusSessionsModule.cs]] (src/Automator.Application/Automation/FocusSessionsModule.cs:18)
- impact (type-reference): [[file-focus-sessions-module-46cdd4e5|FocusSessionsModule.cs]] (src/Automator.Application/Automation/FocusSessionsModule.cs:39)
- impact (type-reference): [[file-focus-sessions-module-46cdd4e5|FocusSessionsModule.cs]] (src/Automator.Application/Automation/FocusSessionsModule.cs:40)
- impact (type-reference): [[file-focus-sessions-module-46cdd4e5|FocusSessionsModule.cs]] (src/Automator.Application/Automation/FocusSessionsModule.cs:41)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:107)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:114)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:117)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:118)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:119)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:120)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:32)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:47)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:56)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:57)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:58)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:65)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:67)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:68)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:69)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:70)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:71)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:72)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:73)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:74)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:75)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:76)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:77)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:86)
- impact (type-reference): [[file-scheduler-module-b921b3b7|SchedulerModule.cs]] (src/Automator.Application/Automation/SchedulerModule.cs:17)
- impact (type-reference): [[file-scheduler-module-b921b3b7|SchedulerModule.cs]] (src/Automator.Application/Automation/SchedulerModule.cs:20)
- impact (type-reference): [[file-scheduler-module-b921b3b7|SchedulerModule.cs]] (src/Automator.Application/Automation/SchedulerModule.cs:21)
- impact (type-reference): [[file-scheduler-module-b921b3b7|SchedulerModule.cs]] (src/Automator.Application/Automation/SchedulerModule.cs:22)
- impact (type-reference): [[file-script-runner-module-9e82267a|ScriptRunnerModule.cs]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:57)
- impact (type-reference): [[file-script-runner-module-9e82267a|ScriptRunnerModule.cs]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:76)
- impact (type-reference): [[file-script-runner-module-9e82267a|ScriptRunnerModule.cs]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:77)
- impact (type-reference): [[file-script-runner-module-9e82267a|ScriptRunnerModule.cs]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:78)
- impact (type-reference): [[file-website-launcher-module-395a7694|WebsiteLauncherModule.cs]] (src/Automator.Application/Automation/WebsiteLauncherModule.cs:29)
- impact (type-reference): [[file-website-launcher-module-395a7694|WebsiteLauncherModule.cs]] (src/Automator.Application/Automation/WebsiteLauncherModule.cs:31)
- impact (type-reference): [[file-website-launcher-module-395a7694|WebsiteLauncherModule.cs]] (src/Automator.Application/Automation/WebsiteLauncherModule.cs:32)
- impact (type-reference): [[file-website-launcher-module-395a7694|WebsiteLauncherModule.cs]] (src/Automator.Application/Automation/WebsiteLauncherModule.cs:33)
- impact (type-reference): [[file-workflow-module-465ef471|WorkflowModule.cs]] (src/Automator.Application/Automation/WorkflowModule.cs:35)
- impact (type-reference): [[file-workflow-module-465ef471|WorkflowModule.cs]] (src/Automator.Application/Automation/WorkflowModule.cs:54)
- impact (type-reference): [[file-workflow-module-465ef471|WorkflowModule.cs]] (src/Automator.Application/Automation/WorkflowModule.cs:55)
- impact (type-reference): [[file-workflow-module-465ef471|WorkflowModule.cs]] (src/Automator.Application/Automation/WorkflowModule.cs:56)

## Source anchors

- `src/Automator.Application/Automation/AutomationModuleRegistry.cs`
