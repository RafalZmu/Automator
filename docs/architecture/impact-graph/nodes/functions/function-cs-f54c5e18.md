---
graph_id: "function-cs-f54c5e18"
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

# Automator.Application.Automation.IAutomationLibrary.ListAsync(string,CancellationToken)

Task ListAsync(string collection, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-35186-427da59a|lambda@tests/Automator.Application.Specs/Program.cs:592]] (tests/Automator.Application.Specs/Program.cs:592)
- impact (called by): [[function-cs-1f6e0b6f|Automator.Application.Automation.ApiModule.ListProfilesAsync(AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:120)
- impact (called by): [[function-cs-654dae28|Automator.Application.Automation.ScriptRunnerTemplateInstaller.InstallAsync(string,JsonElement,IAutomationLibrary,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:29)
- impact (called by): [[function-cs-69732be8|Automator.Application.Automation.WorkflowModule.ListWorkflowsAsync(AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/WorkflowModule.cs:97)
- impact (called by): [[function-cs-72d8ebb0|Automator.Application.Automation.PlaywrightTestExplorer.ReadTagRecordsAsync(string,CancellationToken)]] (src/Automator.Application/Automation/PlaywrightTestExplorer.cs:317)
- impact (called by): [[function-cs-bde25d08|Automator.Application.Automation.ScriptRunnerModule.ListProfilesAsync(AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:121)
- impact (called by): [[function-cs-db9d20d4|Automator.Application.Automation.BrowserAutomationModule.ListProfilesAsync(AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:139)

## Source anchors

- `src/Automator.Application/Automation/AutomationLibraryStore.cs#L26`
