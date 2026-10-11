---
graph_id: "function-cs-f99514cb"
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

# Program.Binding(string,string,string,string,string)

AppBinding Binding(string id, string name, string path, string alias, string arguments)

## Connections

- impact (called by): [[function-cs-192e2a7c|Program.PendingAliasDelayCanBeCancelled()]] (tests/Automator.Application.Specs/Program.cs:1216)
- impact (called by): [[function-cs-1f086787|Program.ModeTransitionCancelsPendingActivation()]] (tests/Automator.Application.Specs/Program.cs:184)
- impact (called by): [[function-cs-2a622a97|Program.CatalogPinsAndDeduplicates()]] (tests/Automator.Application.Specs/Program.cs:121)
- impact (called by): [[function-cs-2a622a97|Program.CatalogPinsAndDeduplicates()]] (tests/Automator.Application.Specs/Program.cs:122)
- impact (called by): [[function-cs-2a622a97|Program.CatalogPinsAndDeduplicates()]] (tests/Automator.Application.Specs/Program.cs:123)
- impact (called by): [[function-cs-2a622a97|Program.CatalogPinsAndDeduplicates()]] (tests/Automator.Application.Specs/Program.cs:124)
- impact (called by): [[function-cs-2a622a97|Program.CatalogPinsAndDeduplicates()]] (tests/Automator.Application.Specs/Program.cs:125)
- impact (called by): [[function-cs-2dfdfcb0|Program.StaleCatalogCompletionIsIgnored()]] (tests/Automator.Application.Specs/Program.cs:200)
- impact (called by): [[function-cs-6413eaf0|Program.AliasEditUpdatesBinding()]] (tests/Automator.Application.Specs/Program.cs:219)
- impact (called by): [[function-cs-6413eaf0|Program.AliasEditUpdatesBinding()]] (tests/Automator.Application.Specs/Program.cs:220)
- impact (called by): [[function-cs-71105874|Program.AliasMatchingIsContextual()]] (tests/Automator.Application.Specs/Program.cs:239)
- impact (called by): [[function-cs-b314ad94|Program.QueryChangeDoesNotCancelActivation()]] (tests/Automator.Application.Specs/Program.cs:1231)

## Source anchors

- `tests/Automator.Application.Specs/Program.cs#L1254`
