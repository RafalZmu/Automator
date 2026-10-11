---
graph_id: "function-cs-01266871"
graph_layer: "function"
graph_kind: "local-function"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/local-function"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
  - "impact-graph/test"
generated_by: "scripts/impact-graph.mjs"
---

# Program.CreateSettings(AppBinding[])

LauncherSettings CreateSettings(AppBinding[] bindings)

## Connections

- impact (called by): [[function-cs-1e9c4477|Program.RecordingRequiresVisibleFocusedContext()]] (tests/Automator.Application.Specs/Program.cs:161)
- impact (called by): [[function-cs-1f086787|Program.ModeTransitionCancelsPendingActivation()]] (tests/Automator.Application.Specs/Program.cs:181)
- impact (called by): [[function-cs-2a622a97|Program.CatalogPinsAndDeduplicates()]] (tests/Automator.Application.Specs/Program.cs:120)
- impact (called by): [[function-cs-2dfdfcb0|Program.StaleCatalogCompletionIsIgnored()]] (tests/Automator.Application.Specs/Program.cs:196)
- impact (called by): [[function-cs-481685e2|Program.GlobalTabKeysRequireLauncherContext()]] (tests/Automator.Application.Specs/Program.cs:101)
- impact (called by): [[function-cs-6413eaf0|Program.AliasEditUpdatesBinding()]] (tests/Automator.Application.Specs/Program.cs:221)
- impact (called by): [[function-cs-6d31402f|Program.OpeningResetsStateAndCapturesContext()]] (tests/Automator.Application.Specs/Program.cs:86)
- impact (called by): [[function-cs-71105874|Program.AliasMatchingIsContextual()]] (tests/Automator.Application.Specs/Program.cs:239)
- impact (called by): [[function-cs-a22a1a1a|Program.RevisionsIncrease()]] (tests/Automator.Application.Specs/Program.cs:256)
- impact (called by): [[function-cs-f79b54b9|Program.NavigationKeyPolicyRequiresFocusedUnmodifiedLauncher()]] (tests/Automator.Application.Specs/Program.cs:140)

## Source anchors

- `tests/Automator.Application.Specs/Program.cs#L1246`
