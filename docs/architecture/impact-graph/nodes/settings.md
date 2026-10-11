---
graph_id: "settings"
graph_layer: "module"
graph_kind: "adapter"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/adapter"
  - "impact-graph/area/infrastructure"
generated_by: "scripts/impact-graph.mjs"
---

# Settings store

Stores preferences and versioned module settings separately from the automation library.

## Connections

- impact (preferences and variables expose settings-backed state): [[options|Options and Variables]] (src/Automator.Backend/BackendServer.cs; ui/variables/GlobalVariables.tsx)
- impact (module settings configure Script Runner behavior): [[script|Script Runner]] (src/Automator.Application/Automation/ScriptRunnerModule.cs; src/Automator.Backend/BackendServer.cs)
- impact (module settings configure API behavior): [[api|API]] (src/Automator.Application/Automation/ApiModule.cs; src/Automator.Backend/BackendServer.cs)
- impact (module settings configure Browser Automation behavior): [[browser|Browser Automation]] (src/Automator.Application/Automation/BrowserAutomationModule.cs; src/Automator.Backend/BackendServer.cs)
- impact (module settings configure workflow behavior): [[workflows|Workflows]] (src/Automator.Application/Automation/WorkflowModule.cs; src/Automator.Backend/BackendServer.cs)
- impact (module settings configure scheduler behavior): [[scheduler|Scheduler]] (src/Automator.Application/Automation/SchedulerModule.cs; src/Automator.Backend/BackendServer.cs)
- impact (module settings configure Work Time behavior): [[work-time|Work Time]] (src/Automator.Application/Automation/FocusSessionsModule.cs; src/Automator.Backend/BackendServer.cs)
- impact (module settings configure Website Launcher behavior): [[website|Website Launcher]] (src/Automator.Application/Automation/WebsiteLauncherModule.cs; src/Automator.Backend/BackendServer.cs)

## Source anchors

- `src/Automator.Windows/WindowsSettingsStore.cs`
- `src/Automator.Backend/BackendServer.cs`
