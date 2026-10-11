---
graph_id: "function-cs-a57fee09"
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

# Automator.Windows.KeyboardHookService.StartRecording()

public void StartRecording()

## Connections

- impact (called by): [[function-cs-a31a9c40|Program.RecordingCanBeCancelled()]] (tests/Automator.Windows.Specs/Program.cs:117)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:293)

## Source anchors

- `src/Automator.Windows/KeyboardHookService.cs#L90`
