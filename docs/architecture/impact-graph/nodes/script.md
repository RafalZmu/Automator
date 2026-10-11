---
graph_id: "script"
graph_layer: "module"
graph_kind: "feature"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/feature"
  - "impact-graph/area/script"
generated_by: "scripts/impact-graph.mjs"
---

# Script Runner

Runs saved scripts and trusted templates through the host-granted process service.

## Connections

- impact (the renderer consumes Script Runner actions and results): [[script-view|ScriptRunnerView]] (src/Automator.Application/Automation/ScriptRunnerModule.cs; ui/modules/ScriptRunnerView.tsx)
- impact (workflows can execute saved Script Runner profiles as steps): [[workflow-engine|Workflow engine]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs#L26-L70; src/Automator.Backend/BackendServer.cs#L923-L927)
- impact (Scheduler can list, validate, and run saved Script Runner profiles directly): [[scheduler-coordinator|Scheduler coordinator]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L111-L114; src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L131-L135; src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L326-L330)
- contains (owns source file): [[file-script-runner-module-9e82267a|ScriptRunnerModule.cs]]
- contains (owns source file): [[file-script-runner-view-8288ea15|ScriptRunnerView.tsx]]

## Source anchors

- `src/Automator.Application/Automation/ScriptRunnerModule.cs`
- `ui/modules/ScriptRunnerView.tsx`
