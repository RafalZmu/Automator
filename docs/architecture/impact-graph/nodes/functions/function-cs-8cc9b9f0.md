---
graph_id: "function-cs-8cc9b9f0"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Automation.AutomationSavedProfileExecutor.RunAsync(string,string,JsonElement?,AutomationExecutionMetadata,CancellationToken)

public Task RunAsync(string moduleId, string profileId, JsonElement? input, AutomationExecutionMetadata metadata, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-63607-a6ef8c56|lambda@tests/Automator.Application.Specs/Program.cs:1080]] (tests/Automator.Application.Specs/Program.cs:1080)
- impact (called by): [[function-cs-a60d77b9|Automator.Application.Automation.AutomationWorkflowEngine.RunAsync(string,JsonElement?,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:70)
- impact (called by): [[function-cs-db1673b5|Automator.Application.Automation.AutomationSchedulerCoordinator.RunScheduleCoreAsync(AutomationScheduleDefinition,DateTimeOffset,bool,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:330)
- impact (called by): [[function-cs-ee81ce94|Automator.Application.Automation.AutomationWorkflowEngine.RunScheduledAsync(string,string,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:149)
- impact (called by): [[function-cs-f2c3e587|Program.SavedProfileExecutionUsesRegisteredHandlers()]] (tests/Automator.Application.Specs/Program.cs:1071)

## Source anchors

- `src/Automator.Application/Automation/AutomationSavedProfileExecution.cs#L72`
