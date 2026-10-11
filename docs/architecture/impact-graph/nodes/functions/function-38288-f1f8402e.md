---
graph_id: "function-38288-f1f8402e"
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

# withNativeDialog

<T>(action: () => Promise<T>) => Promise<T>

## Connections

- impact (called by): [[function-33549-ca4bf9c0|registerIpc.handle.callback@659]] (electron/main.ts:661)
- impact (called by): [[function-36915-be8d9731|withOpenFileDialog]] (electron/main.ts:731)
- impact (called by): [[function-37750-c0938a59|showDialogForCaller]] (electron/main.ts:751)

## Source anchors

- `electron/main.ts#L754`
