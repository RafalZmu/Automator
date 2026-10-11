---
graph_id: "function-cs-4e200fbc"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/scheduler"
  - "impact-graph/language/csharp"
  - "impact-graph/test"
generated_by: "scripts/impact-graph.mjs"
---

# TestTimeProvider.GetUtcNow()

public override DateTimeOffset GetUtcNow()

## Connections

- impact (called by): [[function-17385-58536d2b|lambda@tests/Automator.Scheduler.Specs/Program.cs:218]] (tests/Automator.Scheduler.Specs/Program.cs:218)
- impact (called by): [[function-cs-082c2db5|Program.StartupCanRetry()]] (tests/Automator.Scheduler.Specs/Program.cs:134)
- impact (called by): [[function-cs-3e893542|Program.RecoveryPreservesCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:143)
- impact (called by): [[function-cs-3e893542|Program.RecoveryPreservesCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:145)
- impact (called by): [[function-cs-74f78698|Program.CatchupRunsOnce()]] (tests/Automator.Scheduler.Specs/Program.cs:160)
- impact (called by): [[function-cs-74f78698|Program.CatchupRunsOnce()]] (tests/Automator.Scheduler.Specs/Program.cs:165)
- impact (called by): [[function-cs-9d12dd19|Program.UpdatesRebaselineCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:177)
- impact (called by): [[function-cs-9d12dd19|Program.UpdatesRebaselineCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:180)
- impact (called by): [[function-cs-9d12dd19|Program.UpdatesRebaselineCursor()]] (tests/Automator.Scheduler.Specs/Program.cs:182)
- impact (called by): [[function-cs-e5c7e3ca|Program.ClaimFailureDoesNotReplay()]] (tests/Automator.Scheduler.Specs/Program.cs:224)

## Source anchors

- `tests/Automator.Scheduler.Specs/Program.cs#L308`
