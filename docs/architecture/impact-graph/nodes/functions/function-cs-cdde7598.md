---
graph_id: "function-cs-cdde7598"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/contracts"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Protocol.RpcProtocol.ParseParameters(RpcRequest)

public static T ParseParameters(RpcRequest request)

## Connections

- impact (called by): [[backend-execute-module|Automator.Backend.BackendServer.ExecuteModuleActionAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:418)
- impact (called by): [[function-cs-28033dd0|Automator.Backend.BackendServer.GetGlobalVariablesAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:491)
- impact (called by): [[function-cs-4a974b84|Automator.Backend.BackendServer.UnsubscribeAutomationKeyboardAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:380)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:195)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:207)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:214)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:217)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:220)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:223)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:226)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:230)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:234)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:240)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:246)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:249)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:255)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:262)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:284)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:292)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:308)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:316)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:328)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:336)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:340)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:355)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:359)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:376)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:382)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:394)
- impact (called by): [[function-cs-86c12158|Automator.Backend.BackendServer.CancelAutomationHttpRequest(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:410)
- impact (called by): [[function-cs-993f9d46|Automator.Backend.BackendServer.SendAutomationHttpRequestAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:389)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:188)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:207)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:211)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:222)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:226)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:230)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:237)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:244)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:257)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:267)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:269)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:272)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:281)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:283)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:285)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:290)
- impact (called by): [[function-cs-ae048117|Automator.Backend.BackendServer.CancelAutomationModuleAction(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:556)
- impact (called by): [[function-cs-b20d646a|Automator.Backend.BackendServer.GetRunActivityAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:512)
- impact (called by): [[function-cs-db3278a9|Automator.Backend.BackendServer.GetModuleSettingsCore(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:615)
- impact (called by): [[function-cs-e1a03f42|Automator.Backend.BackendServer.SetGlobalVariablesAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:497)
- impact (called by): [[function-cs-e1f35018|Automator.Backend.BackendServer.GetAutomationKeyboardEligibility(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:349)
- impact (called by): [[function-cs-f3c0dc09|Automator.Backend.BackendServer.UpdateModuleSettingsCoreAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:577)
- impact (called by): [[function-cs-f462a920|Automator.Backend.BackendServer.SubscribeAutomationKeyboardAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:336)

## Source anchors

- `src/Automator.Protocol/RpcProtocol.cs#L97`
