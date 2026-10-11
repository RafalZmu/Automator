---
graph_id: "function-cs-d61422f6"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/api"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Automation.ApiModule.TryReadProfile(AutomationLibraryRecord)

public static ApiProfile TryReadProfile(AutomationLibraryRecord record)

## Connections

- impact (called by): [[function-cs-19609f5b|Automator.Infrastructure.Automation.AutomationLibraryTransferService.NormalizeRecord(AutomationLibraryRecord)]] (src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:206)
- impact (called by): [[function-cs-2b34182c|Automator.Application.Automation.ApiModule.RequireProfileAsync(string,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:255)
- impact (called by): [[function-cs-2ed359b5|Automator.Infrastructure.Automation.AutomationApiProfileRunner.RunProfileAsync(string,JsonElement?,CancellationToken)]] (src/Automator.Infrastructure/Automation/AutomationApiProfileRunner.cs:32)
- impact (called by): [[function-cs-93e0cb83|Automator.Application.Automation.ApiModule.SaveProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:151)
- impact (called by): [[function-cs-c406b5d1|Automator.Application.Automation.ApiSavedProfileHandler.ExecuteAsync(string,JsonElement?,AutomationExecutionMetadata,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:579)
- impact (called by): [[function-cs-dd7d1a9e|Automator.Application.Automation.ApiModule.DeleteProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:172)

## Source anchors

- `src/Automator.Application/Automation/ApiModule.cs#L447`
