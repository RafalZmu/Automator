---
graph_id: "function-cs-f5c2340b"
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

# Automator.Windows.Interop.WindowsNativeMethods.SetWindowsHookEx(int,LowLevelKeyboardProc,nint,uint)

internal static extern nint SetWindowsHookEx(int hookType, LowLevelKeyboardProc callback, nint module, uint threadId)

## Connections

- impact (called by): [[function-cs-1d77c74b|Automator.Windows.KeyboardHookService.HookThread()]] (src/Automator.Windows/KeyboardHookService.cs:111)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L59`
