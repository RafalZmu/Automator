---
graph_id: "function-cs-94f0ef18"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/desktop"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Services.NativeMethods.PeekMessage(Message,nint,uint,uint,uint)

internal static extern bool PeekMessage(Message message, nint window, uint minimum, uint maximum, uint remove)

## Connections

- impact (called by): [[function-cs-bee7c0b4|Automator.Services.KeyboardHookService.HookThread()]] (src/Automator.App/Services/KeyboardHookService.cs:90)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L103`
