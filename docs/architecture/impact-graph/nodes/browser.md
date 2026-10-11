---
graph_id: "browser"
graph_layer: "module"
graph_kind: "feature"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/feature"
  - "impact-graph/area/browser"
generated_by: "scripts/impact-graph.mjs"
---

# Browser Automation

Discovers and runs Playwright tests and browser profiles from the Browser Automation view.

## Connections

- contains (compiled backend action): [[browser-module-execute|Automator.Application.Automation.BrowserAutomationModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]]
- contains (test execution): [[explorer-run|Automator.Application.Automation.PlaywrightTestExplorer.RunAsync(JsonElement,CancellationToken)]]
- contains (explorer factory): [[browser-create-explorer|Automator.Application.Automation.BrowserAutomationModule.CreateExplorer(AutomationServicesContext)]]
- contains (test discovery): [[explorer-discover|Automator.Application.Automation.PlaywrightTestExplorer.DiscoverTestsAsync(string,RunnerState,CancellationToken)]]
- impact (the run handler consumes the runTests result): [[view-run|BrowserAutomationView.run]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L97; ui/modules/BrowserAutomationView.tsx#L235-L238)
- impact (workflows can execute saved Browser Automation profiles as steps): [[workflow-engine|Workflow engine]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs#L26-L70; src/Automator.Backend/BackendServer.cs#L923-L927)
- contains (owns source file): [[file-browser-automation-module-fe90d00b|BrowserAutomationModule.cs]]
- contains (owns source file): [[file-browser-automation-view-41d27232|BrowserAutomationView.tsx]]

## Source anchors

- `src/Automator.Application/Automation/BrowserAutomationModule.cs`
- `src/Automator.Application/Automation/PlaywrightTestExplorer.cs`
- `ui/modules/BrowserAutomationView.tsx`
