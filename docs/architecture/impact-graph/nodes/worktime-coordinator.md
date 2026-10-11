---
graph_id: "worktime-coordinator"
graph_layer: "module"
graph_kind: "runtime"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/runtime"
  - "impact-graph/area/runtime"
generated_by: "scripts/impact-graph.mjs"
---

# Work Time coordinator

Owns work-time session state and persisted summaries.

## Connections

- impact (Work Time actions read and update coordinator state): [[work-time|Work Time]] (src/Automator.Application/Automation/FocusSessionsModule.cs; src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs)
- contains (owns source file): [[file-automation-work-time-coordinator-34a4d8a0|AutomationWorkTimeCoordinator.cs]]

## Source anchors

- `src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs`
