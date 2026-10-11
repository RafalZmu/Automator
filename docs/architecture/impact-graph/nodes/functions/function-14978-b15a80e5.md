---
graph_id: "function-14978-b15a80e5"
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

# validateRpcRequest

(value: unknown) => RpcRequest

## Connections

- impact (called by): [[backend-request|BackendProcess.request]] (electron/backendProcess.ts:74)
- impact (called by): [[function-32261-5f0c9729|registerIpc.handle.callback@638]] (electron/main.ts:640)
- impact (called by): [[function-7302-9a375406|invokeBackend]] (electron/main.ts:183)
- impact (called by): [[function-7791-a3269ee7|invokeModuleBackend]] (electron/main.ts:192)
- impact (called by): [[function-8411-34b58a13|invokeHostServiceBackend]] (electron/main.ts:200)
- impact (called by): [[function-8938-bab6e19e|invokeAutomationService]] (electron/main.ts:215)

## Source anchors

- `contracts/rpc.ts#L300`
