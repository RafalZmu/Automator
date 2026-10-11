---
graph_id: "function-cs-7ffb2950"
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

# Automator.Application.Automation.IAutomationLibrary.DeleteAsync(string,string,CancellationToken)

Task DeleteAsync(string collection, string id, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-0c537ff9|Automator.Application.Automation.PlaywrightTestExplorer.SaveTagsAsync(JsonElement,CancellationToken)]] (src/Automator.Application/Automation/PlaywrightTestExplorer.cs:147)
- impact (called by): [[function-cs-0efb206d|Automator.Application.Automation.ScriptRunnerModule.DeleteProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:156)
- impact (called by): [[function-cs-96bdb175|Automator.Application.Automation.PlaywrightTestExplorer.RemoveStaleTagsAsync(CancellationToken)]] (src/Automator.Application/Automation/PlaywrightTestExplorer.cs:168)
- impact (called by): [[function-cs-dd7d1a9e|Automator.Application.Automation.ApiModule.DeleteProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:173)
- impact (called by): [[function-cs-e729abf2|Automator.Application.Automation.BrowserAutomationModule.DeleteProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:175)
- impact (called by): [[function-cs-ec179b43|Automator.Application.Automation.WorkflowModule.DeleteWorkflowAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/WorkflowModule.cs:160)

## Source anchors

- `src/Automator.Application/Automation/AutomationLibraryStore.cs#L29`
