---
graph_id: "work-time"
graph_layer: "module"
graph_kind: "feature"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/feature"
  - "impact-graph/area/work-time"
generated_by: "scripts/impact-graph.mjs"
---

# Work Time

Records work sessions and exposes summaries and CSV export.

## Connections

- impact (the renderer consumes Work Time actions and snapshots): [[work-time-view|FocusSessionsView]] (src/Automator.Application/Automation/FocusSessionsModule.cs; ui/modules/FocusSessionsView.tsx)
- contains (owns source file): [[file-focus-sessions-module-46cdd4e5|FocusSessionsModule.cs]]
- contains (owns source file): [[file-program-2fc0b4f3|Program.cs]]
- contains (owns source file): [[file-focus-sessions-view-dff540b0|FocusSessionsView.tsx]]

## Source anchors

- `src/Automator.Application/Automation/FocusSessionsModule.cs`
- `src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs`
- `ui/modules/FocusSessionsView.tsx`
