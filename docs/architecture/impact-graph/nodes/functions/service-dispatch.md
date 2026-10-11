---
graph_id: "service-dispatch"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/renderer-services"
  - "impact-graph/language/typescript"
generated_by: "scripts/impact-graph.mjs"
---

# createAutomationServices.dispatch

(actionId: string, input: JSONType, options?: {}) => Promise<{ contractVersion: 1; status: "error" | "information" | "success" | "warning"; message: string; data: JSONType; actions: { ...; }[]; }>

## Connections

- impact (the view awaits and parses the returned action result): [[view-run|BrowserAutomationView.run]] (ui/modules/BrowserAutomationView.tsx#L235-L238)

## Source anchors

- `ui/automationServices.ts#L181`
