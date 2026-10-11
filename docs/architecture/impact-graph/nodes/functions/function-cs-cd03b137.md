---
graph_id: "function-cs-cd03b137"
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

# Automator.Services.AppLogger.Info(string,string,IReadOnlyDictionary<string, object?>?)

public static void Info(string eventName, string message, IReadOnlyDictionary properties)

## Connections

- impact (called by): [[function-25782-572fc698|lambda@src/Automator.App/MainWindow.xaml.cs:581]] (src/Automator.App/MainWindow.xaml.cs:585)
- impact (called by): [[function-cs-03b76bea|Automator.MainWindow.ActivatePanelWindow(string)]] (src/Automator.App/MainWindow.xaml.cs:467)
- impact (called by): [[function-cs-03b76bea|Automator.MainWindow.ActivatePanelWindow(string)]] (src/Automator.App/MainWindow.xaml.cs:551)
- impact (called by): [[function-cs-044cf79d|Automator.Services.AppCatalogService.Discover()]] (src/Automator.App/Services/AppCatalogService.cs:117)
- impact (called by): [[function-cs-044cf79d|Automator.Services.AppCatalogService.Discover()]] (src/Automator.App/Services/AppCatalogService.cs:75)
- impact (called by): [[function-cs-0bea5c60|Automator.MainWindow.HidePanel(string)]] (src/Automator.App/MainWindow.xaml.cs:347)
- impact (called by): [[function-cs-103bed2e|Automator.MainWindow.OnWindowActivated(object,WindowActivatedEventArgs)]] (src/Automator.App/MainWindow.xaml.cs:424)
- impact (called by): [[function-cs-103bed2e|Automator.MainWindow.OnWindowActivated(object,WindowActivatedEventArgs)]] (src/Automator.App/MainWindow.xaml.cs:443)
- impact (called by): [[function-cs-15720945|Automator.MainWindow.LaunchBindingAsync(AppBinding)]] (src/Automator.App/MainWindow.xaml.cs:645)
- impact (called by): [[function-cs-260d6f16|Automator.MainWindow.ConfigureAcrylicBackdrop()]] (src/Automator.App/MainWindow.xaml.cs:204)
- impact (called by): [[function-cs-2b4ab1f4|Automator.MainWindow.OpenCatalogAsync()]] (src/Automator.App/MainWindow.xaml.cs:689)
- impact (called by): [[function-cs-2b9a6aa4|Automator.MainWindow.ShowSettings()]] (src/Automator.App/MainWindow.xaml.cs:842)
- impact (called by): [[function-cs-3d03d356|Automator.Services.WindowsLauncherService.WindowsLauncherService()]] (src/Automator.App/Services/WindowsLauncherService.cs:30)
- impact (called by): [[function-cs-3da4f18c|Automator.Services.WindowsLauncherService.WaitAndMaximizeAsync(string,Process,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:108)
- impact (called by): [[function-cs-3da4f18c|Automator.Services.WindowsLauncherService.WaitAndMaximizeAsync(string,Process,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:123)
- impact (called by): [[function-cs-3da4f18c|Automator.Services.WindowsLauncherService.WaitAndMaximizeAsync(string,Process,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:140)
- impact (called by): [[function-cs-3da4f18c|Automator.Services.WindowsLauncherService.WaitAndMaximizeAsync(string,Process,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:96)
- impact (called by): [[function-cs-491301ee|Automator.MainWindow.OpenLogFolder()]] (src/Automator.App/MainWindow.xaml.cs:976)
- impact (called by): [[function-cs-4d4949d5|Automator.Services.KeyboardHookService.Dispose()]] (src/Automator.App/Services/KeyboardHookService.cs:234)
- impact (called by): [[function-cs-4ff2d6b7|Automator.MainWindow.ActivateLauncher()]] (src/Automator.App/MainWindow.xaml.cs:339)
- impact (called by): [[function-cs-59eb673a|Automator.MainWindow.DisposeServices()]] (src/Automator.App/MainWindow.xaml.cs:1076)
- impact (called by): [[function-cs-5fe9fc70|Automator.MainWindow.PositionNearPointer()]] (src/Automator.App/MainWindow.xaml.cs:405)
- impact (called by): [[function-cs-60d7018b|Automator.Services.SettingsStore.Save(LauncherSettings)]] (src/Automator.App/Services/SettingsStore.cs:38)
- impact (called by): [[function-cs-701adf3d|Automator.Services.WindowsLauncherService.ActivateAsync(AppBinding,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:37)
- impact (called by): [[function-cs-701adf3d|Automator.Services.WindowsLauncherService.ActivateAsync(AppBinding,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:48)
- impact (called by): [[function-cs-701adf3d|Automator.Services.WindowsLauncherService.ActivateAsync(AppBinding,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:59)
- impact (called by): [[function-cs-701adf3d|Automator.Services.WindowsLauncherService.ActivateAsync(AppBinding,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:76)
- impact (called by): [[function-cs-7bb14155|Automator.Services.KeyboardHookService.ProcessKey(uint,bool)]] (src/Automator.App/Services/KeyboardHookService.cs:195)
- impact (called by): [[function-cs-7bb14155|Automator.Services.KeyboardHookService.ProcessKey(uint,bool)]] (src/Automator.App/Services/KeyboardHookService.cs:205)
- impact (called by): [[function-cs-7bb14155|Automator.Services.KeyboardHookService.ProcessKey(uint,bool)]] (src/Automator.App/Services/KeyboardHookService.cs:220)
- impact (called by): [[function-cs-867bbb9b|Automator.Services.RoundedAcrylicBackdrop.OnTargetConnected(ICompositionSupportsSystemBackdrop,XamlRoot)]] (src/Automator.App/Services/RoundedAcrylicBackdrop.cs:59)
- impact (called by): [[function-cs-9a4ddb4d|Automator.MainWindow.TogglePanel()]] (src/Automator.App/MainWindow.xaml.cs:323)
- impact (called by): [[function-cs-9e6eace8|Automator.Services.SettingsStore.Export(string,LauncherSettings)]] (src/Automator.App/Services/SettingsStore.cs:57)
- impact (called by): [[function-cs-ae4b309e|Automator.MainWindow.ConfigureNativeCorners()]] (src/Automator.App/MainWindow.xaml.cs:157)
- impact (called by): [[function-cs-b6da53cd|Automator.App.OnLaunched(LaunchActivatedEventArgs)]] (src/Automator.App/App.xaml.cs:100)
- impact (called by): [[function-cs-b6da53cd|Automator.App.OnLaunched(LaunchActivatedEventArgs)]] (src/Automator.App/App.xaml.cs:111)
- impact (called by): [[function-cs-b6da53cd|Automator.App.OnLaunched(LaunchActivatedEventArgs)]] (src/Automator.App/App.xaml.cs:49)
- impact (called by): [[function-cs-b6da53cd|Automator.App.OnLaunched(LaunchActivatedEventArgs)]] (src/Automator.App/App.xaml.cs:52)
- impact (called by): [[function-cs-b6da53cd|Automator.App.OnLaunched(LaunchActivatedEventArgs)]] (src/Automator.App/App.xaml.cs:88)
- impact (called by): [[function-cs-b8465beb|Automator.Services.KeyboardHookService.Start()]] (src/Automator.App/Services/KeyboardHookService.cs:58)
- impact (called by): [[function-cs-bee7c0b4|Automator.Services.KeyboardHookService.HookThread()]] (src/Automator.App/Services/KeyboardHookService.cs:102)
- impact (called by): [[function-cs-bee7c0b4|Automator.Services.KeyboardHookService.HookThread()]] (src/Automator.App/Services/KeyboardHookService.cs:137)
- impact (called by): [[function-cs-bee7c0b4|Automator.Services.KeyboardHookService.HookThread()]] (src/Automator.App/Services/KeyboardHookService.cs:86)
- impact (called by): [[function-cs-ca01f91b|Automator.Services.SettingsStore.ApplyStartupSetting(bool)]] (src/Automator.App/Services/SettingsStore.cs:107)
- impact (called by): [[function-cs-ca01f91b|Automator.Services.SettingsStore.ApplyStartupSetting(bool)]] (src/Automator.App/Services/SettingsStore.cs:112)
- impact (called by): [[function-cs-d20531ac|Automator.MainWindow.OnToggleRequested(object?,EventArgs)]] (src/Automator.App/MainWindow.xaml.cs:451)
- impact (called by): [[function-cs-d20531ac|Automator.MainWindow.OnToggleRequested(object?,EventArgs)]] (src/Automator.App/MainWindow.xaml.cs:453)
- impact (called by): [[function-cs-da1cf4cd|Automator.Services.RoundedAcrylicBackdrop.OnControllerStateChanged(object,object)]] (src/Automator.App/Services/RoundedAcrylicBackdrop.cs:109)
- impact (called by): [[function-cs-df00b86e|Automator.MainWindow.MainWindow(LauncherSettings,SettingsStore,KeyboardHookService)]] (src/Automator.App/MainWindow.xaml.cs:52)
- impact (called by): [[function-cs-df00b86e|Automator.MainWindow.MainWindow(LauncherSettings,SettingsStore,KeyboardHookService)]] (src/Automator.App/MainWindow.xaml.cs:74)
- impact (called by): [[function-cs-df6f3bd1|Automator.App.ExitApplication()]] (src/Automator.App/App.xaml.cs:116)
- impact (called by): [[function-cs-df6f3bd1|Automator.App.ExitApplication()]] (src/Automator.App/App.xaml.cs:137)
- impact (called by): [[function-cs-dff82375|Automator.App.ActivateExistingInstance()]] (src/Automator.App/App.xaml.cs:159)
- impact (called by): [[function-cs-e0917e34|Automator.Services.TrayIconService.Dispose()]] (src/Automator.App/Services/TrayIconService.cs:132)
- impact (called by): [[function-cs-fb37ca37|Automator.App.App()]] (src/Automator.App/App.xaml.cs:18)
- impact (called by): [[function-cs-fb37ca37|Automator.App.App()]] (src/Automator.App/App.xaml.cs:36)
- impact (called by): [[function-cs-fb37ca37|Automator.App.App()]] (src/Automator.App/App.xaml.cs:42)
- impact (called by): [[function-cs-fe6e446c|Automator.Services.TrayIconService.WindowProc(nint,uint,nuint,nint,nuint,nuint)]] (src/Automator.App/Services/TrayIconService.cs:66)
- impact (called by): [[function-cs-fe6e446c|Automator.Services.TrayIconService.WindowProc(nint,uint,nuint,nint,nuint,nuint)]] (src/Automator.App/Services/TrayIconService.cs:76)
- impact (called by): [[function-cs-fe6e446c|Automator.Services.TrayIconService.WindowProc(nint,uint,nuint,nint,nuint,nuint)]] (src/Automator.App/Services/TrayIconService.cs:83)
- impact (called by): [[function-cs-fe85137b|Automator.Services.SettingsStore.Import(string)]] (src/Automator.App/Services/SettingsStore.cs:76)

## Source anchors

- `src/Automator.App/Services/AppLogger.cs#L69`
