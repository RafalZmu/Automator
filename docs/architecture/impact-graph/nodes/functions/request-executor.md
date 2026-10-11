---
graph_id: "request-executor"
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

# Automator.Application.Automation.AutomationRequestExecutor.ExecuteAsync(string,string,CancellationToken,Func<CancellationToken, Task<T>>)

public Task ExecuteAsync(string moduleId, string requestId, CancellationToken contextToken, Func operation)

## Connections

- impact (module action execution runs inside the request executor callback): [[backend-execute-module|Automator.Backend.BackendServer.ExecuteModuleActionAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs#L428-L439)
- impact (called by): [[function-cs-0dfa7e3c|Program.RequestExecutorCancelsRequestsByScope()]] (tests/Automator.Application.Specs/Program.cs:1017)
- impact (called by): [[function-cs-0dfa7e3c|Program.RequestExecutorCancelsRequestsByScope()]] (tests/Automator.Application.Specs/Program.cs:1029)
- impact (called by): [[function-cs-0dfa7e3c|Program.RequestExecutorCancelsRequestsByScope()]] (tests/Automator.Application.Specs/Program.cs:1035)
- impact (called by): [[backend-execute-module|Automator.Backend.BackendServer.ExecuteModuleActionAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:428)
- impact (called by): [[function-cs-993f9d46|Automator.Backend.BackendServer.SendAutomationHttpRequestAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:394)
- impact (called by): [[function-cs-b46aaf61|Program.ServiceWorkDoesNotBlockLauncherCommands()]] (tests/Automator.Application.Specs/Program.cs:1186)
- impact (called by): [[function-cs-de33264d|Program.RequestExecutorRegistersBeforeImmediateCancellation()]] (tests/Automator.Application.Specs/Program.cs:1054)

## Source anchors

- `src/Automator.Application/Automation/AutomationRequestExecutor.cs#L9`
