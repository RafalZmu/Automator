---
graph_id: "function-cs-9eab53cb"
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

# Automator.Services.NativeMethods.SetWinEventHook(uint,uint,nint,WinEventProc,uint,uint,uint)

internal static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint module, WinEventProc callback, uint processId, uint threadId, uint flags)

## Connections

- impact (called by): [[function-cs-3d03d356|Automator.Services.WindowsLauncherService.WindowsLauncherService()]] (src/Automator.App/Services/WindowsLauncherService.cs:18)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L199`
