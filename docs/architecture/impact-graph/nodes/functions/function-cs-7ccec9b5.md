---
graph_id: "function-cs-7ccec9b5"
graph_layer: "function"
graph_kind: "constructor"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/constructor"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Automation.AutomationKeyboardEventHub.AutomationKeyboardEventHub(IApplicationLog?)

public AutomationKeyboardEventHub(IApplicationLog log)

## Connections

- impact (called by): [[function-cs-02b16009|Program.KeyboardSubscribersAreRevokedAndOverflowIsDetached()]] (tests/Automator.Application.Specs/Program.cs:929)
- impact (called by): [[function-cs-13052aaf|Program.KeyboardDeliveryRequiresTrustedContext()]] (tests/Automator.Application.Specs/Program.cs:896)
- impact (called by): [[function-cs-13052aaf|Program.KeyboardDeliveryRequiresTrustedContext()]] (tests/Automator.Application.Specs/Program.cs:914)
- impact (called by): [[function-cs-8ad53cb2|Automator.Backend.BackendServer.InitializeAsync(InitializeParams)]] (src/Automator.Backend/BackendServer.cs:896)
- impact (called by): [[function-cs-ded4bb7b|Program.ConcurrentKeyboardRevocationAndDisposalAreSafe()]] (tests/Automator.Application.Specs/Program.cs:979)

## Source anchors

- `src/Automator.Application/Automation/AutomationKeyboardEventHub.cs#L28`
