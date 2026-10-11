---
graph_id: "function-843-4a1c6a79"
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

# listen

<T>(channel: string, listener: (value: T) => void) => () => void

## Connections

- impact (called by): [[function-10602-a5a6f5a8|onStateChanged]] (electron/preload.ts:163)
- impact (called by): [[function-10753-6927ba99|onFocusPrimaryControl]] (electron/preload.ts:164)
- impact (called by): [[function-10912-f3853943|onBackendFailure]] (electron/preload.ts:165)
- impact (called by): [[function-11054-a1d4408e|onHotkeyRecorded]] (electron/preload.ts:166)
- impact (called by): [[function-11349-85faae50|onAutomationKeyboardAvailability]] (electron/preload.ts:169)
- impact (called by): [[function-11710-23165094|onAutomationKeyboardInput]] (electron/preload.ts:173)
- impact (called by): [[function-12052-1cb55a0b|onRunActivityChanged]] (electron/preload.ts:177)
- impact (called by): [[function-12367-6aa1523b|onGlobalVariablesChanged]] (electron/preload.ts:181)

## Source anchors

- `electron/preload.ts#L24`
