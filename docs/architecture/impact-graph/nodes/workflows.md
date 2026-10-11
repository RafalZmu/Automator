---
graph_id: "workflows"
graph_layer: "module"
graph_kind: "feature"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/feature"
  - "impact-graph/area/workflows"
generated_by: "scripts/impact-graph.mjs"
---

# Workflows

Composes saved Script, API, and Browser profiles into workflow runs.

## Connections

- impact (the renderer consumes workflow actions and run results): [[workflows-view|WorkflowsView]] (src/Automator.Application/Automation/WorkflowModule.cs; ui/modules/WorkflowsView.tsx)
- contains (owns source file): [[file-workflow-module-465ef471|WorkflowModule.cs]]
- contains (owns source file): [[file-program-c9948b54|Program.cs]]
- contains (owns source file): [[file-workflows-view-842ac07a|WorkflowsView.tsx]]

## Source anchors

- `src/Automator.Application/Automation/WorkflowModule.cs`
- `src/Automator.Application/Automation/AutomationWorkflowEngine.cs`
- `ui/modules/WorkflowsView.tsx`
