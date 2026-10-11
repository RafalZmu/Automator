---
graph_id: "process"
graph_layer: "module"
graph_kind: "adapter"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/adapter"
  - "impact-graph/area/infrastructure"
generated_by: "scripts/impact-graph.mjs"
---

# Local process service

Runs explicitly configured executables and bounds captured stdout/stderr.

## Connections

- contains (process execution): [[process-execute|Automator.Infrastructure.Automation.LocalProcessExecutionService.ExecuteAsync(AutomationProcessRequest,CancellationToken)]]
- contains (output capture): [[process-read-bounded|Automator.Infrastructure.Automation.LocalProcessExecutionService.ReadBoundedAsync(StreamReader)]]
- impact (Script Runner uses the scoped process capability): [[script|Script Runner]] (src/Automator.Application/Automation/ScriptRunnerModule.cs; src/Automator.Application/Automation/AutomationServicesContext.cs)
- impact (Playwright test runs execute through the process capability): [[browser|Browser Automation]] (src/Automator.Application/Automation/PlaywrightTestExplorer.cs; src/Automator.Infrastructure/Automation/LocalProcessExecutionService.cs)
- contains (owns source file): [[file-local-process-execution-service-5c3a44b4|LocalProcessExecutionService.cs]]

## Source anchors

- `src/Automator.Infrastructure/Automation/LocalProcessExecutionService.cs`
