---
graph_id: "windows"
graph_layer: "module"
graph_kind: "adapter"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/adapter"
  - "impact-graph/area/infrastructure"
generated_by: "scripts/impact-graph.mjs"
---

# Windows platform adapters

Provides application discovery, activation, keyboard hooks, foreground tracking, settings, and website launch.

## Connections

- impact (launcher discovery and activation use Windows adapters): [[launcher|Launcher]] (src/Automator.Windows/WindowsAppCatalog.cs; src/Automator.Windows/WindowsApplicationActivator.cs)
- impact (website destinations are activated through the Windows adapter): [[website|Website Launcher]] (src/Automator.Windows/WindowsAutomationWebsiteLauncher.cs)
- contains (owns source file): [[file-file-application-log-e549e5e5|FileApplicationLog.cs]]
- contains (owns source file): [[file-foreground-window-monitor-53cb684b|ForegroundWindowMonitor.cs]]
- contains (owns source file): [[file-windows-native-methods-b736ef3e|WindowsNativeMethods.cs]]
- contains (owns source file): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]]
- contains (owns source file): [[file-keyboard-input-normalizer-d4ae5609|KeyboardInputNormalizer.cs]]
- contains (owns source file): [[file-native-chrome-controller-aee8a59f|NativeChromeController.cs]]
- contains (owns source file): [[file-windows-app-catalog-6686d6e2|WindowsAppCatalog.cs]]
- contains (owns source file): [[file-windows-application-activator-fa3e606b|WindowsApplicationActivator.cs]]
- contains (owns source file): [[file-windows-automation-website-launcher-34138a8e|WindowsAutomationWebsiteLauncher.cs]]
- contains (owns source file): [[file-windows-foreground-window-61234581|WindowsForegroundWindow.cs]]
- contains (owns source file): [[file-windows-icon-cache-5f12d581|WindowsIconCache.cs]]
- contains (owns source file): [[file-windows-settings-store-99ec37f0|WindowsSettingsStore.cs]]
- contains (owns source file): [[file-program-4fc73c51|Program.cs]]

## Source anchors

- `src/Automator.Windows/Automator.Windows.csproj`
- `src/Automator.Windows/WindowsApplicationActivator.cs`
