---
graph_id: "library"
graph_layer: "module"
graph_kind: "adapter"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/adapter"
  - "impact-graph/area/infrastructure"
generated_by: "scripts/impact-graph.mjs"
---

# Automation library store

Persists profiles, variables, activity records, schedules, tags, and work-time data in SQLite.

## Connections

- impact (run activity records are persisted in the shared SQLite library): [[activity|Run Activity and Notifications]] (src/Automator.Application/Automation/AutomationRunActivityService.cs; src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs)
- impact (saved scripts and profile metadata use library storage): [[script|Script Runner]] (src/Automator.Application/Automation/ScriptRunnerModule.cs; src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs)
- impact (API profiles use library storage): [[api|API]] (src/Automator.Application/Automation/ApiModule.cs; src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs)
- impact (Browser Automation tags and project metadata use scoped library storage): [[browser|Browser Automation]] (src/Automator.Application/Automation/BrowserAutomationModule.cs; src/Automator.Application/Automation/PlaywrightTestExplorer.cs)
- impact (workflow profiles and run metadata use library storage): [[workflows|Workflows]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs)
- impact (schedules and run metadata use library storage): [[scheduler|Scheduler]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs)
- impact (work-time entries are persisted in the shared library): [[work-time|Work Time]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs)
- contains (owns source file): [[file-sqlite-automation-library-store-b1950327|SqliteAutomationLibraryStore.cs]]

## Source anchors

- `src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs`
