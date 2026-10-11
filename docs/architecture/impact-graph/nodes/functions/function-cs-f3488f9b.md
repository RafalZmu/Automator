---
graph_id: "function-cs-f3488f9b"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/windows"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Windows.Interop.WindowsNativeMethods.PostThreadMessage(uint,uint,nuint,nint)

internal static extern bool PostThreadMessage(uint threadId, uint message, nuint wParam, nint lParam)

## Connections

- impact (called by): [[function-cs-3c68e13a|Automator.Windows.KeyboardHookService.Dispose()]] (src/Automator.Windows/KeyboardHookService.cs:319)
- impact (called by): [[function-cs-cbe0e819|Automator.Windows.ForegroundWindowMonitor.Dispose()]] (src/Automator.Windows/ForegroundWindowMonitor.cs:99)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L73`
