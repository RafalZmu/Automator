---
graph_id: "file-keyboard-hook-service-084333f9"
graph_layer: "file"
graph_kind: "csharp"
tags:
  - "impact-graph"
  - "impact-graph/file"
  - "impact-graph/kind/csharp"
  - "impact-graph/area/desktop"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# KeyboardHookService.cs

Source file src/Automator.App/Services/KeyboardHookService.cs.

## Connections

- contains (declares function): [[function-cs-865b5eb3|Automator.Services.KeyboardHookService.KeyboardHookService(string)]] (src/Automator.App/Services/KeyboardHookService.cs#L32)
- contains (declares function): [[function-cs-b8465beb|Automator.Services.KeyboardHookService.Start()]] (src/Automator.App/Services/KeyboardHookService.cs#L49)
- contains (declares function): [[function-cs-652b2016|Automator.Services.KeyboardHookService.WaitForInstallation(TimeSpan)]] (src/Automator.App/Services/KeyboardHookService.cs#L64)
- contains (declares function): [[function-cs-07fcf74a|Automator.Services.KeyboardHookService.SetOpener(string)]] (src/Automator.App/Services/KeyboardHookService.cs#L67)
- contains (declares function): [[function-cs-74ef1145|Automator.Services.KeyboardHookService.StartRecording()]] (src/Automator.App/Services/KeyboardHookService.cs#L78)
- contains (declares function): [[function-cs-bee7c0b4|Automator.Services.KeyboardHookService.HookThread()]] (src/Automator.App/Services/KeyboardHookService.cs#L83)
- contains (declares function): [[function-cs-3a9d9de5|Automator.Services.KeyboardHookService.HookCallback(int,nint,nint)]] (src/Automator.App/Services/KeyboardHookService.cs#L141)
- contains (declares function): [[function-cs-5b18a52e|Automator.Services.KeyboardHookService.NormalizeModifierKey(uint,uint)]] (src/Automator.App/Services/KeyboardHookService.cs#L166)
- contains (declares function): [[function-cs-7bb14155|Automator.Services.KeyboardHookService.ProcessKey(uint,bool)]] (src/Automator.App/Services/KeyboardHookService.cs#L173)
- contains (declares function): [[function-cs-4d4949d5|Automator.Services.KeyboardHookService.Dispose()]] (src/Automator.App/Services/KeyboardHookService.cs#L230)
- contains (declares function): [[function-cs-7b62d834|Automator.Services.KeyNames.ToVirtualKey(string)]] (src/Automator.App/Services/KeyboardHookService.cs#L251)
- contains (declares function): [[function-cs-98f2bb96|Automator.Services.KeyNames.FromVirtualKey(uint)]] (src/Automator.App/Services/KeyboardHookService.cs#L254)
- contains (declares function): [[function-cs-ffdb1546|Automator.Services.KeyNames.IsModifier(uint)]] (src/Automator.App/Services/KeyboardHookService.cs#L265)
- impact (type-reference): [[file-app-xaml-26b4c462|App.xaml.cs]] (src/Automator.App/App.xaml.cs:119)
- impact (type-reference): [[file-app-xaml-26b4c462|App.xaml.cs]] (src/Automator.App/App.xaml.cs:12)
- impact (type-reference): [[file-app-xaml-26b4c462|App.xaml.cs]] (src/Automator.App/App.xaml.cs:61)
- impact (type-reference): [[file-app-xaml-26b4c462|App.xaml.cs]] (src/Automator.App/App.xaml.cs:67)
- impact (type-reference): [[file-app-xaml-26b4c462|App.xaml.cs]] (src/Automator.App/App.xaml.cs:70)
- impact (type-reference): [[file-app-xaml-26b4c462|App.xaml.cs]] (src/Automator.App/App.xaml.cs:91)
- impact (type-reference): [[file-app-xaml-26b4c462|App.xaml.cs]] (src/Automator.App/App.xaml.cs:92)
- impact (type-reference): [[file-app-xaml-26b4c462|App.xaml.cs]] (src/Automator.App/App.xaml.cs:95)
- impact (type-reference): [[file-app-xaml-26b4c462|App.xaml.cs]] (src/Automator.App/App.xaml.cs:96)
- impact (type-reference): [[file-main-window-xaml-749cf8c0|MainWindow.xaml.cs]] (src/Automator.App/MainWindow.xaml.cs:1000)
- impact (type-reference): [[file-main-window-xaml-749cf8c0|MainWindow.xaml.cs]] (src/Automator.App/MainWindow.xaml.cs:26)
- impact (type-reference): [[file-main-window-xaml-749cf8c0|MainWindow.xaml.cs]] (src/Automator.App/MainWindow.xaml.cs:322)
- impact (type-reference): [[file-main-window-xaml-749cf8c0|MainWindow.xaml.cs]] (src/Automator.App/MainWindow.xaml.cs:355)
- impact (type-reference): [[file-main-window-xaml-749cf8c0|MainWindow.xaml.cs]] (src/Automator.App/MainWindow.xaml.cs:50)
- impact (type-reference): [[file-main-window-xaml-749cf8c0|MainWindow.xaml.cs]] (src/Automator.App/MainWindow.xaml.cs:557)
- impact (type-reference): [[file-main-window-xaml-749cf8c0|MainWindow.xaml.cs]] (src/Automator.App/MainWindow.xaml.cs:562)
- impact (type-reference): [[file-main-window-xaml-749cf8c0|MainWindow.xaml.cs]] (src/Automator.App/MainWindow.xaml.cs:568)
- impact (type-reference): [[file-main-window-xaml-749cf8c0|MainWindow.xaml.cs]] (src/Automator.App/MainWindow.xaml.cs:574)
- impact (type-reference): [[file-main-window-xaml-749cf8c0|MainWindow.xaml.cs]] (src/Automator.App/MainWindow.xaml.cs:578)
- impact (type-reference): [[file-main-window-xaml-749cf8c0|MainWindow.xaml.cs]] (src/Automator.App/MainWindow.xaml.cs:584)
- impact (type-reference): [[file-main-window-xaml-749cf8c0|MainWindow.xaml.cs]] (src/Automator.App/MainWindow.xaml.cs:60)
- impact (type-reference): [[file-main-window-xaml-749cf8c0|MainWindow.xaml.cs]] (src/Automator.App/MainWindow.xaml.cs:71)
- impact (type-reference): [[file-main-window-xaml-749cf8c0|MainWindow.xaml.cs]] (src/Automator.App/MainWindow.xaml.cs:72)
- impact (type-reference): [[file-main-window-xaml-749cf8c0|MainWindow.xaml.cs]] (src/Automator.App/MainWindow.xaml.cs:73)
- impact (type-reference): [[file-main-window-xaml-749cf8c0|MainWindow.xaml.cs]] (src/Automator.App/MainWindow.xaml.cs:873)
- impact (type-reference): [[file-main-window-xaml-749cf8c0|MainWindow.xaml.cs]] (src/Automator.App/MainWindow.xaml.cs:996)

## Source anchors

- `src/Automator.App/Services/KeyboardHookService.cs`
