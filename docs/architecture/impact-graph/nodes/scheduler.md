---
graph_id: "scheduler"
graph_layer: "module"
graph_kind: "feature"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/feature"
  - "impact-graph/area/scheduler"
generated_by: "scripts/impact-graph.mjs"
---

# Scheduler

Persists schedules and starts saved scripts or workflows when due.

## Connections

- impact (the renderer consumes schedule actions and snapshots): [[scheduler-view|SchedulerView]] (src/Automator.Application/Automation/SchedulerModule.cs; ui/modules/SchedulerView.tsx)
- contains (owns source file): [[file-scheduler-module-b921b3b7|SchedulerModule.cs]]
- contains (owns source file): [[file-program-03ef3c69|Program.cs]]
- contains (owns source file): [[file-scheduler-view-7750ca54|SchedulerView.tsx]]

## Source anchors

- `src/Automator.Application/Automation/SchedulerModule.cs`
- `src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs`
- `ui/modules/SchedulerView.tsx`
