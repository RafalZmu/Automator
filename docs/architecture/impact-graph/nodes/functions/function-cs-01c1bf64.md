---
graph_id: "function-cs-01c1bf64"
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

# Check.ThrowsAsync(Func<Task>)

public static Task ThrowsAsync(Func action)

## Connections

- impact (called by): [[function-cs-0dfa7e3c|Program.RequestExecutorCancelsRequestsByScope()]] (tests/Automator.Application.Specs/Program.cs:1025)
- impact (called by): [[function-cs-0dfa7e3c|Program.RequestExecutorCancelsRequestsByScope()]] (tests/Automator.Application.Specs/Program.cs:1043)
- impact (called by): [[function-cs-0dfa7e3c|Program.RequestExecutorCancelsRequestsByScope()]] (tests/Automator.Application.Specs/Program.cs:1047)
- impact (called by): [[function-cs-5787916b|Program.DisposedContextWaitsForKeyboardCleanup()]] (tests/Automator.Application.Specs/Program.cs:867)
- impact (called by): [[function-cs-62731e3a|Program.ContextDisposalScopesHttpCalls()]] (tests/Automator.Application.Specs/Program.cs:844)
- impact (called by): [[function-cs-874970c5|Program.SavedProfileCatalogsExposeSafeMetadata()]] (tests/Automator.Application.Specs/Program.cs:1091)
- impact (called by): [[function-cs-d431899b|Program.ModuleProcessCapabilityIsScoped()]] (tests/Automator.Application.Specs/Program.cs:609)
- impact (called by): [[function-cs-de33264d|Program.RequestExecutorRegistersBeforeImmediateCancellation()]] (tests/Automator.Application.Specs/Program.cs:1061)
- impact (called by): [[function-cs-f2c3e587|Program.SavedProfileExecutionUsesRegisteredHandlers()]] (tests/Automator.Application.Specs/Program.cs:1080)
- impact (called by): [[function-cs-f47ecfea|Program.CanceledWorkflowPersistsSafeHistory()]] (tests/Automator.Application.Specs/Program.cs:1173)
- impact (called by): [[function-cs-fa089a22|Program.ModuleLibraryCapabilityIsScoped()]] (tests/Automator.Application.Specs/Program.cs:592)

## Source anchors

- `tests/Automator.Application.Specs/Program.cs#L1284`
