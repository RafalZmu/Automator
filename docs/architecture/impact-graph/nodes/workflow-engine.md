---
graph_id: "workflow-engine"
graph_layer: "module"
graph_kind: "runtime"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/runtime"
  - "impact-graph/area/runtime"
generated_by: "scripts/impact-graph.mjs"
---

# Workflow engine

Resolves workflow steps and runs saved profiles through scoped services.

## Connections

- impact (the Workflows module starts engine runs): [[workflows|Workflows]] (src/Automator.Application/Automation/WorkflowModule.cs; src/Automator.Application/Automation/AutomationWorkflowEngine.cs)
- impact (scheduled workflow runs use the workflow runner): [[scheduler|Scheduler]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs; src/Automator.Application/Automation/AutomationWorkflowEngine.cs)
- contains (owns source file): [[file-automation-workflow-engine-92eda817|AutomationWorkflowEngine.cs]]

## Source anchors

- `src/Automator.Application/Automation/AutomationWorkflowEngine.cs`
