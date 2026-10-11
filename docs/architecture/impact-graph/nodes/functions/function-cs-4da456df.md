---
graph_id: "function-cs-4da456df"
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

# Automator.Windows.KeyboardInputNormalizer.TryGetTransition(uint,bool)

public static bool TryGetTransition(uint message, bool isDown)

## Connections

- impact (called by): [[function-cs-3b137fa2|Automator.Windows.KeyboardHookService.ProcessKey(int,KeyboardHookData)]] (src/Automator.Windows/KeyboardHookService.cs:171)
- impact (called by): [[function-cs-eb56fb36|Program.NormalizerTracksTransitionsAndSystemKeys()]] (tests/Automator.Windows.Specs/Program.cs:66)
- impact (called by): [[function-cs-eb56fb36|Program.NormalizerTracksTransitionsAndSystemKeys()]] (tests/Automator.Windows.Specs/Program.cs:68)
- impact (called by): [[function-cs-eb56fb36|Program.NormalizerTracksTransitionsAndSystemKeys()]] (tests/Automator.Windows.Specs/Program.cs:70)

## Source anchors

- `src/Automator.Windows/KeyboardInputNormalizer.cs#L35`
