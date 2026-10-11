---
graph_id: "function-cs-84e6b066"
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

# Automator.Application.Automation.AutomationKeyboardEventHub.SetDeliveryContext(KeyboardDeliveryState?)

public void SetDeliveryContext(KeyboardDeliveryState state)

## Connections

- impact (called by): [[function-59407-84c8bd27|lambda@tests/Automator.Application.Specs/Program.cs:994]] (tests/Automator.Application.Specs/Program.cs:997)
- impact (called by): [[function-cs-02b16009|Program.KeyboardSubscribersAreRevokedAndOverflowIsDetached()]] (tests/Automator.Application.Specs/Program.cs:930)
- impact (called by): [[function-cs-02b16009|Program.KeyboardSubscribersAreRevokedAndOverflowIsDetached()]] (tests/Automator.Application.Specs/Program.cs:959)
- impact (called by): [[function-cs-02b16009|Program.KeyboardSubscribersAreRevokedAndOverflowIsDetached()]] (tests/Automator.Application.Specs/Program.cs:961)
- impact (called by): [[function-cs-13052aaf|Program.KeyboardDeliveryRequiresTrustedContext()]] (tests/Automator.Application.Specs/Program.cs:898)
- impact (called by): [[function-cs-13052aaf|Program.KeyboardDeliveryRequiresTrustedContext()]] (tests/Automator.Application.Specs/Program.cs:905)
- impact (called by): [[function-cs-13052aaf|Program.KeyboardDeliveryRequiresTrustedContext()]] (tests/Automator.Application.Specs/Program.cs:909)
- impact (called by): [[function-cs-13052aaf|Program.KeyboardDeliveryRequiresTrustedContext()]] (tests/Automator.Application.Specs/Program.cs:915)
- impact (called by): [[function-cs-73dd72bc|Automator.Backend.BackendServer.DisposeAsync()]] (src/Automator.Backend/BackendServer.cs:1739)
- impact (called by): [[function-cs-86bb1b22|Automator.Backend.BackendServer.RefreshKeyboardDeliveryContext()]] (src/Automator.Backend/BackendServer.cs:770)
- impact (called by): [[function-cs-86bb1b22|Automator.Backend.BackendServer.RefreshKeyboardDeliveryContext()]] (src/Automator.Backend/BackendServer.cs:778)
- impact (called by): [[function-cs-86bb1b22|Automator.Backend.BackendServer.RefreshKeyboardDeliveryContext()]] (src/Automator.Backend/BackendServer.cs:784)
- impact (called by): [[function-cs-86bb1b22|Automator.Backend.BackendServer.RefreshKeyboardDeliveryContext()]] (src/Automator.Backend/BackendServer.cs:790)
- impact (called by): [[function-cs-ded4bb7b|Program.ConcurrentKeyboardRevocationAndDisposalAreSafe()]] (tests/Automator.Application.Specs/Program.cs:980)
- impact (called by): [[function-cs-e1f35018|Automator.Backend.BackendServer.GetAutomationKeyboardEligibility(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:372)
- impact (called by): [[function-cs-e62248eb|Automator.Backend.BackendServer.DeactivateServicesAsync()]] (src/Automator.Backend/BackendServer.cs:859)

## Source anchors

- `src/Automator.Application/Automation/AutomationKeyboardEventHub.cs#L35`
