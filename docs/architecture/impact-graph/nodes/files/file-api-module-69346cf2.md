---
graph_id: "file-api-module-69346cf2"
graph_layer: "file"
graph_kind: "csharp"
tags:
  - "impact-graph"
  - "impact-graph/file"
  - "impact-graph/kind/csharp"
  - "impact-graph/area/api"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# ApiModule.cs

Source file src/Automator.Application/Automation/ApiModule.cs.

## Connections

- contains (declares function): [[function-cs-25f6bd42|Automator.Application.Automation.ApiModule.CreateInitialState()]] (src/Automator.Application/Automation/ApiModule.cs#L76)
- contains (declares function): [[function-cs-a46c790e|Automator.Application.Automation.ApiModule.CreateDefaultSettings()]] (src/Automator.Application/Automation/ApiModule.cs#L79)
- contains (declares function): [[function-cs-51bce744|Automator.Application.Automation.ApiModule.MigrateSettings(int,JsonElement)]] (src/Automator.Application/Automation/ApiModule.cs#L81)
- contains (declares function): [[function-cs-c3ae6cd8|Automator.Application.Automation.ApiModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs#L84)
- contains (declares function): [[function-cs-1f6e0b6f|Automator.Application.Automation.ApiModule.ListProfilesAsync(AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs#L118)
- contains (declares function): [[function-6156-029875bb|lambda@src/Automator.Application/Automation/ApiModule.cs:121]] (src/Automator.Application/Automation/ApiModule.cs#L121)
- contains (declares function): [[function-cs-93e0cb83|Automator.Application.Automation.ApiModule.SaveProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs#L135)
- contains (declares function): [[function-8214-c5631da8|lambda@src/Automator.Application/Automation/ApiModule.cs:157]] (src/Automator.Application/Automation/ApiModule.cs#L157)
- contains (declares function): [[function-cs-dd7d1a9e|Automator.Application.Automation.ApiModule.DeleteProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs#L164)
- contains (declares function): [[function-cs-0c472db4|Automator.Application.Automation.ApiModule.SetSecretAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs#L182)
- contains (declares function): [[function-cs-6571ede2|Automator.Application.Automation.ApiModule.ClearSecretAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs#L205)
- contains (declares function): [[function-11997-af48244a|lambda@src/Automator.Application/Automation/ApiModule.cs:215]] (src/Automator.Application/Automation/ApiModule.cs#L215)
- contains (declares function): [[function-12106-ce3088ac|lambda@src/Automator.Application/Automation/ApiModule.cs:216]] (src/Automator.Application/Automation/ApiModule.cs#L216)
- contains (declares function): [[function-12124-aecedd11|lambda@src/Automator.Application/Automation/ApiModule.cs:216]] (src/Automator.Application/Automation/ApiModule.cs#L216)
- contains (declares function): [[function-cs-977a1c24|Automator.Application.Automation.ApiModule.RunProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs#L233)
- contains (declares function): [[function-cs-2b34182c|Automator.Application.Automation.ApiModule.RequireProfileAsync(string,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs#L252)
- contains (declares function): [[function-cs-96497c80|Automator.Application.Automation.ApiModule.ExecuteProfileAsync(string,ApiResponseMode,JsonElement?,IAutomationApiProfileRunner,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs#L260)
- contains (declares function): [[function-15537-5f83403c|lambda@src/Automator.Application/Automation/ApiModule.cs:272]] (src/Automator.Application/Automation/ApiModule.cs#L272)
- contains (declares function): [[function-15596-9f85a329|lambda@src/Automator.Application/Automation/ApiModule.cs:273]] (src/Automator.Application/Automation/ApiModule.cs#L273)
- contains (declares function): [[function-15614-ccfa9d11|lambda@src/Automator.Application/Automation/ApiModule.cs:273]] (src/Automator.Application/Automation/ApiModule.cs#L273)
- contains (declares function): [[function-cs-1366ad1c|Automator.Application.Automation.ApiModule.CreateBoundedOutput(AutomationApiProfileResponse,IReadOnlyDictionary<string, string>,string,ApiResponseMode,DecodedBody,bool,bool,DecodedBody)]] (src/Automator.Application/Automation/ApiModule.cs#L292)
- contains (declares function): [[function-cs-5b429894|Automator.Application.Automation.ApiModule.DecodeResponse(ApiResponseMode,string,bool)]] (src/Automator.Application/Automation/ApiModule.cs#L348)
- contains (declares function): [[function-cs-52c61dc6|Automator.Application.Automation.ApiModule.DeleteSecretsAsync(IAutomationSecretManager,string,IEnumerable<string>,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs#L363)
- contains (declares function): [[function-cs-7983b8cb|Automator.Application.Automation.ApiModule.NormalizeAndValidate(ApiProfile)]] (src/Automator.Application/Automation/ApiModule.cs#L379)
- contains (declares function): [[function-20664-60a39c06|lambda@src/Automator.Application/Automation/ApiModule.cs:389]] (src/Automator.Application/Automation/ApiModule.cs#L389)
- contains (declares function): [[function-20682-0279832b|lambda@src/Automator.Application/Automation/ApiModule.cs:389]] (src/Automator.Application/Automation/ApiModule.cs#L389)
- contains (declares function): [[function-20801-1d6412d1|lambda@src/Automator.Application/Automation/ApiModule.cs:390]] (src/Automator.Application/Automation/ApiModule.cs#L390)
- contains (declares function): [[function-20819-bfa61a33|lambda@src/Automator.Application/Automation/ApiModule.cs:390]] (src/Automator.Application/Automation/ApiModule.cs#L390)
- contains (declares function): [[function-cs-465243f9|Automator.Application.Automation.ApiModule.Validate(ApiProfile)]] (src/Automator.Application/Automation/ApiModule.cs#L394)
- contains (declares function): [[function-cs-d61422f6|Automator.Application.Automation.ApiModule.TryReadProfile(AutomationLibraryRecord)]] (src/Automator.Application/Automation/ApiModule.cs#L447)
- contains (declares function): [[function-cs-753e3964|Automator.Application.Automation.ApiModule.TryGetSecretId(ApiProfile,string,string)]] (src/Automator.Application/Automation/ApiModule.cs#L458)
- contains (declares function): [[function-25328-6098272e|lambda@src/Automator.Application/Automation/ApiModule.cs:460]] (src/Automator.Application/Automation/ApiModule.cs#L460)
- contains (declares function): [[function-cs-0dba8912|Automator.Application.Automation.ApiModule.ReadProfileId(JsonElement)]] (src/Automator.Application/Automation/ApiModule.cs#L465)
- contains (declares function): [[function-cs-3cd05cae|Automator.Application.Automation.ApiModule.ReadSecretInput(JsonElement)]] (src/Automator.Application/Automation/ApiModule.cs#L472)
- contains (declares function): [[function-cs-983482fc|Automator.Application.Automation.ApiModule.ReadString(JsonElement,string,int)]] (src/Automator.Application/Automation/ApiModule.cs#L481)
- contains (declares function): [[function-cs-f9666bcb|Automator.Application.Automation.ApiModule.ReadOptionalInput(JsonElement)]] (src/Automator.Application/Automation/ApiModule.cs#L492)
- contains (declares function): [[function-cs-312653b0|Automator.Application.Automation.ApiModule.TruncateUtf8(string,int)]] (src/Automator.Application/Automation/ApiModule.cs#L500)
- contains (declares function): [[function-cs-83c88241|Automator.Application.Automation.ApiModule.StatusMessage(string,int,bool,bool)]] (src/Automator.Application/Automation/ApiModule.cs#L514)
- contains (declares function): [[function-cs-aa0dc1e5|Automator.Application.Automation.ApiModule.Error(string)]] (src/Automator.Application/Automation/ApiModule.cs#L522)
- contains (declares function): [[function-cs-b6ad4ce6|Automator.Application.Automation.ApiModule.Result(AutomationStatus,string,object,IReadOnlyList<AutomationAction>?)]] (src/Automator.Application/Automation/ApiModule.cs#L524)
- contains (declares function): [[function-cs-79c36eb7|Automator.Application.Automation.ApiModule.CreateJsonOptions()]] (src/Automator.Application/Automation/ApiModule.cs#L540)
- contains (declares function): [[function-cs-c222d00b|Automator.Application.Automation.ApiSavedProfileHandler.ListProfilesAsync(CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs#L557)
- contains (declares function): [[function-30229-17ecf6a4|lambda@src/Automator.Application/Automation/ApiModule.cs:562]] (src/Automator.Application/Automation/ApiModule.cs#L562)
- contains (declares function): [[function-30282-0cf0c611|lambda@src/Automator.Application/Automation/ApiModule.cs:563]] (src/Automator.Application/Automation/ApiModule.cs#L563)
- contains (declares function): [[function-cs-c406b5d1|Automator.Application.Automation.ApiSavedProfileHandler.ExecuteAsync(string,JsonElement?,AutomationExecutionMetadata,CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs#L567)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:926)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:96)

## Source anchors

- `src/Automator.Application/Automation/ApiModule.cs`
