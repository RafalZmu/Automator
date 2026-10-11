---
graph_id: "function-cs-8fc55d67"
graph_layer: "function"
graph_kind: "local-function"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/local-function"
  - "impact-graph/area/workflows"
  - "impact-graph/language/csharp"
  - "impact-graph/test"
generated_by: "scripts/impact-graph.mjs"
---

# Program.CreateContext(WorkflowModule,MemoryLibraryStore,FakeWorkflowRunner)

(AutomationCapabilityRegistry Registry, AutomationServicesContext Context) CreateContext(WorkflowModule module, MemoryLibraryStore store, FakeWorkflowRunner runner)

## Connections

- impact (called by): [[function-cs-1cdada9f|Program.PreservesLiteralNull()]] (tests/Automator.Workflows.Specs/Program.cs:105)
- impact (called by): [[function-cs-42422505|Program.ListsOnlySupportedProfileTypes()]] (tests/Automator.Workflows.Specs/Program.cs:146)
- impact (called by): [[function-cs-7310876f|Program.SaveUsesWorkflowLibraryPartition()]] (tests/Automator.Workflows.Specs/Program.cs:43)
- impact (called by): [[function-cs-8d6bc55b|Program.RunUsesSavedHostWorkflow()]] (tests/Automator.Workflows.Specs/Program.cs:128)
- impact (called by): [[function-cs-aa856c37|Program.LoadsSingleWorkflowAndBoundsInput()]] (tests/Automator.Workflows.Specs/Program.cs:168)
- impact (called by): [[function-cs-c9e8caf8|Program.RejectsInvalidSteps()]] (tests/Automator.Workflows.Specs/Program.cs:64)
- impact (called by): [[function-cs-e3a1f302|Program.PersistsVariablesAndDefaultsLegacyRecords()]] (tests/Automator.Workflows.Specs/Program.cs:223)
- impact (called by): [[function-cs-fad74e4a|Program.AcceptsRootVariableMappingsAndRejectsEmptyPointerSegments()]] (tests/Automator.Workflows.Specs/Program.cs:194)

## Source anchors

- `tests/Automator.Workflows.Specs/Program.cs#L303`
