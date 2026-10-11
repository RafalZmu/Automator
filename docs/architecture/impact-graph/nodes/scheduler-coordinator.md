---
graph_id: "scheduler-coordinator"
graph_layer: "module"
graph_kind: "runtime"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/runtime"
  - "impact-graph/area/runtime"
generated_by: "scripts/impact-graph.mjs"
---

# Scheduler coordinator

Owns schedule state and due-run coordination.

## Connections

- impact (Scheduler actions read and update coordinator state): [[scheduler|Scheduler]] (src/Automator.Application/Automation/SchedulerModule.cs; src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs)
- contains (owns source file): [[file-automation-scheduler-coordinator-5f0f022a|AutomationSchedulerCoordinator.cs]]

## Source anchors

- `src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs`
