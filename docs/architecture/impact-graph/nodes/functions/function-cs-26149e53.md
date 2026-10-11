---
graph_id: "function-cs-26149e53"
graph_layer: "function"
graph_kind: "local-function"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/local-function"
  - "impact-graph/area/scheduler"
  - "impact-graph/language/csharp"
  - "impact-graph/test"
generated_by: "scripts/impact-graph.mjs"
---

# Program.ThrowsAsync(Func<Task>)

Task ThrowsAsync(Func action)

## Connections

- impact (called by): [[function-cs-028ec883|Program.WorkerSurvivesFailureAndSlowRuns()]] (tests/Automator.Scheduler.Specs/Program.cs:196)
- impact (called by): [[function-cs-082c2db5|Program.StartupCanRetry()]] (tests/Automator.Scheduler.Specs/Program.cs:129)
- impact (called by): [[function-cs-082c2db5|Program.StartupCanRetry()]] (tests/Automator.Scheduler.Specs/Program.cs:131)
- impact (called by): [[function-cs-082c2db5|Program.StartupCanRetry()]] (tests/Automator.Scheduler.Specs/Program.cs:132)
- impact (called by): [[function-cs-3586d040|Program.ShutdownCancelsManualRun()]] (tests/Automator.Scheduler.Specs/Program.cs:206)
- impact (called by): [[function-cs-f4f2ab89|Program.CoordinatorPersistsAndSerializesRuns()]] (tests/Automator.Scheduler.Specs/Program.cs:60)

## Source anchors

- `tests/Automator.Scheduler.Specs/Program.cs#L230`
