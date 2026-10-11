---
graph_id: "file-automation-request-executor-ee35da0c"
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

# AutomationRequestExecutor.cs

Source file src/Automator.Application/Automation/AutomationRequestExecutor.cs.

## Connections

- contains (declares function): [[request-executor|Automator.Application.Automation.AutomationRequestExecutor.ExecuteAsync(string,string,CancellationToken,Func<CancellationToken, Task<T>>)]] (src/Automator.Application/Automation/AutomationRequestExecutor.cs#L9)
- contains (declares function): [[function-cs-88b22d32|Automator.Application.Automation.AutomationRequestExecutor.Cancel(string,string)]] (src/Automator.Application/Automation/AutomationRequestExecutor.cs#L38)
- contains (declares function): [[function-1734-570283f9|lambda@src/Automator.Application/Automation/AutomationRequestExecutor.cs:47]] (src/Automator.Application/Automation/AutomationRequestExecutor.cs#L47)
- contains (declares function): [[function-cs-9058ce02|Automator.Application.Automation.AutomationRequestExecutor.CancelModule(string)]] (src/Automator.Application/Automation/AutomationRequestExecutor.cs#L47)
- contains (declares function): [[function-1855-f0a23742|lambda@src/Automator.Application/Automation/AutomationRequestExecutor.cs:49]] (src/Automator.Application/Automation/AutomationRequestExecutor.cs#L49)
- contains (declares function): [[function-cs-a24151b9|Automator.Application.Automation.AutomationRequestExecutor.CancelAll()]] (src/Automator.Application/Automation/AutomationRequestExecutor.cs#L49)
- contains (declares function): [[function-cs-91e4210f|Automator.Application.Automation.AutomationRequestExecutor.CancelMatching(Func<(string ModuleId, string RequestId), bool>)]] (src/Automator.Application/Automation/AutomationRequestExecutor.cs#L51)
- contains (declares function): [[function-2079-cc9a532e|lambda@src/Automator.Application/Automation/AutomationRequestExecutor.cs:55]] (src/Automator.Application/Automation/AutomationRequestExecutor.cs#L55)
- contains (declares function): [[function-2079-cc9a532e|lambda@src/Automator.Application/Automation/AutomationRequestExecutor.cs:55]] (src/Automator.Application/Automation/AutomationRequestExecutor.cs#L55)
- contains (declares function): [[function-cs-f98303a9|Automator.Application.Automation.AutomationRequestExecutor.TryCancel(CancellationTokenSource)]] (src/Automator.Application/Automation/AutomationRequestExecutor.cs#L60)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:1728)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:30)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:394)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:413)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:428)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:559)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:863)

## Source anchors

- `src/Automator.Application/Automation/AutomationRequestExecutor.cs`
