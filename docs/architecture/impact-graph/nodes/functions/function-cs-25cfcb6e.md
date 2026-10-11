---
graph_id: "function-cs-25cfcb6e"
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

# Automator.Services.NativeMethods.GetMessage(Message,nint,uint,uint)

internal static extern int GetMessage(Message message, nint window, uint minimum, uint maximum)

## Connections

- impact (called by): [[function-cs-bee7c0b4|Automator.Services.KeyboardHookService.HookThread()]] (src/Automator.App/Services/KeyboardHookService.cs:108)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L99`
