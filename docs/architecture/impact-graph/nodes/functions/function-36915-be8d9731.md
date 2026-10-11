---
graph_id: "function-36915-be8d9731"
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

# withOpenFileDialog

(event: IpcMainInvokeEvent, filters: Electron.FileFilter[], method: RpcMethod, parameters: (filePath: string) => Record<string, unknown>) => Promise<unknown>

## Connections

- impact (called by): [[function-33117-55a5fa0f|registerIpc.handle.callback@653]] (electron/main.ts:653)
- impact (called by): [[function-33341-9dc86f78|registerIpc.handle.callback@656]] (electron/main.ts:656)
- impact (called by): [[function-35367-3d6ecf56|registerIpc.handle.callback@691]] (electron/main.ts:691)

## Source anchors

- `electron/main.ts#L724`
