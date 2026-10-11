---
graph_id: "backend-request"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/desktop"
  - "impact-graph/language/typescript"
generated_by: "scripts/impact-graph.mjs"
---

# BackendProcess.request

(method: RpcMethod, params: JsonObject) => Promise<unknown>

## Connections

- impact (the main IPC handler awaits the backend request result): [[main-module-handler|registerIpc.handle.callback@570]] (electron/main.ts#L570-L572)
- impact (called by): [[function-10500-b0a5d35c|applyBackendState]] (electron/main.ts:258)
- impact (called by): [[function-1373-ec38c6f0|BackendProcess.start]] (electron/backendProcess.ts:63)
- impact (called by): [[function-21209-fc3dca4f|registerIpc.handle.callback@450]] (electron/main.ts:456)
- impact (called by): [[function-35760-7443413b|registerIpc.finally.callback@697]] (electron/main.ts:710)
- impact (called by): [[function-39031-b2a504f6|showLauncher]] (electron/main.ts:792)
- impact (called by): [[function-41204-bd2f0544|requestPrimaryControlFocus]] (electron/main.ts:863)
- impact (called by): [[function-45361-31f262b0|verifyNativeForeground]] (electron/main.ts:923)
- impact (called by): [[function-46288-b674615c|updateWindowContext]] (electron/main.ts:934)
- impact (called by): [[function-46927-40299552|closeLauncher]] (electron/main.ts:953)
- impact (called by): [[function-57345-31131077|createWindow.on.callback@1165]] (electron/main.ts:1167)
- impact (called by): [[function-7302-9a375406|invokeBackend]] (electron/main.ts:184)
- impact (called by): [[function-7791-a3269ee7|invokeModuleBackend]] (electron/main.ts:193)
- impact (called by): [[function-8411-34b58a13|invokeHostServiceBackend]] (electron/main.ts:201)
- impact (called by): [[function-8938-bab6e19e|invokeAutomationService]] (electron/main.ts:230)

## Source anchors

- `electron/backendProcess.ts#L70`
