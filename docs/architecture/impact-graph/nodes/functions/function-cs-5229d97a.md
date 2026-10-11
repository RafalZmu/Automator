---
graph_id: "function-cs-5229d97a"
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

# Program.ChildExecutable()

(string Executable, string[] PrefixArguments) ChildExecutable()

## Connections

- impact (called by): [[function-cs-3770ff82|Program.PassesStandardInput()]] (tests/Automator.Infrastructure.Specs/Program.cs:119)
- impact (called by): [[function-cs-bdd747f6|Program.RunsBoundedProcesses()]] (tests/Automator.Infrastructure.Specs/Program.cs:77)

## Source anchors

- `tests/Automator.Infrastructure.Specs/Program.cs#L107`
