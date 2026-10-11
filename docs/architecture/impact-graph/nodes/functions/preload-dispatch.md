---
graph_id: "preload-dispatch"
graph_layer: "function"
graph_kind: "arrow"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/arrow"
  - "impact-graph/area/desktop"
  - "impact-graph/language/typescript"
generated_by: "scripts/impact-graph.mjs"
---

# dispatchModuleAction

(request: Parameters<AutomatorBridge['dispatchModuleAction']>[0]) => Promise<{ contractVersion: 1; status: "error" | "information" | "success" | "warning"; message: string; data: JSONType; actions: { ...; }[]; }>

## Connections

- impact (the module action facade awaits the exposed bridge method): [[service-dispatch|createAutomationServices.dispatch]] (ui/automationServices.ts#L189; electron/preload.ts#L114-L117)

## Source anchors

- `electron/preload.ts#L114`
