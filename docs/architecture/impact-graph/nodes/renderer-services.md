---
graph_id: "renderer-services"
graph_layer: "module"
graph_kind: "shared-service"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/shared-service"
  - "impact-graph/area/shared-services"
generated_by: "scripts/impact-graph.mjs"
---

# Renderer action facade

Creates typed, module-scoped action clients used by compiled module views.

## Connections

- contains (module action facade): [[service-dispatch|createAutomationServices.dispatch]]
- impact (the view uses the shared module action facade): [[script|Script Runner]] (ui/automationServices.ts; ui/modules/ScriptRunnerView.tsx)
- impact (the view uses the shared module action facade): [[api|API]] (ui/automationServices.ts; ui/modules/ApiView.tsx)
- impact (the view uses the shared module action facade): [[browser|Browser Automation]] (ui/automationServices.ts; ui/modules/BrowserAutomationView.tsx)
- impact (the view uses the shared module action facade): [[workflows|Workflows]] (ui/automationServices.ts; ui/modules/WorkflowsView.tsx)
- impact (the view uses the shared module action facade): [[scheduler|Scheduler]] (ui/automationServices.ts; ui/modules/SchedulerView.tsx)
- impact (the view uses the shared module action facade): [[work-time|Work Time]] (ui/automationServices.ts; ui/modules/FocusSessionsView.tsx)
- impact (the view uses the shared module action facade): [[website|Website Launcher]] (ui/automationServices.ts; ui/modules/WebsiteLauncherView.tsx)
- contains (owns source file): [[file-automation-services-0537dbd3|automationServices.ts]]

## Source anchors

- `ui/automationServices.ts`
