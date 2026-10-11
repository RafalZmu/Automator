---
graph_id: "function-cs-30d02432"
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

# Automator.Application.Logging.IApplicationLog.Write(ApplicationLogLevel,string,string,Exception?,IReadOnlyDictionary<string, object?>?)

void Write(ApplicationLogLevel level, string eventName, string message, Exception exception, IReadOnlyDictionary properties)

## Connections

- impact (called by): [[function-13015-fb8087b2|lambda@src/Automator.Windows/KeyboardHookService.cs:303]] (src/Automator.Windows/KeyboardHookService.cs:303)
- impact (called by): [[function-6879-5a1ef2c2|lambda@src/Automator.Windows/KeyboardHookService.cs:162]] (src/Automator.Windows/KeyboardHookService.cs:162)
- impact (called by): [[function-cs-026f9fb1|Automator.Application.Automation.AutomationKeyboardEventHub.DispatchAsync(Subscriber)]] (src/Automator.Application/Automation/AutomationKeyboardEventHub.cs:112)
- impact (called by): [[function-cs-0d23090f|Automator.Application.Automation.AutomationKeyboardEventHub.Publish(KeyInputEvent)]] (src/Automator.Application/Automation/AutomationKeyboardEventHub.cs:74)
- impact (called by): [[function-cs-128b33d6|Automator.Application.Automation.AutomationSchedulerCoordinator.SafelyProcessOccurrenceAsync(AutomationScheduleDefinition,DateTimeOffset,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:257)
- impact (called by): [[function-cs-1d77c74b|Automator.Windows.KeyboardHookService.HookThread()]] (src/Automator.Windows/KeyboardHookService.cs:117)
- impact (called by): [[function-cs-1d77c74b|Automator.Windows.KeyboardHookService.HookThread()]] (src/Automator.Windows/KeyboardHookService.cs:122)
- impact (called by): [[function-cs-1d77c74b|Automator.Windows.KeyboardHookService.HookThread()]] (src/Automator.Windows/KeyboardHookService.cs:136)
- impact (called by): [[function-cs-1d77c74b|Automator.Windows.KeyboardHookService.HookThread()]] (src/Automator.Windows/KeyboardHookService.cs:145)
- impact (called by): [[function-cs-1f3283a4|Automator.Windows.WindowsApplicationActivator.ActivateAsync(AppBinding,string?,CancellationToken)]] (src/Automator.Windows/WindowsApplicationActivator.cs:32)
- impact (called by): [[function-cs-1f3283a4|Automator.Windows.WindowsApplicationActivator.ActivateAsync(AppBinding,string?,CancellationToken)]] (src/Automator.Windows/WindowsApplicationActivator.cs:45)
- impact (called by): [[function-cs-1f3283a4|Automator.Windows.WindowsApplicationActivator.ActivateAsync(AppBinding,string?,CancellationToken)]] (src/Automator.Windows/WindowsApplicationActivator.cs:69)
- impact (called by): [[function-cs-1f3283a4|Automator.Windows.WindowsApplicationActivator.ActivateAsync(AppBinding,string?,CancellationToken)]] (src/Automator.Windows/WindowsApplicationActivator.cs:77)
- impact (called by): [[function-cs-1f3283a4|Automator.Windows.WindowsApplicationActivator.ActivateAsync(AppBinding,string?,CancellationToken)]] (src/Automator.Windows/WindowsApplicationActivator.cs:90)
- impact (called by): [[function-cs-1f3283a4|Automator.Windows.WindowsApplicationActivator.ActivateAsync(AppBinding,string?,CancellationToken)]] (src/Automator.Windows/WindowsApplicationActivator.cs:98)
- impact (called by): [[function-cs-27b1ab97|Automator.Application.Automation.AutomationSchedulerCoordinator.NotifyHistoryChangedAsync()]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:375)
- impact (called by): [[function-cs-2ed359b5|Automator.Infrastructure.Automation.AutomationApiProfileRunner.RunProfileAsync(string,JsonElement?,CancellationToken)]] (src/Automator.Infrastructure/Automation/AutomationApiProfileRunner.cs:101)
- impact (called by): [[function-cs-2ed359b5|Automator.Infrastructure.Automation.AutomationApiProfileRunner.RunProfileAsync(string,JsonElement?,CancellationToken)]] (src/Automator.Infrastructure/Automation/AutomationApiProfileRunner.cs:87)
- impact (called by): [[function-cs-41bdc5f2|Automator.Application.Automation.AutomationFocusSessionCoordinator.WorkerLoopAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:276)
- impact (called by): [[function-cs-41bdc5f2|Automator.Application.Automation.AutomationFocusSessionCoordinator.WorkerLoopAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:284)
- impact (called by): [[function-cs-420a0f53|Automator.Windows.WindowsSettingsStore.Export(string,LauncherSettings)]] (src/Automator.Windows/WindowsSettingsStore.cs:124)
- impact (called by): [[function-cs-476a7400|Automator.Windows.WindowsAppCatalog.CaptureUnderCursor()]] (src/Automator.Windows/WindowsAppCatalog.cs:32)
- impact (called by): [[function-cs-476a7400|Automator.Windows.WindowsAppCatalog.CaptureUnderCursor()]] (src/Automator.Windows/WindowsAppCatalog.cs:56)
- impact (called by): [[function-cs-4bf12468|Automator.Application.Automation.AutomationSchedulerCoordinator.WorkerLoopAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:213)
- impact (called by): [[function-cs-4bf12468|Automator.Application.Automation.AutomationSchedulerCoordinator.WorkerLoopAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:219)
- impact (called by): [[function-cs-5083c79b|Automator.Windows.WindowsAppCatalog.Discover(CancellationToken)]] (src/Automator.Windows/WindowsAppCatalog.cs:105)
- impact (called by): [[function-cs-5083c79b|Automator.Windows.WindowsAppCatalog.Discover(CancellationToken)]] (src/Automator.Windows/WindowsAppCatalog.cs:114)
- impact (called by): [[function-cs-5083c79b|Automator.Windows.WindowsAppCatalog.Discover(CancellationToken)]] (src/Automator.Windows/WindowsAppCatalog.cs:90)
- impact (called by): [[function-cs-51272d51|Automator.Infrastructure.Automation.PlaywrightAutomationBrowserService.CloseSessionAsync(string,CancellationToken)]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:172)
- impact (called by): [[function-cs-56a445db|Automator.Windows.ForegroundWindowMonitor.HookThread()]] (src/Automator.Windows/ForegroundWindowMonitor.cs:57)
- impact (called by): [[function-cs-56a445db|Automator.Windows.ForegroundWindowMonitor.HookThread()]] (src/Automator.Windows/ForegroundWindowMonitor.cs:62)
- impact (called by): [[function-cs-5e713aaa|Automator.Windows.WindowsIconCache.GetDataUrl(string)]] (src/Automator.Windows/WindowsIconCache.cs:25)
- impact (called by): [[function-cs-74ac77d8|Automator.Windows.WindowsSettingsStore.WriteWithBackup(string)]] (src/Automator.Windows/WindowsSettingsStore.cs:164)
- impact (called by): [[function-cs-74ac77d8|Automator.Windows.WindowsSettingsStore.WriteWithBackup(string)]] (src/Automator.Windows/WindowsSettingsStore.cs:169)
- impact (called by): [[function-cs-77b3ad9f|Automator.Application.Automation.AutomationFocusSessionCoordinator.ProcessDuePhaseAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:326)
- impact (called by): [[function-cs-8737a8ad|Automator.Windows.WindowsApplicationActivator.RestoreAndMaximizeAsync(nint,string,CancellationToken)]] (src/Automator.Windows/WindowsApplicationActivator.cs:119)
- impact (called by): [[function-cs-8737a8ad|Automator.Windows.WindowsApplicationActivator.RestoreAndMaximizeAsync(nint,string,CancellationToken)]] (src/Automator.Windows/WindowsApplicationActivator.cs:137)
- impact (called by): [[function-cs-8955725a|Automator.Infrastructure.Automation.SharedHttpService.WriteLog(string,string?,string,long,int?,AutomationServiceErrorCategory?)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs:467)
- impact (called by): [[function-cs-994ec76c|Automator.Application.Automation.AutomationSchedulerCoordinator.StartAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:85)
- impact (called by): [[function-cs-a1c9db57|Automator.Windows.WindowsIconCache.ExtractDataUrl(string)]] (src/Automator.Windows/WindowsIconCache.cs:45)
- impact (called by): [[function-cs-a1c9db57|Automator.Windows.WindowsIconCache.ExtractDataUrl(string)]] (src/Automator.Windows/WindowsIconCache.cs:51)
- impact (called by): [[function-cs-b4327623|Automator.Windows.WindowsSettingsStore.Import(string)]] (src/Automator.Windows/WindowsSettingsStore.cs:103)
- impact (called by): [[function-cs-bb2ddc62|Automator.Windows.WindowsSettingsStore.Import(LauncherSettings)]] (src/Automator.Windows/WindowsSettingsStore.cs:114)
- impact (called by): [[function-cs-bbf0aede|Automator.Windows.NativeChromeController.Apply(nint)]] (src/Automator.Windows/NativeChromeController.cs:22)
- impact (called by): [[function-cs-bbf0aede|Automator.Windows.NativeChromeController.Apply(nint)]] (src/Automator.Windows/NativeChromeController.cs:26)
- impact (called by): [[function-cs-d521a055|Automator.Application.Automation.AutomationSchedulerCoordinator.ProcessDueSchedulesAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:239)
- impact (called by): [[function-cs-db1673b5|Automator.Application.Automation.AutomationSchedulerCoordinator.RunScheduleCoreAsync(AutomationScheduleDefinition,DateTimeOffset,bool,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:350)
- impact (called by): [[function-cs-edbebc2d|Automator.Windows.WindowsSettingsStore.ApplyStartupSetting(bool)]] (src/Automator.Windows/WindowsSettingsStore.cs:132)
- impact (called by): [[function-cs-edbebc2d|Automator.Windows.WindowsSettingsStore.ApplyStartupSetting(bool)]] (src/Automator.Windows/WindowsSettingsStore.cs:143)
- impact (called by): [[function-cs-edbebc2d|Automator.Windows.WindowsSettingsStore.ApplyStartupSetting(bool)]] (src/Automator.Windows/WindowsSettingsStore.cs:149)
- impact (called by): [[function-cs-f0c28852|Automator.Infrastructure.Automation.PlaywrightAutomationBrowserService.ExecuteAsync(string,AutomationBrowserAction,CancellationToken)]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:104)
- impact (called by): [[function-cs-f0c28852|Automator.Infrastructure.Automation.PlaywrightAutomationBrowserService.ExecuteAsync(string,AutomationBrowserAction,CancellationToken)]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:150)
- impact (called by): [[function-cs-fdaacf4b|Automator.Windows.WindowsSettingsStore.Load()]] (src/Automator.Windows/WindowsSettingsStore.cs:61)
- impact (called by): [[function-cs-fdaacf4b|Automator.Windows.WindowsSettingsStore.Load()]] (src/Automator.Windows/WindowsSettingsStore.cs:70)

## Source anchors

- `src/Automator.Application/Logging/IApplicationLog.cs#L13`
