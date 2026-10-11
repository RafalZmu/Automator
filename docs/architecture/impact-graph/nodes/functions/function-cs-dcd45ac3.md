---
graph_id: "function-cs-dcd45ac3"
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

# Automator.Services.NativeMethods.SetWindowsHookEx(int,LowLevelKeyboardProc,nint,uint)

internal static extern nint SetWindowsHookEx(int hookType, LowLevelKeyboardProc callback, nint module, uint threadId)

## Connections

- impact (called by): [[function-cs-bee7c0b4|Automator.Services.KeyboardHookService.HookThread()]] (src/Automator.App/Services/KeyboardHookService.cs:91)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L91`
