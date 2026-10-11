---
graph_id: "function-cs-6d0e05fc"
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

# Automator.Application.Launcher.NavigationKeyPolicy.Decide(LauncherNavigationState,bool,bool,bool,uint)

public static NavigationKeyAction Decide(LauncherNavigationState state, bool nativeForeground, bool rendererFocused, bool hasModifier, uint virtualKey)

## Connections

- impact (called by): [[function-84600-faf3abc3|lambda@src/Automator.Backend/BackendServer.cs:1576]] (src/Automator.Backend/BackendServer.cs:1582)
- impact (called by): [[function-cs-3b137fa2|Automator.Windows.KeyboardHookService.ProcessKey(int,KeyboardHookData)]] (src/Automator.Windows/KeyboardHookService.cs:280)
- impact (called by): [[function-cs-f79b54b9|Program.NavigationKeyPolicyRequiresFocusedUnmodifiedLauncher()]] (tests/Automator.Application.Specs/Program.cs:143)
- impact (called by): [[function-cs-f79b54b9|Program.NavigationKeyPolicyRequiresFocusedUnmodifiedLauncher()]] (tests/Automator.Application.Specs/Program.cs:144)
- impact (called by): [[function-cs-f79b54b9|Program.NavigationKeyPolicyRequiresFocusedUnmodifiedLauncher()]] (tests/Automator.Application.Specs/Program.cs:145)
- impact (called by): [[function-cs-f79b54b9|Program.NavigationKeyPolicyRequiresFocusedUnmodifiedLauncher()]] (tests/Automator.Application.Specs/Program.cs:146)
- impact (called by): [[function-cs-f79b54b9|Program.NavigationKeyPolicyRequiresFocusedUnmodifiedLauncher()]] (tests/Automator.Application.Specs/Program.cs:147)
- impact (called by): [[function-cs-f79b54b9|Program.NavigationKeyPolicyRequiresFocusedUnmodifiedLauncher()]] (tests/Automator.Application.Specs/Program.cs:149)
- impact (called by): [[function-cs-f79b54b9|Program.NavigationKeyPolicyRequiresFocusedUnmodifiedLauncher()]] (tests/Automator.Application.Specs/Program.cs:153)
- impact (called by): [[function-cs-f79b54b9|Program.NavigationKeyPolicyRequiresFocusedUnmodifiedLauncher()]] (tests/Automator.Application.Specs/Program.cs:155)

## Source anchors

- `src/Automator.Application/Launcher/NavigationKeyPolicy.cs#L14`
