---
graph_id: "file-script-runner-module-9e82267a"
graph_layer: "file"
graph_kind: "csharp"
tags:
  - "impact-graph"
  - "impact-graph/file"
  - "impact-graph/kind/csharp"
  - "impact-graph/area/script"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# ScriptRunnerModule.cs

Source file src/Automator.Application/Automation/ScriptRunnerModule.cs.

## Connections

- contains (declares function): [[function-cs-f320da91|Automator.Application.Automation.ScriptRunnerModule.ScriptRunnerModule(IAutomationVariableProvider?,ScriptRunnerTemplateInstaller?)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L43)
- contains (declares function): [[function-cs-c95436fa|Automator.Application.Automation.ScriptRunnerModule.CreateInitialState()]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L80)
- contains (declares function): [[function-cs-6bca79e1|Automator.Application.Automation.ScriptRunnerModule.CreateDefaultSettings()]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L83)
- contains (declares function): [[function-cs-c8095014|Automator.Application.Automation.ScriptRunnerModule.MigrateSettings(int,JsonElement)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L88)
- contains (declares function): [[function-cs-f69b1132|Automator.Application.Automation.ScriptRunnerModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L91)
- contains (declares function): [[function-cs-bde25d08|Automator.Application.Automation.ScriptRunnerModule.ListProfilesAsync(AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L119)
- contains (declares function): [[function-5571-c9bac41b|lambda@src/Automator.Application/Automation/ScriptRunnerModule.cs:123]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L123)
- contains (declares function): [[function-cs-5136517a|Automator.Application.Automation.ScriptRunnerModule.ListTemplates()]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L126)
- contains (declares function): [[function-cs-c8b971aa|Automator.Application.Automation.ScriptRunnerModule.InstallTemplateAsync(JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L130)
- contains (declares function): [[function-cs-f7252106|Automator.Application.Automation.ScriptRunnerModule.SaveProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L138)
- contains (declares function): [[function-cs-0efb206d|Automator.Application.Automation.ScriptRunnerModule.DeleteProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L152)
- contains (declares function): [[function-cs-594088fd|Automator.Application.Automation.ScriptRunnerModule.RunProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L161)
- contains (declares function): [[function-9845-ba99f72e|lambda@src/Automator.Application/Automation/ScriptRunnerModule.cs:190]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L190)
- contains (declares function): [[function-cs-f673d3ca|Automator.Application.Automation.ScriptRunnerModule.RedactSensitive(string,ScriptRunnerTemplateDescriptor,IReadOnlyList<string>)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L247)
- contains (declares function): [[function-13236-bf3f62f8|lambda@src/Automator.Application/Automation/ScriptRunnerModule.cs:249]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L249)
- contains (declares function): [[function-cs-755ef4be|Automator.Application.Automation.ScriptRunnerModule.ReadId(JsonElement)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L257)
- contains (declares function): [[function-cs-be07d3ca|Automator.Application.Automation.ScriptRunnerModule.Validate(ScriptRunnerProfile)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L267)
- contains (declares function): [[function-16053-c5d3e6f1|lambda@src/Automator.Application/Automation/ScriptRunnerModule.cs:291]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L291)
- contains (declares function): [[function-cs-ded489cb|Automator.Application.Automation.ScriptRunnerModule.Error(string)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L300)
- contains (declares function): [[function-cs-9a43ce40|Automator.Application.Automation.ScriptRunnerModule.Result(AutomationStatus,string,object,IReadOnlyList<AutomationAction>?)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L302)
- contains (declares function): [[function-cs-893dd8b2|Automator.Application.Automation.ScriptRunnerModule.CreateJsonOptions()]] (src/Automator.Application/Automation/ScriptRunnerModule.cs#L310)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:94)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:13)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:17)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:20)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:21)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:33)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:35)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:36)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:38)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:39)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:44)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:45)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:46)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:47)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:48)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:50)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:52)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:53)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:58)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:69)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:74)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:89)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:91)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:93)
- impact (type-reference): [[file-script-runner-saved-profile-handler-19d8f324|ScriptRunnerSavedProfileHandler.cs]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:94)
- impact (type-reference): [[file-script-runner-template-catalog-bf37d81d|ScriptRunnerTemplateCatalog.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateCatalog.cs:26)
- impact (type-reference): [[file-script-runner-template-catalog-bf37d81d|ScriptRunnerTemplateCatalog.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateCatalog.cs:28)
- impact (type-reference): [[file-script-runner-template-catalog-bf37d81d|ScriptRunnerTemplateCatalog.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateCatalog.cs:42)
- impact (type-reference): [[file-script-runner-template-catalog-bf37d81d|ScriptRunnerTemplateCatalog.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateCatalog.cs:66)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:22)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:29)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:32)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:33)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:35)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:36)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:37)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:38)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:61)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:62)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:64)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:65)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:66)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:67)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:80)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:82)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:83)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:84)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:85)
- impact (type-reference): [[file-script-runner-template-installer-b6b3796d|ScriptRunnerTemplateInstaller.cs]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:92)

## Source anchors

- `src/Automator.Application/Automation/ScriptRunnerModule.cs`
