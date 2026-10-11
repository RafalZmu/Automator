---
graph_id: "function-cs-fd00efee"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
  - "impact-graph/test"
generated_by: "scripts/impact-graph.mjs"
---

# RecordingWorkTimeCoordinator.RecordAsync(string,CancellationToken)

private Task RecordAsync(string action, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-21c911cd|RecordingWorkTimeCoordinator.StopAsync(CancellationToken)]] (tests/Automator.Application.Specs/Program.cs:1491)
- impact (called by): [[function-cs-311d091d|RecordingWorkTimeCoordinator.StartAsync(CancellationToken)]] (tests/Automator.Application.Specs/Program.cs:1490)
- impact (called by): [[function-cs-350073a2|RecordingWorkTimeCoordinator.DiscardPendingAsync(CancellationToken)]] (tests/Automator.Application.Specs/Program.cs:1493)
- impact (called by): [[function-cs-6f2a7bb4|RecordingWorkTimeCoordinator.ResumePendingAsync(CancellationToken)]] (tests/Automator.Application.Specs/Program.cs:1492)

## Source anchors

- `tests/Automator.Application.Specs/Program.cs#L1525`
