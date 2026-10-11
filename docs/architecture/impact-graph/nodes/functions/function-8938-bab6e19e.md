---
graph_id: "function-8938-bab6e19e"
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

# invokeAutomationService

(event: IpcMainInvokeEvent, method: Extract<RpcMethod, 'automation/keyboardEligibility' | 'automation/keyboardSubscribe' | 'automation/keyboardUnsubscribe' | 'automation/httpRequest' | 'automation/httpCancel'>, params: Record<string, unknown>) => Promise<unknown> | unknown

## Connections

- impact (called by): [[function-25316-e4b6af16|registerIpc.handle.callback@534]] (electron/main.ts:535)
- impact (called by): [[function-25751-9ffa6af5|registerIpc.handle.callback@540]] (electron/main.ts:541)
- impact (called by): [[function-26190-4b79f12b|registerIpc.handle.callback@545]] (electron/main.ts:546)
- impact (called by): [[function-26630-e9a2dd8c|registerIpc.handle.callback@550]] (electron/main.ts:552)
- impact (called by): [[function-27283-2a4a646b|registerIpc.handle.callback@565]] (electron/main.ts:566)

## Source anchors

- `electron/main.ts#L204`
