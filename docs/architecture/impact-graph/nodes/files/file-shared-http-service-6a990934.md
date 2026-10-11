---
graph_id: "file-shared-http-service-6a990934"
graph_layer: "file"
graph_kind: "csharp"
tags:
  - "impact-graph"
  - "impact-graph/file"
  - "impact-graph/kind/csharp"
  - "impact-graph/area/http"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# SharedHttpService.cs

Source file src/Automator.Infrastructure/Automation/SharedHttpService.cs.

## Connections

- contains (declares function): [[function-cs-89d4d01f|Automator.Infrastructure.Automation.SharedHttpService.SharedHttpService(HttpClient,HttpClient,IHostAddressResolver,IApplicationLog,TimeSpan?)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L38)
- contains (declares function): [[function-cs-a16c2d4f|Automator.Infrastructure.Automation.SharedHttpService.CreateProductionClient(IHostAddressResolver,bool)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L50)
- contains (declares function): [[function-3105-d3cbe54c|lambda@src/Automator.Infrastructure/Automation/SharedHttpService.cs:60]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L60)
- contains (declares function): [[function-cs-658d828b|Automator.Infrastructure.Automation.SharedHttpService.ForModule(string,AutomationHttpPolicy,TimeSpan?)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L99)
- contains (declares function): [[function-cs-3a6319d0|Automator.Infrastructure.Automation.SharedHttpService.SendAsync(string,AutomationHttpPolicy,AutomationHttpRequest,TimeSpan?,CancellationToken)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L108)
- contains (declares function): [[function-7794-b47ffb75|lambda@src/Automator.Infrastructure/Automation/SharedHttpService.cs:141]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L141)
- contains (declares function): [[function-7812-463d7701|lambda@src/Automator.Infrastructure/Automation/SharedHttpService.cs:141]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L141)
- contains (declares function): [[function-cs-55d02b72|Automator.Infrastructure.Automation.SharedHttpService.Dispose()]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L229)
- contains (declares function): [[function-cs-328cfb35|Automator.Infrastructure.Automation.SharedHttpService.ValidateNetworkAddressAsync(Uri,AutomationHttpPolicy,CancellationToken)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L235)
- contains (declares function): [[function-cs-1d2ec323|Automator.Infrastructure.Automation.SharedHttpService.ResolveAddressesAsync(IHostAddressResolver,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L246)
- contains (declares function): [[function-cs-ebe57c69|Automator.Infrastructure.Automation.SharedHttpService.ValidateAddresses(IReadOnlyList<IPAddress>,bool)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L263)
- contains (declares function): [[function-cs-251aa7ab|Automator.Infrastructure.Automation.SharedHttpService.CreateRequest(Uri,HttpMethod,IReadOnlyDictionary<string, string>,byte[]?,AutomationHttpPolicy)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L271)
- contains (declares function): [[function-cs-5bbe6bfb|Automator.Infrastructure.Automation.SharedHttpService.ReadTextBodyAsync(HttpContent,MediaTypeHeaderValue?,CancellationToken)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L298)
- contains (declares function): [[function-cs-bcf298e7|Automator.Infrastructure.Automation.SharedHttpService.DecodeText(byte[],MediaTypeHeaderValue?)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L322)
- contains (declares function): [[function-cs-76563fc9|Automator.Infrastructure.Automation.SharedHttpService.IsTextualMediaType(string?)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L337)
- contains (declares function): [[function-cs-ebc61539|Automator.Infrastructure.Automation.SharedHttpService.GetExposedHeaders(HttpResponseMessage)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L346)
- contains (declares function): [[function-cs-54c4f562|Automator.Infrastructure.Automation.SharedHttpService.ParseAndValidateUri(string)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L357)
- contains (declares function): [[function-cs-cf4eb6ec|Automator.Infrastructure.Automation.SharedHttpService.ParseAndValidateUri(Uri)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L364)
- contains (declares function): [[function-cs-7dda8c42|Automator.Infrastructure.Automation.SharedHttpService.ValidateAllowedHost(Uri,HashSet<string>)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L376)
- contains (declares function): [[function-cs-be855801|Automator.Infrastructure.Automation.SharedHttpService.NormalizeAllowedHosts(AutomationHttpPolicy)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L383)
- contains (declares function): [[function-cs-37615ba6|Automator.Infrastructure.Automation.SharedHttpService.NormalizeHost(string)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L398)
- contains (declares function): [[function-cs-cd601a74|Automator.Infrastructure.Automation.SharedHttpService.IsRedirect(HttpStatusCode)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L405)
- contains (declares function): [[function-cs-eba865db|Automator.Infrastructure.Automation.SharedHttpService.SameOrigin(Uri,Uri)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L409)
- contains (declares function): [[function-cs-5b8d6670|Automator.Infrastructure.Automation.SharedHttpService.IsAlwaysDisallowedAddress(IPAddress)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L414)
- contains (declares function): [[function-cs-800ec1af|Automator.Infrastructure.Automation.SharedHttpService.IsNonPublicAddress(IPAddress)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L421)
- contains (declares function): [[function-cs-befb57cc|Automator.Infrastructure.Automation.SharedHttpService.FindServiceException(HttpRequestException)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L448)
- contains (declares function): [[function-cs-8955725a|Automator.Infrastructure.Automation.SharedHttpService.WriteLog(string,string?,string,long,int?,AutomationServiceErrorCategory?)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L455)
- contains (declares function): [[function-cs-6745b097|Automator.Infrastructure.Automation.SharedHttpService.SafeMethodName(string)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L473)
- contains (declares function): [[function-25566-41fda3da|lambda@src/Automator.Infrastructure/Automation/SharedHttpService.cs:476]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L476)
- contains (declares function): [[function-cs-587b957c|Automator.Infrastructure.Automation.SharedHttpService.Error(AutomationServiceErrorCategory,string)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L481)
- contains (declares function): [[function-cs-aa3c94d2|Automator.Infrastructure.Automation.SharedHttpService.ScopedHttpClient.SendAsync(AutomationHttpRequest,CancellationToken)]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs#L489)
- impact (type-reference): [[file-automation-api-profile-runner-5fd1dcea|AutomationApiProfileRunner.cs]] (src/Automator.Infrastructure/Automation/AutomationApiProfileRunner.cs:14)
- impact (type-reference): [[file-automation-api-profile-runner-5fd1dcea|AutomationApiProfileRunner.cs]] (src/Automator.Infrastructure/Automation/AutomationApiProfileRunner.cs:72)
- impact (type-reference): [[file-program-cae4b7ba|Program.cs]] (tests/Automator.Variables.Specs/Program.cs:22)
- impact (type-reference): [[file-program-cae4b7ba|Program.cs]] (tests/Automator.Variables.Specs/Program.cs:26)

## Source anchors

- `src/Automator.Infrastructure/Automation/SharedHttpService.cs`
