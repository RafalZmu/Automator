---
graph_id: "function-cs-cd23d773"
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

# Automator.Windows.WindowsForegroundWindow.TryBringToForeground(nint,int)

public static ForegroundWindowAttempt TryBringToForeground(nint window, int expectedProcessId)

## Connections

- impact (called by): [[function-cs-2d3ba82d|Automator.Backend.BackendServer.AcquirePanelForeground()]] (src/Automator.Backend/BackendServer.cs:1099)
- impact (called by): [[function-cs-3fb84b3a|Automator.Backend.BackendServer.RestorePriorForeground(ForegroundCheckParams)]] (src/Automator.Backend/BackendServer.cs:1133)
- impact (called by): [[function-cs-8737a8ad|Automator.Windows.WindowsApplicationActivator.RestoreAndMaximizeAsync(nint,string,CancellationToken)]] (src/Automator.Windows/WindowsApplicationActivator.cs:115)
- impact (called by): [[function-cs-8737a8ad|Automator.Windows.WindowsApplicationActivator.RestoreAndMaximizeAsync(nint,string,CancellationToken)]] (src/Automator.Windows/WindowsApplicationActivator.cs:136)

## Source anchors

- `src/Automator.Windows/WindowsForegroundWindow.cs#L19`
