---
graph_id: "function-cs-83c0db6a"
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

# Automator.Application.Automation.IAutomationLibrary.UpsertAsync(string,string,int,JsonElement,CancellationToken)

Task UpsertAsync(string collection, string id, int schemaVersion, JsonElement data, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-0c537ff9|Automator.Application.Automation.PlaywrightTestExplorer.SaveTagsAsync(JsonElement,CancellationToken)]] (src/Automator.Application/Automation/PlaywrightTestExplorer.cs:152)
- impact (called by): [[function-cs-654dae28|Automator.Application.Automation.ScriptRunnerTemplateInstaller.InstallAsync(string,JsonElement,IAutomationLibrary,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:65)
- impact (called by): [[function-cs-6571ede2|Automator.Application.Automation.ApiModule.ClearSecretAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:218)
- impact (called by): [[function-cs-8734f347|Automator.Application.Automation.WorkflowModule.SaveWorkflowAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/WorkflowModule.cs:136)
- impact (called by): [[function-cs-93e0cb83|Automator.Application.Automation.ApiModule.SaveProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:152)
- impact (called by): [[function-cs-c5c06584|Automator.Application.Automation.BrowserAutomationModule.SaveProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:165)
- impact (called by): [[function-cs-d67c7069|Automator.Application.Automation.PlaywrightTestExplorer.SetProjectAsync(JsonElement,CancellationToken)]] (src/Automator.Application/Automation/PlaywrightTestExplorer.cs:51)
- impact (called by): [[function-cs-d9b15d07|Automator.Application.Automation.PlaywrightTestExplorer.GetProjectRootAsync(CancellationToken)]] (src/Automator.Application/Automation/PlaywrightTestExplorer.cs:361)
- impact (called by): [[function-cs-f7252106|Automator.Application.Automation.ScriptRunnerModule.SaveProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:147)
- impact (called by): [[function-cs-fa089a22|Program.ModuleLibraryCapabilityIsScoped()]] (tests/Automator.Application.Specs/Program.cs:587)

## Source anchors

- `src/Automator.Application/Automation/AutomationLibraryStore.cs#L28`
