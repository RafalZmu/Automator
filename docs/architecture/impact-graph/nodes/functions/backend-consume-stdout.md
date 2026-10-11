---
graph_id: "backend-consume-stdout"
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

# BackendProcess.consumeStdout

(chunk: string) => void

## Connections

- impact (stdout parsing resolves a pending request promise): [[backend-request|BackendProcess.request]] (electron/backendProcess.ts#L70-L115)
- impact (called by): [[function-1847-36925f76|BackendProcess.start.on.callback@49]] (electron/backendProcess.ts:49)

## Source anchors

- `electron/backendProcess.ts#L115`
