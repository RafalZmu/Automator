---
graph_id: "file-automation-keyboard-event-hub-6cbb478d"
graph_layer: "file"
graph_kind: "csharp"
tags:
  - "impact-graph"
  - "impact-graph/file"
  - "impact-graph/kind/csharp"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# AutomationKeyboardEventHub.cs

Source file src/Automator.Application/Automation/AutomationKeyboardEventHub.cs.

## Connections

- contains (declares function): [[function-cs-7ccec9b5|Automator.Application.Automation.AutomationKeyboardEventHub.AutomationKeyboardEventHub(IApplicationLog?)]] (src/Automator.Application/Automation/AutomationKeyboardEventHub.cs#L28)
- contains (declares function): [[function-cs-84e6b066|Automator.Application.Automation.AutomationKeyboardEventHub.SetDeliveryContext(KeyboardDeliveryState?)]] (src/Automator.Application/Automation/AutomationKeyboardEventHub.cs#L35)
- contains (declares function): [[function-cs-0d23090f|Automator.Application.Automation.AutomationKeyboardEventHub.Publish(KeyInputEvent)]] (src/Automator.Application/Automation/AutomationKeyboardEventHub.cs#L55)
- contains (declares function): [[function-2152-ed74263c|lambda@src/Automator.Application/Automation/AutomationKeyboardEventHub.cs:62]] (src/Automator.Application/Automation/AutomationKeyboardEventHub.cs#L62)
- contains (declares function): [[function-cs-32265fb5|Automator.Application.Automation.AutomationKeyboardEventHub.SubscribeAsync(string,Func<KeyInputEvent, ValueTask>,CancellationToken)]] (src/Automator.Application/Automation/AutomationKeyboardEventHub.cs#L82)
- contains (declares function): [[function-3923-57206157|lambda@src/Automator.Application/Automation/AutomationKeyboardEventHub.cs:96]] (src/Automator.Application/Automation/AutomationKeyboardEventHub.cs#L96)
- contains (declares function): [[function-cs-026f9fb1|Automator.Application.Automation.AutomationKeyboardEventHub.DispatchAsync(Subscriber)]] (src/Automator.Application/Automation/AutomationKeyboardEventHub.cs#L102)
- contains (declares function): [[function-cs-fce7a68a|Automator.Application.Automation.AutomationKeyboardEventHub.Detach(Subscriber)]] (src/Automator.Application/Automation/AutomationKeyboardEventHub.cs#L123)
- contains (declares function): [[function-cs-cfe82845|Automator.Application.Automation.AutomationKeyboardEventHub.StopSubscriber(Subscriber)]] (src/Automator.Application/Automation/AutomationKeyboardEventHub.cs#L129)
- contains (declares function): [[function-cs-cea8721e|Automator.Application.Automation.AutomationKeyboardEventHub.DisposeSubscriberAsync(Subscriber)]] (src/Automator.Application/Automation/AutomationKeyboardEventHub.cs#L144)
- contains (declares function): [[function-cs-0857a5f7|Automator.Application.Automation.AutomationKeyboardEventHub.Subscriber.TryBeginStop()]] (src/Automator.Application/Automation/AutomationKeyboardEventHub.cs#L171)
- contains (declares function): [[function-cs-11f5bbab|Automator.Application.Automation.AutomationKeyboardEventHub.Subscriber.CompleteStop()]] (src/Automator.Application/Automation/AutomationKeyboardEventHub.cs#L172)
- contains (declares function): [[function-cs-d1d3ed8e|Automator.Application.Automation.AutomationKeyboardEventHub.Subscriber.DisposeStop()]] (src/Automator.Application/Automation/AutomationKeyboardEventHub.cs#L173)
- contains (declares function): [[function-cs-5227445f|Automator.Application.Automation.AutomationKeyboardEventHub.Subscriber.Start(Func<Subscriber, Task>)]] (src/Automator.Application/Automation/AutomationKeyboardEventHub.cs#L177)
- contains (declares function): [[function-cs-9c065847|Automator.Application.Automation.AutomationKeyboardEventHub.Subscription.DisposeAsync()]] (src/Automator.Application/Automation/AutomationKeyboardEventHub.cs#L183)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:1739)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:1794)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:1797)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:367)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:370)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:372)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:43)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:648)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:654)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:747)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:755)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:765)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:766)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:770)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:778)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:784)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:789)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:790)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:791)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:859)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:870)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:896)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:938)

## Source anchors

- `src/Automator.Application/Automation/AutomationKeyboardEventHub.cs`
