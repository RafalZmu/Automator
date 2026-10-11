---
graph_id: "function-cs-8a8d3172"
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

# Automator.Services.AppLogger.Error(string,string,Exception?,IReadOnlyDictionary<string, object?>?)

public static void Error(string eventName, string message, Exception exception, IReadOnlyDictionary properties)

## Connections

- impact (called by): [[function-1243-9541fed8|lambda@src/Automator.App/App.xaml.cs:30]] (src/Automator.App/App.xaml.cs:30)
- impact (called by): [[function-327-57acf19c|lambda@src/Automator.App/Services/AppCatalogService.cs:12]] (src/Automator.App/Services/AppCatalogService.cs:17)
- impact (called by): [[function-cs-03b76bea|Automator.MainWindow.ActivatePanelWindow(string)]] (src/Automator.App/MainWindow.xaml.cs:521)
- impact (called by): [[function-cs-0b9625f2|Automator.Services.TrayIconService.TrayIconService(nint,Action,Action,Action,Action)]] (src/Automator.App/Services/TrayIconService.cs:33)
- impact (called by): [[function-cs-0f149492|Automator.MainWindow.ShowSettingsDialogAsync()]] (src/Automator.App/MainWindow.xaml.cs:933)
- impact (called by): [[function-cs-15720945|Automator.MainWindow.LaunchBindingAsync(AppBinding)]] (src/Automator.App/MainWindow.xaml.cs:652)
- impact (called by): [[function-cs-15720945|Automator.MainWindow.LaunchBindingAsync(AppBinding)]] (src/Automator.App/MainWindow.xaml.cs:662)
- impact (called by): [[function-cs-215136bc|Automator.MainWindow.ImportSettingsAsync(Action?)]] (src/Automator.App/MainWindow.xaml.cs:1011)
- impact (called by): [[function-cs-260d6f16|Automator.MainWindow.ConfigureAcrylicBackdrop()]] (src/Automator.App/MainWindow.xaml.cs:218)
- impact (called by): [[function-cs-2b4ab1f4|Automator.MainWindow.OpenCatalogAsync()]] (src/Automator.App/MainWindow.xaml.cs:694)
- impact (called by): [[function-cs-36411d21|Automator.MainWindow.ShowHookFailure(int)]] (src/Automator.App/MainWindow.xaml.cs:369)
- impact (called by): [[function-cs-3a9d9de5|Automator.Services.KeyboardHookService.HookCallback(int,nint,nint)]] (src/Automator.App/Services/KeyboardHookService.cs:159)
- impact (called by): [[function-cs-45dd5f08|Automator.MainWindow.ExportSettingsAsync()]] (src/Automator.App/MainWindow.xaml.cs:959)
- impact (called by): [[function-cs-491301ee|Automator.MainWindow.OpenLogFolder()]] (src/Automator.App/MainWindow.xaml.cs:980)
- impact (called by): [[function-cs-5823fce6|Automator.MainWindow.OnAddCustomAppClicked(object,RoutedEventArgs)]] (src/Automator.App/MainWindow.xaml.cs:828)
- impact (called by): [[function-cs-60d7018b|Automator.Services.SettingsStore.Save(LauncherSettings)]] (src/Automator.App/Services/SettingsStore.cs:46)
- impact (called by): [[function-cs-867bbb9b|Automator.Services.RoundedAcrylicBackdrop.OnTargetConnected(ICompositionSupportsSystemBackdrop,XamlRoot)]] (src/Automator.App/Services/RoundedAcrylicBackdrop.cs:73)
- impact (called by): [[function-cs-9e6eace8|Automator.Services.SettingsStore.Export(string,LauncherSettings)]] (src/Automator.App/Services/SettingsStore.cs:65)
- impact (called by): [[function-cs-b3967ee1|Automator.Plugins.LauncherTabModule.ExecuteAsync(string,object?,CancellationToken)]] (src/Automator.App/Plugins/LauncherTabModule.cs:34)
- impact (called by): [[function-cs-b6da53cd|Automator.App.OnLaunched(LaunchActivatedEventArgs)]] (src/Automator.App/App.xaml.cs:94)
- impact (called by): [[function-cs-ba1d3698|Automator.Services.TrayIconService.InvokeSafely(Action,string)]] (src/Automator.App/Services/TrayIconService.cs:105)
- impact (called by): [[function-cs-bee7c0b4|Automator.Services.KeyboardHookService.HookThread()]] (src/Automator.App/Services/KeyboardHookService.cs:112)
- impact (called by): [[function-cs-bee7c0b4|Automator.Services.KeyboardHookService.HookThread()]] (src/Automator.App/Services/KeyboardHookService.cs:123)
- impact (called by): [[function-cs-bee7c0b4|Automator.Services.KeyboardHookService.HookThread()]] (src/Automator.App/Services/KeyboardHookService.cs:95)
- impact (called by): [[function-cs-df6f3bd1|Automator.App.ExitApplication()]] (src/Automator.App/App.xaml.cs:118)
- impact (called by): [[function-cs-df6f3bd1|Automator.App.ExitApplication()]] (src/Automator.App/App.xaml.cs:120)
- impact (called by): [[function-cs-df6f3bd1|Automator.App.ExitApplication()]] (src/Automator.App/App.xaml.cs:122)
- impact (called by): [[function-cs-df6f3bd1|Automator.App.ExitApplication()]] (src/Automator.App/App.xaml.cs:133)
- impact (called by): [[function-cs-f47f8f75|Automator.MainWindow.SaveSettings()]] (src/Automator.App/MainWindow.xaml.cs:1062)
- impact (called by): [[function-cs-fe85137b|Automator.Services.SettingsStore.Import(string)]] (src/Automator.App/Services/SettingsStore.cs:85)

## Source anchors

- `src/Automator.App/Services/AppLogger.cs#L76`
