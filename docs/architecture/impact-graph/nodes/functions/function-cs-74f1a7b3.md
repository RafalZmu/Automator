---
graph_id: "function-cs-74f1a7b3"
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

# Automator.Windows.Interop.WindowsNativeMethods.GetCurrentThreadId()

internal static extern uint GetCurrentThreadId()

## Connections

- impact (called by): [[function-cs-1d77c74b|Automator.Windows.KeyboardHookService.HookThread()]] (src/Automator.Windows/KeyboardHookService.cs:107)
- impact (called by): [[function-cs-56a445db|Automator.Windows.ForegroundWindowMonitor.HookThread()]] (src/Automator.Windows/ForegroundWindowMonitor.cs:44)
- impact (called by): [[function-cs-cd23d773|Automator.Windows.WindowsForegroundWindow.TryBringToForeground(nint,int)]] (src/Automator.Windows/WindowsForegroundWindow.cs:33)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L75`
