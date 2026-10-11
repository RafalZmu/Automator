---
graph_id: "function-cs-a00e9932"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/workflows"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Automation.WorkflowModule.Result(AutomationStatus,string,object)

private static AutomationResult Result(AutomationStatus status, string message, object data)

## Connections

- impact (called by): [[function-cs-69732be8|Automator.Application.Automation.WorkflowModule.ListWorkflowsAsync(AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/WorkflowModule.cs:107)
- impact (called by): [[function-cs-71ed3a11|Automator.Application.Automation.WorkflowModule.GetWorkflowAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/WorkflowModule.cs:124)
- impact (called by): [[function-cs-8734f347|Automator.Application.Automation.WorkflowModule.SaveWorkflowAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/WorkflowModule.cs:138)
- impact (called by): [[function-cs-b6b28d3c|Automator.Application.Automation.WorkflowModule.Error(string)]] (src/Automator.Application/Automation/WorkflowModule.cs:393)
- impact (called by): [[function-cs-cb57835a|Automator.Application.Automation.WorkflowModule.ListSavedProfilesAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/WorkflowModule.cs:152)
- impact (called by): [[function-cs-ec179b43|Automator.Application.Automation.WorkflowModule.DeleteWorkflowAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/WorkflowModule.cs:161)

## Source anchors

- `src/Automator.Application/Automation/WorkflowModule.cs#L395`
