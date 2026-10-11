---
graph_id: "function-cs-62c4b967"
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

# Automator.Application.Automation.IAutomationLibrary.GetAsync(string,string,CancellationToken)

Task GetAsync(string collection, string id, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-2b34182c|Automator.Application.Automation.ApiModule.RequireProfileAsync(string,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:254)
- impact (called by): [[function-cs-4224ed33|Automator.Application.Automation.BrowserAutomationModule.RequireProfileAsync(string,IAutomationLibrary,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:227)
- impact (called by): [[function-cs-452bdcae|Automator.Application.Automation.WorkflowModule.RunWorkflowAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/WorkflowModule.cs:170)
- impact (called by): [[function-cs-594088fd|Automator.Application.Automation.ScriptRunnerModule.RunProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:165)
- impact (called by): [[function-cs-71ed3a11|Automator.Application.Automation.WorkflowModule.GetWorkflowAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/WorkflowModule.cs:115)
- impact (called by): [[function-cs-93e0cb83|Automator.Application.Automation.ApiModule.SaveProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:150)
- impact (called by): [[function-cs-d9b15d07|Automator.Application.Automation.PlaywrightTestExplorer.GetProjectRootAsync(CancellationToken)]] (src/Automator.Application/Automation/PlaywrightTestExplorer.cs:340)
- impact (called by): [[function-cs-dd7d1a9e|Automator.Application.Automation.ApiModule.DeleteProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:168)

## Source anchors

- `src/Automator.Application/Automation/AutomationLibraryStore.cs#L27`
