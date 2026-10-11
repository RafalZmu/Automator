---
graph_id: "function-7791-a3269ee7"
graph_layer: "function"
graph_kind: "function"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/function"
  - "impact-graph/area/desktop"
  - "impact-graph/language/typescript"
generated_by: "scripts/impact-graph.mjs"
---

# invokeModuleBackend

(event: IpcMainInvokeEvent, method: RpcMethod, params?: Record<string, unknown>) => Promise<unknown>

## Connections

- impact (called by): [[main-module-handler|registerIpc.handle.callback@570]] (electron/main.ts:571)
- impact (called by): [[function-28074-1c259e17|registerIpc.handle.callback@576]] (electron/main.ts:577)
- impact (called by): [[function-31125-fb536bb6|registerIpc.handle.callback@620]] (electron/main.ts:621)
- impact (called by): [[function-31530-af94a583|registerIpc.handle.callback@626]] (electron/main.ts:627)

## Source anchors

- `electron/main.ts#L187`
