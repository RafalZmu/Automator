---
graph_id: "function-464-16f9a512"
graph_layer: "function"
graph_kind: "function"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/function"
  - "impact-graph/area/desktop"
  - "impact-graph/language/typescript"
generated_by: "scripts/impact-graph.mjs"
---

# authorizeAutomationServiceCall

(isMainFrame: boolean, state: AutomationServiceState | undefined, moduleId: string, capabilityId: string, allowInactiveCleanup: boolean, windowContext?: AutomationWindowContext) => 'forward' | 'noop'

## Connections

- impact (called by): [[function-8938-bab6e19e|invokeAutomationService]] (electron/main.ts:220)

## Source anchors

- `electron/automationServiceAuthorization.ts#L17`
