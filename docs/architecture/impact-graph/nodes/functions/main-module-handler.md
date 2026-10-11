---
graph_id: "main-module-handler"
graph_layer: "function"
graph_kind: "callback"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/callback"
  - "impact-graph/area/desktop"
  - "impact-graph/language/typescript"
generated_by: "scripts/impact-graph.mjs"
---

# registerIpc.handle.callback@570

(event: Electron.IpcMainInvokeEvent, params: Record<string, unknown>) => Promise<{ contractVersion: 1; status: "error" | "information" | "success" | "warning"; message: string; data: JSONType; actions: { ...; }[]; }>

## Connections

- impact (the preload invocation resolves from the main IPC handler): [[preload-dispatch|dispatchModuleAction]] (electron/preload.ts#L114-L117; electron/main.ts#L570-L572)

## Source anchors

- `electron/main.ts#L570`
