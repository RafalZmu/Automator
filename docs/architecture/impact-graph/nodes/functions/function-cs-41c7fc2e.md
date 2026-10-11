---
graph_id: "function-cs-41c7fc2e"
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

# Automator.Windows.Interop.WindowsNativeMethods.UnhookWinEvent(nint)

internal static extern bool UnhookWinEvent(nint hook)

## Connections

- impact (called by): [[function-cs-56a445db|Automator.Windows.ForegroundWindowMonitor.HookThread()]] (src/Automator.Windows/ForegroundWindowMonitor.cs:83)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L112`
