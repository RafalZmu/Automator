---
graph_id: "file-automation-library-transfer-service-6afe8174"
graph_layer: "file"
graph_kind: "csharp"
tags:
  - "impact-graph"
  - "impact-graph/file"
  - "impact-graph/kind/csharp"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# AutomationLibraryTransferService.cs

Source file src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs.

## Connections

- contains (declares function): [[function-cs-482c9774|Automator.Infrastructure.Automation.AutomationLibraryTransferService.AutomationLibraryTransferService(IAutomationLibraryStore,Func<string, string, CancellationToken, Task<bool>>?,TimeProvider?)]] (src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs#L48)
- contains (declares function): [[function-cs-18e55154|Automator.Infrastructure.Automation.AutomationLibraryTransferService.ExportAsync(CancellationToken)]] (src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs#L58)
- contains (declares function): [[function-cs-bfa77545|Automator.Infrastructure.Automation.AutomationLibraryTransferService.ImportAsync(ReadOnlyMemory<byte>,CancellationToken)]] (src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs#L81)
- contains (declares function): [[function-5395-f54bdd2d|lambda@src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:115]] (src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs#L115)
- contains (declares function): [[function-cs-658d907c|Automator.Infrastructure.Automation.AutomationLibraryTransferService.ParseEnvelope(JsonElement)]] (src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs#L145)
- contains (declares function): [[function-cs-1c2a9dd4|Automator.Infrastructure.Automation.AutomationLibraryTransferService.TryReadString(JsonElement,string,string)]] (src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs#L176)
- contains (declares function): [[function-cs-19609f5b|Automator.Infrastructure.Automation.AutomationLibraryTransferService.NormalizeRecord(AutomationLibraryRecord)]] (src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs#L185)
- contains (declares function): [[function-cs-23e7b32f|Automator.Infrastructure.Automation.AutomationLibraryTransferService.CollectRepairWarningsAsync(PortableRecord,List<AutomationLibraryImportWarning>,CancellationToken)]] (src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs#L247)
- contains (declares function): [[function-cs-8ab68a20|Automator.Infrastructure.Automation.AutomationLibraryTransferService.CreateJsonOptions()]] (src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs#L274)
- contains (declares function): [[function-cs-b60e9b5e|Automator.Infrastructure.Automation.AutomationLibraryTransferService.PortableRecord.ToLibraryRecord(DateTimeOffset)]] (src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs#L290)
- impact (type-reference): [[file-program-cae4b7ba|Program.cs]] (tests/Automator.Variables.Specs/Program.cs:58)
- impact (type-reference): [[file-program-cae4b7ba|Program.cs]] (tests/Automator.Variables.Specs/Program.cs:61)
- impact (type-reference): [[file-program-cae4b7ba|Program.cs]] (tests/Automator.Variables.Specs/Program.cs:63)
- impact (type-reference): [[file-program-cae4b7ba|Program.cs]] (tests/Automator.Variables.Specs/Program.cs:68)

## Source anchors

- `src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs`
