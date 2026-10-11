---
graph_id: "function-cs-d10965af"
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

# Automator.Windows.Interop.WindowsNativeMethods.SetWinEventHook(uint,uint,nint,WinEventProc,uint,uint,uint)

internal static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint module, WinEventProc callback, uint processId, uint threadId, uint flags)

## Connections

- impact (called by): [[function-cs-56a445db|Automator.Windows.ForegroundWindowMonitor.HookThread()]] (src/Automator.Windows/ForegroundWindowMonitor.cs:46)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L110`
