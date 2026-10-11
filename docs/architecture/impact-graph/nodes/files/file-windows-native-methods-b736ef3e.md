---
graph_id: "file-windows-native-methods-b736ef3e"
graph_layer: "file"
graph_kind: "csharp"
tags:
  - "impact-graph"
  - "impact-graph/file"
  - "impact-graph/kind/csharp"
  - "impact-graph/area/windows"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# WindowsNativeMethods.cs

Source file src/Automator.Windows/Interop/WindowsNativeMethods.cs.

## Connections

- contains (declares function): [[function-cs-f5c2340b|Automator.Windows.Interop.WindowsNativeMethods.SetWindowsHookEx(int,LowLevelKeyboardProc,nint,uint)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L59)
- contains (declares function): [[function-cs-12f58257|Automator.Windows.Interop.WindowsNativeMethods.UnhookWindowsHookEx(nint)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L61)
- contains (declares function): [[function-cs-e17a0de1|Automator.Windows.Interop.WindowsNativeMethods.CallNextHookEx(nint,int,nint,nint)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L63)
- contains (declares function): [[function-cs-8c56dd43|Automator.Windows.Interop.WindowsNativeMethods.GetMessage(Message,nint,uint,uint)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L65)
- contains (declares function): [[function-cs-06f7223e|Automator.Windows.Interop.WindowsNativeMethods.PeekMessage(Message,nint,uint,uint,uint)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L67)
- contains (declares function): [[function-cs-39bcacb6|Automator.Windows.Interop.WindowsNativeMethods.TranslateMessage(Message)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L69)
- contains (declares function): [[function-cs-9db6518c|Automator.Windows.Interop.WindowsNativeMethods.DispatchMessage(Message)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L71)
- contains (declares function): [[function-cs-f3488f9b|Automator.Windows.Interop.WindowsNativeMethods.PostThreadMessage(uint,uint,nuint,nint)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L73)
- contains (declares function): [[function-cs-74f1a7b3|Automator.Windows.Interop.WindowsNativeMethods.GetCurrentThreadId()]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L75)
- contains (declares function): [[function-cs-6855ddf8|Automator.Windows.Interop.WindowsNativeMethods.GetModuleHandle(string?)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L77)
- contains (declares function): [[function-cs-2e42c7ab|Automator.Windows.Interop.WindowsNativeMethods.GetForegroundWindow()]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L80)
- contains (declares function): [[function-cs-b9b4c1a2|Automator.Windows.Interop.WindowsNativeMethods.GetAsyncKeyState(int)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L82)
- contains (declares function): [[function-cs-70774901|Automator.Windows.Interop.WindowsNativeMethods.GetWindowThreadProcessId(nint,uint)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L84)
- contains (declares function): [[function-cs-227cb77a|Automator.Windows.Interop.WindowsNativeMethods.IsWindowVisible(nint)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L86)
- contains (declares function): [[function-cs-25fecf8e|Automator.Windows.Interop.WindowsNativeMethods.IsIconic(nint)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L88)
- contains (declares function): [[function-cs-a4c1cf06|Automator.Windows.Interop.WindowsNativeMethods.IsWindow(nint)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L90)
- contains (declares function): [[function-cs-92774ec4|Automator.Windows.Interop.WindowsNativeMethods.ShowWindowAsync(nint,int)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L92)
- contains (declares function): [[function-cs-1c4e9894|Automator.Windows.Interop.WindowsNativeMethods.SetForegroundWindow(nint)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L94)
- contains (declares function): [[function-cs-c047ee24|Automator.Windows.Interop.WindowsNativeMethods.BringWindowToTop(nint)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L96)
- contains (declares function): [[function-cs-68bb0ada|Automator.Windows.Interop.WindowsNativeMethods.AttachThreadInput(uint,uint,bool)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L98)
- contains (declares function): [[function-cs-3ed28f4c|Automator.Windows.Interop.WindowsNativeMethods.EnumWindows(EnumWindowsProc,nint)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L100)
- contains (declares function): [[function-cs-50a90ad3|Automator.Windows.Interop.WindowsNativeMethods.GetCursorPos(Point)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L102)
- contains (declares function): [[function-cs-41c56f8b|Automator.Windows.Interop.WindowsNativeMethods.WindowFromPoint(Point)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L104)
- contains (declares function): [[function-cs-bbf53b70|Automator.Windows.Interop.WindowsNativeMethods.GetAncestor(nint,uint)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L106)
- contains (declares function): [[function-cs-4ce6dc36|Automator.Windows.Interop.WindowsNativeMethods.GetWindowText(nint,StringBuilder,int)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L108)
- contains (declares function): [[function-cs-d10965af|Automator.Windows.Interop.WindowsNativeMethods.SetWinEventHook(uint,uint,nint,WinEventProc,uint,uint,uint)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L110)
- contains (declares function): [[function-cs-41c7fc2e|Automator.Windows.Interop.WindowsNativeMethods.UnhookWinEvent(nint)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L112)
- contains (declares function): [[function-cs-09c625d8|Automator.Windows.Interop.WindowsNativeMethods.DwmSetWindowAttribute(nint,uint,uint,uint)]] (src/Automator.Windows/Interop/WindowsNativeMethods.cs#L115)
- impact (type-reference): [[file-foreground-window-monitor-53cb684b|ForegroundWindowMonitor.cs]] (src/Automator.Windows/ForegroundWindowMonitor.cs:15)
- impact (type-reference): [[file-foreground-window-monitor-53cb684b|ForegroundWindowMonitor.cs]] (src/Automator.Windows/ForegroundWindowMonitor.cs:25)
- impact (type-reference): [[file-foreground-window-monitor-53cb684b|ForegroundWindowMonitor.cs]] (src/Automator.Windows/ForegroundWindowMonitor.cs:44)
- impact (type-reference): [[file-foreground-window-monitor-53cb684b|ForegroundWindowMonitor.cs]] (src/Automator.Windows/ForegroundWindowMonitor.cs:45)
- impact (type-reference): [[file-foreground-window-monitor-53cb684b|ForegroundWindowMonitor.cs]] (src/Automator.Windows/ForegroundWindowMonitor.cs:46)
- impact (type-reference): [[file-foreground-window-monitor-53cb684b|ForegroundWindowMonitor.cs]] (src/Automator.Windows/ForegroundWindowMonitor.cs:47)
- impact (type-reference): [[file-foreground-window-monitor-53cb684b|ForegroundWindowMonitor.cs]] (src/Automator.Windows/ForegroundWindowMonitor.cs:48)
- impact (type-reference): [[file-foreground-window-monitor-53cb684b|ForegroundWindowMonitor.cs]] (src/Automator.Windows/ForegroundWindowMonitor.cs:50)
- impact (type-reference): [[file-foreground-window-monitor-53cb684b|ForegroundWindowMonitor.cs]] (src/Automator.Windows/ForegroundWindowMonitor.cs:53)
- impact (type-reference): [[file-foreground-window-monitor-53cb684b|ForegroundWindowMonitor.cs]] (src/Automator.Windows/ForegroundWindowMonitor.cs:73)
- impact (type-reference): [[file-foreground-window-monitor-53cb684b|ForegroundWindowMonitor.cs]] (src/Automator.Windows/ForegroundWindowMonitor.cs:75)
- impact (type-reference): [[file-foreground-window-monitor-53cb684b|ForegroundWindowMonitor.cs]] (src/Automator.Windows/ForegroundWindowMonitor.cs:76)
- impact (type-reference): [[file-foreground-window-monitor-53cb684b|ForegroundWindowMonitor.cs]] (src/Automator.Windows/ForegroundWindowMonitor.cs:83)
- impact (type-reference): [[file-foreground-window-monitor-53cb684b|ForegroundWindowMonitor.cs]] (src/Automator.Windows/ForegroundWindowMonitor.cs:99)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:107)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:108)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:111)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:112)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:127)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:129)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:130)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:144)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:157)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:166)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:169)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:172)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:197)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:214)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:23)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:278)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:300)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:309)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:310)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:319)
- impact (type-reference): [[file-keyboard-hook-service-4b1eb174|KeyboardHookService.cs]] (src/Automator.Windows/KeyboardHookService.cs:53)
- impact (type-reference): [[file-native-chrome-controller-aee8a59f|NativeChromeController.cs]] (src/Automator.Windows/NativeChromeController.cs:14)
- impact (type-reference): [[file-native-chrome-controller-aee8a59f|NativeChromeController.cs]] (src/Automator.Windows/NativeChromeController.cs:15)
- impact (type-reference): [[file-native-chrome-controller-aee8a59f|NativeChromeController.cs]] (src/Automator.Windows/NativeChromeController.cs:16)
- impact (type-reference): [[file-native-chrome-controller-aee8a59f|NativeChromeController.cs]] (src/Automator.Windows/NativeChromeController.cs:17)
- impact (type-reference): [[file-native-chrome-controller-aee8a59f|NativeChromeController.cs]] (src/Automator.Windows/NativeChromeController.cs:18)
- impact (type-reference): [[file-native-chrome-controller-aee8a59f|NativeChromeController.cs]] (src/Automator.Windows/NativeChromeController.cs:19)
- impact (type-reference): [[file-windows-app-catalog-6686d6e2|WindowsAppCatalog.cs]] (src/Automator.Windows/WindowsAppCatalog.cs:30)
- impact (type-reference): [[file-windows-app-catalog-6686d6e2|WindowsAppCatalog.cs]] (src/Automator.Windows/WindowsAppCatalog.cs:37)
- impact (type-reference): [[file-windows-app-catalog-6686d6e2|WindowsAppCatalog.cs]] (src/Automator.Windows/WindowsAppCatalog.cs:39)
- impact (type-reference): [[file-windows-app-catalog-6686d6e2|WindowsAppCatalog.cs]] (src/Automator.Windows/WindowsAppCatalog.cs:48)
- impact (type-reference): [[file-windows-application-activator-fa3e606b|WindowsApplicationActivator.cs]] (src/Automator.Windows/WindowsApplicationActivator.cs:106)
- impact (type-reference): [[file-windows-application-activator-fa3e606b|WindowsApplicationActivator.cs]] (src/Automator.Windows/WindowsApplicationActivator.cs:107)
- impact (type-reference): [[file-windows-application-activator-fa3e606b|WindowsApplicationActivator.cs]] (src/Automator.Windows/WindowsApplicationActivator.cs:108)
- impact (type-reference): [[file-windows-application-activator-fa3e606b|WindowsApplicationActivator.cs]] (src/Automator.Windows/WindowsApplicationActivator.cs:112)
- impact (type-reference): [[file-windows-application-activator-fa3e606b|WindowsApplicationActivator.cs]] (src/Automator.Windows/WindowsApplicationActivator.cs:156)
- impact (type-reference): [[file-windows-application-activator-fa3e606b|WindowsApplicationActivator.cs]] (src/Automator.Windows/WindowsApplicationActivator.cs:158)
- impact (type-reference): [[file-windows-application-activator-fa3e606b|WindowsApplicationActivator.cs]] (src/Automator.Windows/WindowsApplicationActivator.cs:159)
- impact (type-reference): [[file-windows-application-activator-fa3e606b|WindowsApplicationActivator.cs]] (src/Automator.Windows/WindowsApplicationActivator.cs:44)
- impact (type-reference): [[file-windows-application-activator-fa3e606b|WindowsApplicationActivator.cs]] (src/Automator.Windows/WindowsApplicationActivator.cs:85)
- impact (type-reference): [[file-windows-foreground-window-61234581|WindowsForegroundWindow.cs]] (src/Automator.Windows/WindowsForegroundWindow.cs:16)
- impact (type-reference): [[file-windows-foreground-window-61234581|WindowsForegroundWindow.cs]] (src/Automator.Windows/WindowsForegroundWindow.cs:21)
- impact (type-reference): [[file-windows-foreground-window-61234581|WindowsForegroundWindow.cs]] (src/Automator.Windows/WindowsForegroundWindow.cs:24)
- impact (type-reference): [[file-windows-foreground-window-61234581|WindowsForegroundWindow.cs]] (src/Automator.Windows/WindowsForegroundWindow.cs:25)
- impact (type-reference): [[file-windows-foreground-window-61234581|WindowsForegroundWindow.cs]] (src/Automator.Windows/WindowsForegroundWindow.cs:31)
- impact (type-reference): [[file-windows-foreground-window-61234581|WindowsForegroundWindow.cs]] (src/Automator.Windows/WindowsForegroundWindow.cs:32)
- impact (type-reference): [[file-windows-foreground-window-61234581|WindowsForegroundWindow.cs]] (src/Automator.Windows/WindowsForegroundWindow.cs:33)
- impact (type-reference): [[file-windows-foreground-window-61234581|WindowsForegroundWindow.cs]] (src/Automator.Windows/WindowsForegroundWindow.cs:34)
- impact (type-reference): [[file-windows-foreground-window-61234581|WindowsForegroundWindow.cs]] (src/Automator.Windows/WindowsForegroundWindow.cs:42)
- impact (type-reference): [[file-windows-foreground-window-61234581|WindowsForegroundWindow.cs]] (src/Automator.Windows/WindowsForegroundWindow.cs:48)
- impact (type-reference): [[file-windows-foreground-window-61234581|WindowsForegroundWindow.cs]] (src/Automator.Windows/WindowsForegroundWindow.cs:49)
- impact (type-reference): [[file-windows-foreground-window-61234581|WindowsForegroundWindow.cs]] (src/Automator.Windows/WindowsForegroundWindow.cs:53)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs`
