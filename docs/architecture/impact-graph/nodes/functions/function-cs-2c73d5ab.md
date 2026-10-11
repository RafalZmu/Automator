---
graph_id: "function-cs-2c73d5ab"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/desktop"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Services.AppLogger.Warning(string,string,Exception?,IReadOnlyDictionary<string, object?>?)

public static void Warning(string eventName, string message, Exception exception, IReadOnlyDictionary properties)

## Connections

- impact (called by): [[function-cs-03b76bea|Automator.MainWindow.ActivatePanelWindow(string)]] (src/Automator.App/MainWindow.xaml.cs:528)
- impact (called by): [[function-cs-03b76bea|Automator.MainWindow.ActivatePanelWindow(string)]] (src/Automator.App/MainWindow.xaml.cs:553)
- impact (called by): [[function-cs-044cf79d|Automator.Services.AppCatalogService.Discover()]] (src/Automator.App/Services/AppCatalogService.cs:107)
- impact (called by): [[function-cs-044cf79d|Automator.Services.AppCatalogService.Discover()]] (src/Automator.App/Services/AppCatalogService.cs:92)
- impact (called by): [[function-cs-0b9625f2|Automator.Services.TrayIconService.TrayIconService(nint,Action,Action,Action,Action)]] (src/Automator.App/Services/TrayIconService.cs:53)
- impact (called by): [[function-cs-103bed2e|Automator.MainWindow.OnWindowActivated(object,WindowActivatedEventArgs)]] (src/Automator.App/MainWindow.xaml.cs:445)
- impact (called by): [[function-cs-260d6f16|Automator.MainWindow.ConfigureAcrylicBackdrop()]] (src/Automator.App/MainWindow.xaml.cs:193)
- impact (called by): [[function-cs-2b9a6aa4|Automator.MainWindow.ShowSettings()]] (src/Automator.App/MainWindow.xaml.cs:843)
- impact (called by): [[function-cs-2d1aa4b1|Automator.Services.AppCatalogService.CaptureUnderCursor()]] (src/Automator.App/Services/AppCatalogService.cs:26)
- impact (called by): [[function-cs-2d1aa4b1|Automator.Services.AppCatalogService.CaptureUnderCursor()]] (src/Automator.App/Services/AppCatalogService.cs:57)
- impact (called by): [[function-cs-3d03d356|Automator.Services.WindowsLauncherService.WindowsLauncherService()]] (src/Automator.App/Services/WindowsLauncherService.cs:27)
- impact (called by): [[function-cs-3da4f18c|Automator.Services.WindowsLauncherService.WaitAndMaximizeAsync(string,Process,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:145)
- impact (called by): [[function-cs-3da4f18c|Automator.Services.WindowsLauncherService.WaitAndMaximizeAsync(string,Process,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:149)
- impact (called by): [[function-cs-5fe9fc70|Automator.MainWindow.PositionNearPointer()]] (src/Automator.App/MainWindow.xaml.cs:393)
- impact (called by): [[function-cs-701adf3d|Automator.Services.WindowsLauncherService.ActivateAsync(AppBinding,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:86)
- impact (called by): [[function-cs-75032a56|Automator.Services.RoundedAcrylicBackdrop.ReleaseController(ICompositionSupportsSystemBackdrop?)]] (src/Automator.App/Services/RoundedAcrylicBackdrop.cs:128)
- impact (called by): [[function-cs-7aec0ce4|Automator.MainWindow.ShowFallbackMode()]] (src/Automator.App/MainWindow.xaml.cs:361)
- impact (called by): [[function-cs-7fefee1f|Automator.MainWindow.OnGlobalKeyPressed(object?,GlobalKeyEventArgs)]] (src/Automator.App/MainWindow.xaml.cs:577)
- impact (called by): [[function-cs-ae4b309e|Automator.MainWindow.ConfigureNativeCorners()]] (src/Automator.App/MainWindow.xaml.cs:121)
- impact (called by): [[function-cs-ae4b309e|Automator.MainWindow.ConfigureNativeCorners()]] (src/Automator.App/MainWindow.xaml.cs:140)
- impact (called by): [[function-cs-ae4b309e|Automator.MainWindow.ConfigureNativeCorners()]] (src/Automator.App/MainWindow.xaml.cs:175)
- impact (called by): [[function-cs-b6da53cd|Automator.App.OnLaunched(LaunchActivatedEventArgs)]] (src/Automator.App/App.xaml.cs:108)
- impact (called by): [[function-cs-b6da53cd|Automator.App.OnLaunched(LaunchActivatedEventArgs)]] (src/Automator.App/App.xaml.cs:65)
- impact (called by): [[function-cs-b6da53cd|Automator.App.OnLaunched(LaunchActivatedEventArgs)]] (src/Automator.App/App.xaml.cs:83)
- impact (called by): [[function-cs-b8465beb|Automator.Services.KeyboardHookService.Start()]] (src/Automator.App/Services/KeyboardHookService.cs:54)
- impact (called by): [[function-cs-bee7c0b4|Automator.Services.KeyboardHookService.HookThread()]] (src/Automator.App/Services/KeyboardHookService.cs:131)
- impact (called by): [[function-cs-ca01f91b|Automator.Services.SettingsStore.ApplyStartupSetting(bool)]] (src/Automator.App/Services/SettingsStore.cs:100)
- impact (called by): [[function-cs-d1682377|Automator.Services.SettingsStore.Load()]] (src/Automator.App/Services/SettingsStore.cs:26)
- impact (called by): [[function-cs-d20531ac|Automator.MainWindow.OnToggleRequested(object?,EventArgs)]] (src/Automator.App/MainWindow.xaml.cs:456)
- impact (called by): [[function-cs-dff82375|Automator.App.ActivateExistingInstance()]] (src/Automator.App/App.xaml.cs:172)
- impact (called by): [[function-cs-f46e96f0|Automator.Services.WindowsLauncherService.Dispose()]] (src/Automator.App/Services/WindowsLauncherService.cs:200)

## Source anchors

- `src/Automator.App/Services/AppLogger.cs#L72`
