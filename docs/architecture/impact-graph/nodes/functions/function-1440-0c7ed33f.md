---
graph_id: "function-1440-0c7ed33f"
graph_layer: "function"
graph_kind: "function"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/function"
  - "impact-graph/area/contracts"
  - "impact-graph/language/typescript"
generated_by: "scripts/impact-graph.mjs"
---

# createAutomationHttpIpcFailure

(error: unknown) => Extract<AutomationHttpIpcResponse, { ok: false; }>

## Connections

- impact (called by): [[function-26630-e9a2dd8c|registerIpc.handle.callback@550]] (electron/main.ts:555)
- impact (called by): [[function-26630-e9a2dd8c|registerIpc.handle.callback@550]] (electron/main.ts:562)
- impact (called by): [[function-6309-06e709e7|automationHttpRequest]] (electron/preload.ts:102)
- impact (called by): [[function-6520-227c23d8|automationHttpRequest.catch.callback@99]] (electron/preload.ts:99)

## Source anchors

- `contracts/automationHttpIpc.ts#L46`
