---
graph_id: "function-cs-793ad323"
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

# Automator.Windows.KeyboardHookService.CancelRecording()

public void CancelRecording()

## Connections

- impact (called by): [[function-cs-175a186c|Automator.Backend.BackendServer.SetModeAsync(LauncherMode)]] (src/Automator.Backend/BackendServer.cs:1454)
- impact (called by): [[function-cs-a31a9c40|Program.RecordingCanBeCancelled()]] (tests/Automator.Windows.Specs/Program.cs:119)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:299)
- impact (called by): [[function-cs-bc9e1b17|Automator.Backend.BackendServer.HidePanelAsync()]] (src/Automator.Backend/BackendServer.cs:1439)
- impact (called by): [[function-cs-eb9f861d|Automator.Backend.BackendServer.OpenPanelAsync(bool)]] (src/Automator.Backend/BackendServer.cs:1418)

## Source anchors

- `src/Automator.Windows/KeyboardHookService.cs#L100`
