# Workflows contributor guide

Read the root AGENTS.md first. Workflows compose saved Script Runner, API, and Browser Automation profiles into ordered sequences.

## Editing and execution rules

- Keep the renderer editor in ui/modules/WorkflowsView.tsx, shared workflow DTO validation in contracts/workflows.ts, and execution/validation in WorkflowModule.cs and AutomationWorkflowEngine.cs.
- Steps are sequential. Preserve add, remove, reorder, profile selection, JSON input mapping, validation, and stop-on-first-failure behavior. Do not turn the workflow into an implicit graph or parallel runner.
- A step may receive a literal JSON value, workflow variables, or a JSON Pointer value from an earlier step. Preserve JSON types; do not stringify values unless the target profile requires text.
- Only use supported saved-profile module IDs and profile IDs. Do not add arbitrary executable paths or raw shell snippets to workflow definitions.
- Validate source and destination pointers and detect invalid/conflicting mappings before saving or running.
- Keep workflow output/history limits and metadata-only persistence. Treat step output as potentially sensitive and do not write it to application logs.
- Add Application specs for mapping, ordering, failure/cancellation and renderer contract tests for any new editor field.

## Code and checks

UI: ui/modules/WorkflowsView.tsx and WorkflowsView.css.
Contracts: contracts/workflows.ts and src/Automator.Application/Automation/AutomationWorkflowContracts.cs.
Module/engine: WorkflowModule.cs and AutomationWorkflowEngine.cs.
Tests: tests/workflows-contract.test.mjs and tests/Automator.Workflows.Specs.

Update the sibling README when supported step types, mapping semantics, or workflow behavior changes.
