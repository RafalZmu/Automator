---
graph_id: "function-cs-39bcacb6"
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

# Automator.Windows.Interop.WindowsNativeMethods.TranslateMessage(Message)

internal static extern bool TranslateMessage(Message message)

## Connections

- impact (called by): [[function-cs-1d77c74b|Automator.Windows.KeyboardHookService.HookThread()]] (src/Automator.Windows/KeyboardHookService.cs:129)
- impact (called by): [[function-cs-56a445db|Automator.Windows.ForegroundWindowMonitor.HookThread()]] (src/Automator.Windows/ForegroundWindowMonitor.cs:75)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L69`
