---
graph_id: "function-cs-9098fd60"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/workflow-engine"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Automation.AutomationWorkflowEngine.ValidateJsonTextSize(string,int,string)

private static void ValidateJsonTextSize(string value, int limit, string label)

## Connections

- impact (called by): [[function-cs-6ec23e73|Automator.Application.Automation.AutomationWorkflowEngine.BuildRootInput(AutomationWorkflowProfile,AutomationVariableSnapshot?,JsonElement?)]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:462)
- impact (called by): [[function-cs-7361710f|Automator.Application.Automation.AutomationWorkflowEngine.ValidateJsonSize(JsonElement?,int,string)]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:411)
- impact (called by): [[function-cs-88c65419|Automator.Application.Automation.AutomationWorkflowEngine.ValidateWorkflow(AutomationWorkflowProfile)]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:290)
- impact (called by): [[function-cs-ea057a38|Automator.Application.Automation.AutomationWorkflowEngine.MapInput(AutomationWorkflowStep,JsonElement?,JsonElement?,IReadOnlyDictionary<string, JsonElement>)]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:272)

## Source anchors

- `src/Automator.Application/Automation/AutomationWorkflowEngine.cs#L415`
