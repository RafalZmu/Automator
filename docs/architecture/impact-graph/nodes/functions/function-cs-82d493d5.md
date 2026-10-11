---
graph_id: "function-cs-82d493d5"
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

# Automator.Services.NativeMethods.GetCurrentThreadId()

internal static extern uint GetCurrentThreadId()

## Connections

- impact (called by): [[function-cs-03b76bea|Automator.MainWindow.ActivatePanelWindow(string)]] (src/Automator.App/MainWindow.xaml.cs:476)
- impact (called by): [[function-cs-bee7c0b4|Automator.Services.KeyboardHookService.HookThread()]] (src/Automator.App/Services/KeyboardHookService.cs:85)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L109`
