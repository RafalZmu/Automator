---
graph_id: "desktop"
graph_layer: "module"
graph_kind: "runtime"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/runtime"
  - "impact-graph/area/runtime"
generated_by: "scripts/impact-graph.mjs"
---

# Electron desktop shell

Owns windows, preload bridge, IPC handlers, backend process lifecycle, and desktop adapters.

## Connections

- contains (preload bridge): [[preload-dispatch|dispatchModuleAction]]
- contains (main-process IPC handler): [[main-module-handler|registerIpc.handle.callback@570]]
- contains (backend process client): [[backend-request|BackendProcess.request]]
- contains (backend response reader): [[backend-consume-stdout|BackendProcess.consumeStdout]]
- impact (renderer uses the exposed preload bridge and host context): [[renderer|React renderer]] (electron/preload.ts; ui/bridge.ts)
- contains (owns source file): [[file-automation-service-authorization-35b89aa6|automationServiceAuthorization.ts]]
- contains (owns source file): [[file-backend-process-607c1e5b|backendProcess.ts]]
- contains (owns source file): [[file-host-window-context-7a246009|hostWindowContext.ts]]
- contains (owns source file): [[file-main-13ee6440|main.ts]]
- contains (owns source file): [[file-platform-e2512d22|platform.ts]]
- contains (owns source file): [[file-preload-17fc44a4|preload.ts]]
- contains (owns source file): [[file-script-runner-path-picker-e31c0fb1|scriptRunnerPathPicker.ts]]
- contains (owns source file): [[file-window-context-registry-75d96185|windowContextRegistry.ts]]
- contains (owns source file): [[file-app-xaml-26b4c462|App.xaml.cs]]
- contains (owns source file): [[file-main-window-xaml-749cf8c0|MainWindow.xaml.cs]]
- contains (owns source file): [[file-iautomation-tab-view-66b9983c|IAutomationTabView.cs]]
- contains (owns source file): [[file-launcher-tab-module-ebb01805|LauncherTabModule.cs]]
- contains (owns source file): [[file-app-catalog-service-2f790a7f|AppCatalogService.cs]]
- contains (owns source file): [[file-app-logger-cfd60568|AppLogger.cs]]
- contains (owns source file): [[file-keyboard-hook-service-084333f9|KeyboardHookService.cs]]
- contains (owns source file): [[file-native-methods-8fef308b|NativeMethods.cs]]
- contains (owns source file): [[file-rounded-acrylic-backdrop-c31062de|RoundedAcrylicBackdrop.cs]]
- contains (owns source file): [[file-settings-store-d9ae34e1|SettingsStore.cs]]
- contains (owns source file): [[file-tray-icon-service-f60bd302|TrayIconService.cs]]
- contains (owns source file): [[file-windows-launcher-service-914b3c18|WindowsLauncherService.cs]]
- contains (owns source file): [[file-vite-backend-process-test-config-22652191|vite.backend-process-test.config.ts]]
- contains (owns source file): [[file-vite-config-6a3b01ba|vite.config.ts]]
- contains (owns source file): [[file-vite-main-config-214e9fcb|vite.main.config.ts]]
- contains (owns source file): [[file-vite-preload-config-4ad9a7cf|vite.preload.config.ts]]

## Source anchors

- `electron/main.ts`
- `electron/preload.ts`
- `electron/backendProcess.ts`
