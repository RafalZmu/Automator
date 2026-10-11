---
graph_id: "function-cs-f716b6ae"
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

# Automator.Application.Automation.AutomationServiceAuthorization.Evaluate(string,string,IReadOnlyList<AutomationCapabilityRequirement>,string,bool,bool)

public static AutomationServiceAuthorizationFailure Evaluate(string requestedModuleId, string activeModuleId, IReadOnlyList capabilities, string requiredCapability, bool contextEligible, bool requireEligibleContext)

## Connections

- impact (called by): [[function-cs-b3dc5dbe|Automator.Backend.BackendServer.RequireActiveServiceContext(string,string?,RpcRequest,HostWindowContextParams?,bool,bool)]] (src/Automator.Backend/BackendServer.cs:653)
- impact (called by): [[function-cs-d249902a|Automator.Backend.BackendServer.ValidateRequestedCapability(string,string?,HostWindowContextParams?,RpcRequest,bool)]] (src/Automator.Backend/BackendServer.cs:711)
- impact (called by): [[function-cs-e1f35018|Automator.Backend.BackendServer.GetAutomationKeyboardEligibility(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:353)

## Source anchors

- `src/Automator.Application/Automation/AutomationServiceAuthorization.cs#L16`
