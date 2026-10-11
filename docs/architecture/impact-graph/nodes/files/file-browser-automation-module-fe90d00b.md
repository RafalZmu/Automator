---
graph_id: "file-browser-automation-module-fe90d00b"
graph_layer: "file"
graph_kind: "csharp"
tags:
  - "impact-graph"
  - "impact-graph/file"
  - "impact-graph/kind/csharp"
  - "impact-graph/area/browser"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# BrowserAutomationModule.cs

Source file src/Automator.Application/Automation/BrowserAutomationModule.cs.

## Connections

- contains (declares function): [[function-cs-b537bfc3|Automator.Application.Automation.BrowserAutomationModule.BrowserAutomationModule(IAutomationVariableProvider?)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L25)
- contains (declares function): [[function-cs-64753001|Automator.Application.Automation.BrowserAutomationModule.CreateInitialState()]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L71)
- contains (declares function): [[function-cs-5dddb001|Automator.Application.Automation.BrowserAutomationModule.CreateDefaultSettings()]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L74)
- contains (declares function): [[function-cs-7ccfa9df|Automator.Application.Automation.BrowserAutomationModule.MigrateSettings(int,JsonElement)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L76)
- contains (declares function): [[browser-module-execute|Automator.Application.Automation.BrowserAutomationModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L79)
- contains (declares function): [[browser-create-explorer|Automator.Application.Automation.BrowserAutomationModule.CreateExplorer(AutomationServicesContext)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L134)
- contains (declares function): [[function-cs-db9d20d4|Automator.Application.Automation.BrowserAutomationModule.ListProfilesAsync(AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L137)
- contains (declares function): [[function-8415-25cc7801|lambda@src/Automator.Application/Automation/BrowserAutomationModule.cs:140]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L140)
- contains (declares function): [[function-8502-0701334c|lambda@src/Automator.Application/Automation/BrowserAutomationModule.cs:141]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L141)
- contains (declares function): [[function-cs-c5c06584|Automator.Application.Automation.BrowserAutomationModule.SaveProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L150)
- contains (declares function): [[function-cs-e729abf2|Automator.Application.Automation.BrowserAutomationModule.DeleteProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L170)
- contains (declares function): [[function-cs-1380e8ef|Automator.Application.Automation.BrowserAutomationModule.GetRuntimeStatusAsync(AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L180)
- contains (declares function): [[function-cs-580ac465|Automator.Application.Automation.BrowserAutomationModule.RunActionAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L197)
- contains (declares function): [[function-cs-41e9841d|Automator.Application.Automation.BrowserAutomationModule.CloseSessionAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L216)
- contains (declares function): [[function-cs-4224ed33|Automator.Application.Automation.BrowserAutomationModule.RequireProfileAsync(string,IAutomationLibrary,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L224)
- contains (declares function): [[function-cs-3c15c57d|Automator.Application.Automation.BrowserAutomationModule.ReadAction(JsonElement,BrowserAutomationProfile)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L234)
- contains (declares function): [[function-cs-a5137743|Automator.Application.Automation.BrowserAutomationModule.BoundResult(AutomationBrowserActionResult,BrowserAutomationProfile)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L256)
- contains (declares function): [[function-15641-56a02ab3|lambda@src/Automator.Application/Automation/BrowserAutomationModule.cs:264]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L264)
- contains (declares function): [[function-15728-a095167b|lambda@src/Automator.Application/Automation/BrowserAutomationModule.cs:265]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L265)
- contains (declares function): [[function-15970-c7c81947|lambda@src/Automator.Application/Automation/BrowserAutomationModule.cs:269]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L269)
- contains (declares function): [[function-cs-2632ea08|Automator.Application.Automation.BrowserAutomationModule.ValidateAction(AutomationBrowserAction,BrowserAutomationProfile)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L287)
- contains (declares function): [[function-cs-f4f7d443|Automator.Application.Automation.BrowserAutomationModule.NormalizeAndValidate(BrowserAutomationProfile)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L318)
- contains (declares function): [[function-cs-b72e5733|Automator.Application.Automation.BrowserAutomationModule.Validate(BrowserAutomationProfile)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L331)
- contains (declares function): [[function-cs-373fdc50|Automator.Application.Automation.BrowserAutomationModule.NormalizeHost(string?)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L371)
- contains (declares function): [[function-cs-b96b13d0|Automator.Application.Automation.BrowserAutomationModule.TryReadWebUri(string?,Uri?)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L384)
- contains (declares function): [[function-cs-d89a49df|Automator.Application.Automation.BrowserAutomationModule.IsAllowedHost(string?,IReadOnlyList<string>)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L393)
- contains (declares function): [[function-23011-3a185bcc|lambda@src/Automator.Application/Automation/BrowserAutomationModule.cs:399]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L399)
- contains (declares function): [[function-cs-fde5f1cc|Automator.Application.Automation.BrowserAutomationModule.SanitizeUrl(string?,int)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L404)
- contains (declares function): [[function-cs-112b477a|Automator.Application.Automation.BrowserAutomationModule.ReadProfileId(JsonElement)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L415)
- contains (declares function): [[function-cs-3c64aa17|Automator.Application.Automation.BrowserAutomationModule.TryReadProfile(AutomationLibraryRecord)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L426)
- contains (declares function): [[function-cs-33c5dd6b|Automator.Application.Automation.BrowserAutomationModule.ActionLabel(AutomationBrowserActionKind)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L444)
- contains (declares function): [[function-cs-f3343d01|Automator.Application.Automation.BrowserAutomationModule.TruncateUtf8(string,int)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L457)
- contains (declares function): [[function-cs-a29622d1|Automator.Application.Automation.BrowserAutomationModule.Error(string)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L471)
- contains (declares function): [[function-cs-1461a5ac|Automator.Application.Automation.BrowserAutomationModule.Result(AutomationStatus,string,object,IReadOnlyList<AutomationAction>?)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L473)
- contains (declares function): [[function-cs-431137ca|Automator.Application.Automation.BrowserAutomationModule.CreateJsonOptions()]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L481)
- contains (declares function): [[function-cs-f49349d2|Automator.Application.Automation.BrowserSavedProfileHandler.ListProfilesAsync(CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L503)
- contains (declares function): [[function-28222-77686ae8|lambda@src/Automator.Application/Automation/BrowserAutomationModule.cs:506]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L506)
- contains (declares function): [[function-28275-3cecaa91|lambda@src/Automator.Application/Automation/BrowserAutomationModule.cs:507]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L507)
- contains (declares function): [[function-cs-3bc99e91|Automator.Application.Automation.BrowserSavedProfileHandler.ExecuteAsync(string,JsonElement?,AutomationExecutionMetadata,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L510)
- contains (declares function): [[function-cs-bfbb80c3|Automator.Application.Automation.BrowserSavedProfileHandler.DeserializeAction(JsonElement,BrowserAutomationProfile)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L544)
- contains (declares function): [[function-cs-cbc5525c|Automator.Application.Automation.BrowserSavedProfileHandler.BoundedOutput(AutomationBrowserActionResult,BrowserAutomationProfile)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L579)
- contains (declares function): [[function-33396-2e330646|lambda@src/Automator.Application/Automation/BrowserAutomationModule.cs:586]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L586)
- contains (declares function): [[function-33483-cb27a87a|lambda@src/Automator.Application/Automation/BrowserAutomationModule.cs:587]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L587)
- contains (declares function): [[function-33599-56c83577|lambda@src/Automator.Application/Automation/BrowserAutomationModule.cs:588]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L588)
- contains (declares function): [[function-cs-3003b2a4|Automator.Application.Automation.BrowserSavedProfileHandler.IsAllowedHost(string?,IReadOnlyList<string>)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L605)
- contains (declares function): [[function-34943-f67556e8|lambda@src/Automator.Application/Automation/BrowserAutomationModule.cs:613]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L613)
- contains (declares function): [[function-cs-31d69587|Automator.Application.Automation.BrowserSavedProfileHandler.SafeUrl(string?,int)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L618)
- contains (declares function): [[function-cs-209d2834|Automator.Application.Automation.BrowserSavedProfileHandler.NormalizeHost(string?)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L627)
- contains (declares function): [[function-cs-a029a815|Automator.Application.Automation.BrowserSavedProfileHandler.Truncate(string,int)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L636)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:927)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:97)

## Source anchors

- `src/Automator.Application/Automation/BrowserAutomationModule.cs`
