---
graph_id: "function-cs-2b273f70"
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

# Automator.Application.Automation.AutomationServicesContext.DisposeAsync()

public ValueTask DisposeAsync()

## Connections

- impact (called by): [[backend-execute-module|Automator.Backend.BackendServer.ExecuteModuleActionAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:485)
- impact (called by): [[function-cs-5787916b|Program.DisposedContextWaitsForKeyboardCleanup()]] (tests/Automator.Application.Specs/Program.cs:857)
- impact (called by): [[function-cs-62731e3a|Program.ContextDisposalScopesHttpCalls()]] (tests/Automator.Application.Specs/Program.cs:840)
- impact (called by): [[function-cs-993f9d46|Automator.Backend.BackendServer.SendAutomationHttpRequestAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:404)
- impact (called by): [[function-cs-d431899b|Program.ModuleProcessCapabilityIsScoped()]] (tests/Automator.Application.Specs/Program.cs:607)
- impact (called by): [[function-cs-e62248eb|Automator.Backend.BackendServer.DeactivateServicesAsync()]] (src/Automator.Backend/BackendServer.cs:864)
- impact (called by): [[function-cs-fa089a22|Program.ModuleLibraryCapabilityIsScoped()]] (tests/Automator.Application.Specs/Program.cs:591)

## Source anchors

- `src/Automator.Application/Automation/AutomationServicesContext.cs#L119`
