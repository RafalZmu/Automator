---
graph_id: "function-10500-b0a5d35c"
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

# applyBackendState

(value: unknown, generation?: number, presentOnOpen?: boolean) => BackendUiState | undefined

## Connections

- impact (called by): [[function-12777-dad0afe3|startBackend]] (electron/main.ts:354)
- impact (called by): [[function-15337-a011c05a|startBackend.on.callback@316]] (electron/main.ts:318)
- impact (called by): [[function-23798-e40b9cf9|registerIpc.handle.callback@503]] (electron/main.ts:506)
- impact (called by): [[function-24143-f1c4c16c|registerIpc.handle.callback@511]] (electron/main.ts:518)
- impact (called by): [[function-35760-7443413b|registerIpc.finally.callback@697]] (electron/main.ts:712)
- impact (called by): [[function-36915-be8d9731|withOpenFileDialog]] (electron/main.ts:739)
- impact (called by): [[function-39031-b2a504f6|showLauncher]] (electron/main.ts:794)
- impact (called by): [[function-46927-40299552|closeLauncher]] (electron/main.ts:954)

## Source anchors

- `electron/main.ts#L233`
