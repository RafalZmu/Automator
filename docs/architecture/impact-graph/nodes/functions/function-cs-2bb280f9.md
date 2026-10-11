---
graph_id: "function-cs-2bb280f9"
graph_layer: "function"
graph_kind: "local-function"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/local-function"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
  - "impact-graph/test"
generated_by: "scripts/impact-graph.mjs"
---

# Program.CreateService(DelegateHandler,IHostAddressResolver?,RecordingLog?,TimeSpan?)

SharedHttpService CreateService(DelegateHandler handler, IHostAddressResolver resolver, RecordingLog log, TimeSpan? requestTimeout)

## Connections

- impact (called by): [[function-cs-0ee6ee36|Program.EnforcesResolvedAddressPolicy()]] (tests/Automator.Infrastructure.Specs/Program.cs:320)
- impact (called by): [[function-cs-0ee6ee36|Program.EnforcesResolvedAddressPolicy()]] (tests/Automator.Infrastructure.Specs/Program.cs:326)
- impact (called by): [[function-cs-0ee6ee36|Program.EnforcesResolvedAddressPolicy()]] (tests/Automator.Infrastructure.Specs/Program.cs:332)
- impact (called by): [[function-cs-0ee6ee36|Program.EnforcesResolvedAddressPolicy()]] (tests/Automator.Infrastructure.Specs/Program.cs:341)
- impact (called by): [[function-cs-0ee6ee36|Program.EnforcesResolvedAddressPolicy()]] (tests/Automator.Infrastructure.Specs/Program.cs:350)
- impact (called by): [[function-cs-2e383eda|Program.RejectsUnsafeUri()]] (tests/Automator.Infrastructure.Specs/Program.cs:234)
- impact (called by): [[function-cs-3cc6f6a8|Program.ApiProfilesRejectCrossHostRedirects()]] (tests/Automator.Infrastructure.Specs/Program.cs:527)
- impact (called by): [[function-cs-60923b88|Program.MapsCancellationAndTimeout()]] (tests/Automator.Infrastructure.Specs/Program.cs:270)
- impact (called by): [[function-cs-60923b88|Program.MapsCancellationAndTimeout()]] (tests/Automator.Infrastructure.Specs/Program.cs:278)
- impact (called by): [[function-cs-632676fe|Program.DeniesCrossHostRedirect()]] (tests/Automator.Infrastructure.Specs/Program.cs:311)
- impact (called by): [[function-cs-6cc872ec|Program.RunsApiProfileSafely()]] (tests/Automator.Infrastructure.Specs/Program.cs:485)
- impact (called by): [[function-cs-6d98fe9f|Program.ReturnsNonSuccessResult()]] (tests/Automator.Infrastructure.Specs/Program.cs:211)
- impact (called by): [[function-cs-83a98a88|Program.MapsRequestFields()]] (tests/Automator.Infrastructure.Specs/Program.cs:196)
- impact (called by): [[function-cs-840644f2|Program.BoundsResponseReading()]] (tests/Automator.Infrastructure.Specs/Program.cs:261)
- impact (called by): [[function-cs-886e1885|Program.RejectsOversizedRequest()]] (tests/Automator.Infrastructure.Specs/Program.cs:245)
- impact (called by): [[function-cs-9d37c309|Program.ApiProfilesUseProfileDefaultInput()]] (tests/Automator.Infrastructure.Specs/Program.cs:557)
- impact (called by): [[function-cs-b811e19d|Program.RedactsSecrets()]] (tests/Automator.Infrastructure.Specs/Program.cs:453)
- impact (called by): [[function-cs-b892b765|Program.LocalGrantDoesNotBypassHostAllowlist()]] (tests/Automator.Infrastructure.Specs/Program.cs:359)
- impact (called by): [[function-cs-b892b765|Program.LocalGrantDoesNotBypassHostAllowlist()]] (tests/Automator.Infrastructure.Specs/Program.cs:363)
- impact (called by): [[function-cs-c58707a3|Program.LimitsRedirects()]] (tests/Automator.Infrastructure.Specs/Program.cs:294)
- impact (called by): [[function-cs-c58707a3|Program.LimitsRedirects()]] (tests/Automator.Infrastructure.Specs/Program.cs:301)
- impact (called by): [[function-cs-e2e26f33|Program.EnforcesExactHostAllowlist()]] (tests/Automator.Infrastructure.Specs/Program.cs:221)
- impact (called by): [[function-cs-f6df1e39|Program.RejectsBinaryResponse()]] (tests/Automator.Infrastructure.Specs/Program.cs:439)

## Source anchors

- `tests/Automator.Infrastructure.Specs/Program.cs#L579`
