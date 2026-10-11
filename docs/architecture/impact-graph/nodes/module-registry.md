---
graph_id: "module-registry"
graph_layer: "module"
graph_kind: "runtime"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/runtime"
  - "impact-graph/area/runtime"
generated_by: "scripts/impact-graph.mjs"
---

# Automation module registry

Validates module/action versions and capabilities before dispatching actions to compiled modules.

## Connections

- contains (module action dispatch): [[registry-dispatch|Automator.Application.Automation.AutomationModuleRegistry.DispatchAsync(string,string,int,int,JsonElement,string,AutomationServicesContext,IReadOnlyList<ModuleSettingsEntry>,CancellationToken)]]
- impact (registered compiled module action dispatch): [[script|Script Runner]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs; src/Automator.Application/Automation/ScriptRunnerModule.cs)
- impact (registered compiled module action dispatch): [[api|API]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs; src/Automator.Application/Automation/ApiModule.cs)
- impact (registered compiled module action dispatch): [[browser|Browser Automation]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs; src/Automator.Application/Automation/BrowserAutomationModule.cs)
- impact (registered compiled module action dispatch): [[workflows|Workflows]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs; src/Automator.Application/Automation/WorkflowModule.cs)
- impact (registered compiled module action dispatch): [[scheduler|Scheduler]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs; src/Automator.Application/Automation/SchedulerModule.cs)
- impact (registered compiled module action dispatch): [[work-time|Work Time]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs; src/Automator.Application/Automation/FocusSessionsModule.cs)
- impact (registered compiled module action dispatch): [[website|Website Launcher]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs; src/Automator.Application/Automation/WebsiteLauncherModule.cs)
- contains (owns source file): [[file-automation-module-registry-2431f75f|AutomationModuleRegistry.cs]]

## Source anchors

- `src/Automator.Application/Automation/AutomationModuleRegistry.cs`
