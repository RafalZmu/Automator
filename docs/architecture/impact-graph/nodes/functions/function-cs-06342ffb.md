---
graph_id: "function-cs-06342ffb"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/work-time"
  - "impact-graph/language/csharp"
  - "impact-graph/test"
generated_by: "scripts/impact-graph.mjs"
---

# WorkTimeCoordinatorHarness.CreateAsync(IAutomationLibraryStore,TimeProvider)

public static Task CreateAsync(IAutomationLibraryStore store, TimeProvider timeProvider)

## Connections

- impact (called by): [[function-cs-0ff7e025|Program.WorkTimeRequiresDescriptionAndStoresTags()]] (tests/Automator.Focus.Specs/Program.cs:252)
- impact (called by): [[function-cs-5ae3f6f0|Program.ActiveWorkTimeSurvivesRestart()]] (tests/Automator.Focus.Specs/Program.cs:329)
- impact (called by): [[function-cs-5ae3f6f0|Program.ActiveWorkTimeSurvivesRestart()]] (tests/Automator.Focus.Specs/Program.cs:335)
- impact (called by): [[function-cs-b4965e3b|Program.WorkTimeEntriesCanBeEditedAndDeleted()]] (tests/Automator.Focus.Specs/Program.cs:298)
- impact (called by): [[function-cs-e707ba34|Program.WorkTimePendingCanResumeAndDiscard()]] (tests/Automator.Focus.Specs/Program.cs:276)

## Source anchors

- `tests/Automator.Focus.Specs/Program.cs#L380`
