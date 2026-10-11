---
graph_id: "function-4914-d28bbc21"
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

# getWindowHandleHex

(window: BrowserWindow) => string

## Connections

- impact (called by): [[function-21209-fc3dca4f|registerIpc.handle.callback@450]] (electron/main.ts:454)
- impact (called by): [[function-41204-bd2f0544|requestPrimaryControlFocus]] (electron/main.ts:872)
- impact (called by): [[function-41204-bd2f0544|requestPrimaryControlFocus]] (electron/main.ts:889)
- impact (called by): [[function-41204-bd2f0544|requestPrimaryControlFocus]] (electron/main.ts:902)
- impact (called by): [[function-45361-31f262b0|verifyNativeForeground]] (electron/main.ts:919)
- impact (called by): [[function-46288-b674615c|updateWindowContext]] (electron/main.ts:935)
- impact (called by): [[function-47392-17d7f975|maybeLogReady]] (electron/main.ts:973)
- impact (called by): [[function-48737-f52a0e66|createWorkspaceWindow]] (electron/main.ts:1058)
- impact (called by): [[function-57345-31131077|createWindow.on.callback@1165]] (electron/main.ts:1166)

## Source anchors

- `electron/main.ts#L121`
