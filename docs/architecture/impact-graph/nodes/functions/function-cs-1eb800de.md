---
graph_id: "function-cs-1eb800de"
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

# Automator.Services.NativeMethods.EnumWindows(EnumWindowsProc,nint)

internal static extern bool EnumWindows(EnumWindowsProc callback, nint data)

## Connections

- impact (called by): [[function-cs-cf69058c|Automator.Services.WindowsLauncherService.FindWindows(string)]] (src/Automator.App/Services/WindowsLauncherService.cs:158)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L162`
