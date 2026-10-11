---
graph_id: "function-cs-a7ae1b55"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/workflows"
  - "impact-graph/language/csharp"
  - "impact-graph/test"
generated_by: "scripts/impact-graph.mjs"
---

# MemoryLibraryStore.Seed(string,string,string,int,JsonElement)

public void Seed(string moduleId, string collection, string id, int schemaVersion, JsonElement data)

## Connections

- impact (called by): [[function-cs-31f6febf|Program.RunsWithSavedVariablesAndSafeHistory()]] (tests/Automator.Workflows.Specs/Program.cs:282)
- impact (called by): [[function-cs-8d6bc55b|Program.RunUsesSavedHostWorkflow()]] (tests/Automator.Workflows.Specs/Program.cs:127)
- impact (called by): [[function-cs-aa856c37|Program.LoadsSingleWorkflowAndBoundsInput()]] (tests/Automator.Workflows.Specs/Program.cs:167)
- impact (called by): [[function-cs-e3a1f302|Program.PersistsVariablesAndDefaultsLegacyRecords()]] (tests/Automator.Workflows.Specs/Program.cs:238)

## Source anchors

- `tests/Automator.Workflows.Specs/Program.cs#L348`
