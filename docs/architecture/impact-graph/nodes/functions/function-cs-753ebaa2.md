---
graph_id: "function-cs-753ebaa2"
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

# RecordingWorkTimeCoordinator.Snapshot()

private static AutomationWorkTimeSnapshot Snapshot()

## Connections

- impact (called by): [[function-cs-65ad61ff|RecordingWorkTimeCoordinator.DeleteEntryAsync(string,CancellationToken)]] (tests/Automator.Application.Specs/Program.cs:1522)
- impact (called by): [[function-cs-9864e9c5|RecordingWorkTimeCoordinator.SaveEntryAsync(string,IReadOnlyList<string>,CancellationToken)]] (tests/Automator.Application.Specs/Program.cs:1502)
- impact (called by): [[function-cs-c3ae2341|RecordingWorkTimeCoordinator.GetSnapshotAsync(CancellationToken)]] (tests/Automator.Application.Specs/Program.cs:1487)
- impact (called by): [[function-cs-cbbc4096|RecordingWorkTimeCoordinator.UpdateEntryAsync(string,string,IReadOnlyList<string>,CancellationToken)]] (tests/Automator.Application.Specs/Program.cs:1513)
- impact (called by): [[function-cs-fd00efee|RecordingWorkTimeCoordinator.RecordAsync(string,CancellationToken)]] (tests/Automator.Application.Specs/Program.cs:1529)

## Source anchors

- `tests/Automator.Application.Specs/Program.cs#L1532`
