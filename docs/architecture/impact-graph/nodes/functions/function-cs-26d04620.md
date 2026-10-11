---
graph_id: "function-cs-26d04620"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/windows"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Windows.KeyboardInputNormalizer.Normalize(uint,uint,uint,bool,bool,long)

public KeyInputEvent Normalize(uint virtualKey, uint scanCode, uint flags, bool isDown, bool isRepeat, long sequence)

## Connections

- impact (called by): [[function-cs-3b137fa2|Automator.Windows.KeyboardHookService.ProcessKey(int,KeyboardHookData)]] (src/Automator.Windows/KeyboardHookService.cs:197)

## Source anchors

- `src/Automator.Windows/KeyboardInputNormalizer.cs#L16`
