---
graph_id: "file-browser-automation-proxy-f27cfe26"
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

# BrowserAutomationProxy.cs

Source file src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs.

## Connections

- contains (declares function): [[function-cs-b32f5cd2|Automator.Infrastructure.Automation.BrowserAutomationProxy.BrowserAutomationProxy(IHostAddressResolver,IReadOnlyList<string>,bool,string,string)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L29)
- contains (declares function): [[function-cs-52f8717a|Automator.Infrastructure.Automation.BrowserAutomationProxy.StartAsync(CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L46)
- contains (declares function): [[function-cs-c9fb5ab5|Automator.Infrastructure.Automation.BrowserAutomationProxy.UpdatePolicy(IReadOnlyList<string>,bool)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L63)
- contains (declares function): [[function-cs-40f250dc|Automator.Infrastructure.Automation.BrowserAutomationProxy.DisposeAsync()]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L77)
- contains (declares function): [[function-cs-9ce39ce9|Automator.Infrastructure.Automation.BrowserAutomationProxy.AcceptLoopAsync(TcpListener,CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L102)
- contains (declares function): [[function-5832-38296387|lambda@src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs:124]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L124)
- contains (declares function): [[function-cs-08ba8630|Automator.Infrastructure.Automation.BrowserAutomationProxy.HandleClientAsync(TcpClient,NetworkPolicy,CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L134)
- contains (declares function): [[function-cs-31abd2e5|Automator.Infrastructure.Automation.BrowserAutomationProxy.HandleConnectAsync(TcpClient,NetworkStream,ProxyRequest,NetworkPolicy,CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L167)
- contains (declares function): [[function-cs-c30644e4|Automator.Infrastructure.Automation.BrowserAutomationProxy.HandleHttpAsync(TcpClient,NetworkStream,ProxyRequest,NetworkPolicy,CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L189)
- contains (declares function): [[function-10518-e407a706|lambda@src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs:211]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L211)
- contains (declares function): [[function-cs-9a045ef7|Automator.Infrastructure.Automation.BrowserAutomationProxy.ResolveAndValidateAsync(string,NetworkPolicy,CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L227)
- contains (declares function): [[function-cs-a7c582e1|Automator.Infrastructure.Automation.BrowserAutomationProxy.IsAllowedHost(string,NetworkPolicy)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L244)
- contains (declares function): [[function-cs-2b378ed9|Automator.Infrastructure.Automation.BrowserAutomationProxy.HasValidCredential(IReadOnlyDictionary<string, string>)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L250)
- contains (declares function): [[function-cs-01052b9d|Automator.Infrastructure.Automation.BrowserAutomationProxy.ReadHeaderAsync(NetworkStream,CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L262)
- contains (declares function): [[function-cs-5123e6bf|Automator.Infrastructure.Automation.BrowserAutomationProxy.TryParseRequest(byte[],ProxyRequest)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L278)
- contains (declares function): [[function-14673-52802fa8|lambda@src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs:286]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L286)
- contains (declares function): [[function-14867-861a5c6f|lambda@src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs:291]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L291)
- contains (declares function): [[function-cs-8871a4c6|Automator.Infrastructure.Automation.BrowserAutomationProxy.RequestHostMatches(IReadOnlyDictionary<string, string>,Uri)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L298)
- contains (declares function): [[function-cs-025392ad|Automator.Infrastructure.Automation.BrowserAutomationProxy.TryParseAuthority(string,string,int)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L305)
- contains (declares function): [[function-cs-c217b01c|Automator.Infrastructure.Automation.BrowserAutomationProxy.TunnelAsync(Stream,Stream,CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L317)
- contains (declares function): [[function-cs-36989a76|Automator.Infrastructure.Automation.BrowserAutomationProxy.CopyAsync(Stream,Stream,CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L328)
- contains (declares function): [[function-cs-b1110dd9|Automator.Infrastructure.Automation.BrowserAutomationProxy.WriteErrorAsync(Stream,int,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L339)
- contains (declares function): [[function-cs-eceaacdf|Automator.Infrastructure.Automation.BrowserAutomationProxy.NormalizeHost(string)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L346)
- contains (declares function): [[function-cs-818f8d28|Automator.Infrastructure.Automation.BrowserAutomationProxy.CreatePolicy(IReadOnlyList<string>,bool)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L355)
- contains (declares function): [[function-cs-d2144c35|Automator.Infrastructure.Automation.BrowserAutomationProxy.IsAlwaysDisallowedAddress(IPAddress)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L364)
- contains (declares function): [[function-cs-a24c3b77|Automator.Infrastructure.Automation.BrowserAutomationProxy.IsNonPublicAddress(IPAddress)]] (src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs#L371)
- impact (type-reference): [[file-playwright-automation-browser-service-e8fdacd1|PlaywrightAutomationBrowserService.cs]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:261)
- impact (type-reference): [[file-playwright-automation-browser-service-e8fdacd1|PlaywrightAutomationBrowserService.cs]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:262)
- impact (type-reference): [[file-playwright-automation-browser-service-e8fdacd1|PlaywrightAutomationBrowserService.cs]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:270)
- impact (type-reference): [[file-playwright-automation-browser-service-e8fdacd1|PlaywrightAutomationBrowserService.cs]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:271)
- impact (type-reference): [[file-playwright-automation-browser-service-e8fdacd1|PlaywrightAutomationBrowserService.cs]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:272)
- impact (type-reference): [[file-playwright-automation-browser-service-e8fdacd1|PlaywrightAutomationBrowserService.cs]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:286)
- impact (type-reference): [[file-playwright-automation-browser-service-e8fdacd1|PlaywrightAutomationBrowserService.cs]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:287)
- impact (type-reference): [[file-playwright-automation-browser-service-e8fdacd1|PlaywrightAutomationBrowserService.cs]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:297)
- impact (type-reference): [[file-playwright-automation-browser-service-e8fdacd1|PlaywrightAutomationBrowserService.cs]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:315)
- impact (type-reference): [[file-playwright-automation-browser-service-e8fdacd1|PlaywrightAutomationBrowserService.cs]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:325)
- impact (type-reference): [[file-playwright-automation-browser-service-e8fdacd1|PlaywrightAutomationBrowserService.cs]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:327)
- impact (type-reference): [[file-playwright-automation-browser-service-e8fdacd1|PlaywrightAutomationBrowserService.cs]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:333)
- impact (type-reference): [[file-playwright-automation-browser-service-e8fdacd1|PlaywrightAutomationBrowserService.cs]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:432)
- impact (type-reference): [[file-playwright-automation-browser-service-e8fdacd1|PlaywrightAutomationBrowserService.cs]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:438)
- impact (type-reference): [[file-playwright-automation-browser-service-e8fdacd1|PlaywrightAutomationBrowserService.cs]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:464)

## Source anchors

- `src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs`
