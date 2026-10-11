---
graph_id: "api"
graph_layer: "module"
graph_kind: "feature"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/feature"
  - "impact-graph/area/api"
generated_by: "scripts/impact-graph.mjs"
---

# API

Runs saved HTTP profiles through shared network policy and secret resolution.

## Connections

- impact (the renderer consumes API actions and results): [[api-view|ApiView]] (src/Automator.Application/Automation/ApiModule.cs; ui/modules/ApiView.tsx)
- impact (workflows can execute saved API profiles as steps): [[workflow-engine|Workflow engine]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs#L26-L70; src/Automator.Backend/BackendServer.cs#L923-L927)
- contains (owns source file): [[file-api-module-69346cf2|ApiModule.cs]]
- contains (owns source file): [[file-api-view-f157b445|ApiView.tsx]]

## Source anchors

- `src/Automator.Application/Automation/ApiModule.cs`
- `src/Automator.Infrastructure/Automation/SharedHttpService.cs`
- `ui/modules/ApiView.tsx`
