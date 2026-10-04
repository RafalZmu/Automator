using System.Text.Json;
using System.Text;
using Automator.Core.Automation;
using Automator.Core.Plugins;
using Automator.Protocol;

using var fixture = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "rpc-contract-fixtures.json")));
var root = fixture.RootElement;
var checks = 0;

if (!RpcProtocol.MustDispatchOutsideCommandQueue(RpcMethods.ModuleAction)
    || !RpcProtocol.MustDispatchOutsideCommandQueue(RpcMethods.HttpRequest)
    || !RpcProtocol.MustDispatchOutsideCommandQueue(RpcMethods.HttpCancel)
    || RpcProtocol.MustDispatchOutsideCommandQueue(RpcMethods.SetQuery)
    || RpcProtocol.MustDispatchOutsideCommandQueue(RpcMethods.ModuleSettingsUpdate))
    throw new InvalidOperationException("Automation service and module action calls must bypass serialized launcher dispatch, while launcher state changes remain queued.");
checks++;
Console.WriteLine("PASS automation-actions-bypass-serialized-command-queue");

foreach (var item in root.GetProperty("validRequests").EnumerateArray())
{
    var request = RpcProtocol.ParseRequest(item.GetProperty("request").GetRawText());
    RpcParameterValidator.Validate(request);
    checks++;
    Console.WriteLine($"PASS {item.GetProperty("name").GetString()}");
}

foreach (var item in root.GetProperty("invalidRequests").EnumerateArray())
{
    try
    {
        var request = RpcProtocol.ParseRequest(item.GetProperty("request").GetRawText());
        RpcParameterValidator.Validate(request);
        throw new InvalidOperationException("Expected a protocol validation error.");
    }
    catch (RpcProtocolException exception)
    {
        var expectedId = item.GetProperty("expectedId");
        var response = RpcProtocol.Failure(null, exception);
        if (exception.Code != item.GetProperty("expectedCode").GetInt32()
            || response.Id is null
            || response.Id.Value.GetRawText() != expectedId.GetRawText())
        {
            throw new InvalidOperationException($"Unexpected error response for '{item.GetProperty("name").GetString()}'.");
        }

        checks++;
        Console.WriteLine($"PASS {item.GetProperty("name").GetString()}");
    }
}

var workspaceAction = RpcProtocol.ParseRequest("""{"jsonrpc":"2.0","id":1,"method":"automation/moduleAction","params":{"requestId":"ws-run","contractVersion":1,"moduleId":"api","actionId":"send","actionVersion":1,"input":{},"hostWindowContext":{"role":"workspace","selectedTab":3}}}""");
RpcParameterValidator.Validate(workspaceAction);
checks++;
Console.WriteLine("PASS host-owned-workspace-module-context");
try
{
    var invalidWorkspaceAction = RpcProtocol.ParseRequest("""{"jsonrpc":"2.0","id":1,"method":"automation/moduleAction","params":{"requestId":"ws-run","contractVersion":1,"moduleId":"api","actionId":"send","actionVersion":1,"input":{},"hostWindowContext":{"role":"spoofed","selectedTab":3}}}""");
    RpcParameterValidator.Validate(invalidWorkspaceAction);
    throw new InvalidOperationException("Expected host window context validation to fail.");
}
catch (RpcProtocolException exception) when (exception.Code == -32602)
{
    checks++;
    Console.WriteLine("PASS host-window-role-validation");
}

foreach (var item in root.GetProperty("authorizationCases").EnumerateArray())
{
    var request = RpcProtocol.ParseRequest(item.GetProperty("request").GetRawText());
    RpcParameterValidator.Validate(request);
    if (item.GetProperty("authorized").GetBoolean())
        throw new InvalidOperationException($"Fixture '{item.GetProperty("name").GetString()}' should require backend authorization.");
    checks++;
    Console.WriteLine($"PASS {item.GetProperty("name").GetString()}-well-formed-awaits-backend-authorization");
}

var serializedNotification = RpcProtocol.Serialize(new RpcNotification("2.0", "stateChanged", new
{
    protocolVersion = RpcProtocol.Version,
    buildId = "fixture-build",
    revision = 1,
    tabRegistryVersion = 3,
    moduleStates = Array.Empty<object>(),
    visible = false
}));
using (var notificationDocument = JsonDocument.Parse(serializedNotification))
{
    var notification = notificationDocument.RootElement;
    if (!notification.TryGetProperty("params", out var parameters)
        || parameters.GetProperty("buildId").GetString() != "fixture-build"
        || notification.TryGetProperty("parameters", out _))
        throw new InvalidOperationException("stateChanged notification must serialize its payload using the JSON-RPC params key.");
}
checks++;
Console.WriteLine("PASS stateChanged-uses-json-rpc-params");

var keyboardNotification = RpcProtocol.Serialize(new RpcNotification("2.0", "automation/keyboardInput",
    new AutomationKeyboardInputNotification("launcher", new KeyInputEvent(7, "KeyA", 0x41, true, false,
        AutomationKeyModifiers.Shift | AutomationKeyModifiers.Control))));
using (var keyboardDocument = JsonDocument.Parse(keyboardNotification))
{
    var inputEvent = keyboardDocument.RootElement.GetProperty("params").GetProperty("event");
    if (keyboardDocument.RootElement.GetProperty("method").GetString() != "automation/keyboardInput"
        || inputEvent.GetProperty("code").GetString() != "KeyA"
        || inputEvent.GetProperty("modifiers").GetInt32() != 3)
        throw new InvalidOperationException("Keyboard input notification must serialize its typed, versioned event payload.");
}
checks++;
Console.WriteLine("PASS keyboard-input-notification-uses-typed-payload-and-numeric-modifiers");

var keyboardAvailability = RpcProtocol.Serialize(new RpcNotification("2.0", RpcMethods.KeyboardAvailability,
    new AutomationKeyboardAvailabilityNotification("launcher", true, 9)));
using (var availabilityDocument = JsonDocument.Parse(keyboardAvailability))
{
    var parameters = availabilityDocument.RootElement.GetProperty("params");
    if (availabilityDocument.RootElement.GetProperty("method").GetString() != RpcMethods.KeyboardAvailability
        || parameters.GetProperty("moduleId").GetString() != "launcher"
        || !parameters.GetProperty("eligible").GetBoolean()
        || parameters.GetProperty("revision").GetInt64() != 9)
        throw new InvalidOperationException("Keyboard availability notifications must expose typed eligibility and revision fields.");
}
checks++;
Console.WriteLine("PASS keyboard-availability-notification-uses-revisioned-payload");

using (var resultData = JsonDocument.Parse("{\"count\":3}"))
using (var followUpPayload = JsonDocument.Parse("{\"reportId\":\"r3\"}"))
{
    var moduleResult = new AutomationResult(1, AutomationStatus.Success, "Done", resultData.RootElement.Clone(),
        [new AutomationAction("open-report", "Open report", 1, followUpPayload.RootElement.Clone())]);
    using var resultDocument = JsonDocument.Parse(RpcProtocol.Serialize(moduleResult));
    var result = resultDocument.RootElement;
    if (result.GetProperty("contractVersion").GetInt32() != 1
        || result.GetProperty("status").GetString() != "success"
        || result.GetProperty("data").GetProperty("count").GetInt32() != 3
        || result.GetProperty("actions")[0].GetProperty("payload").GetProperty("reportId").GetString() != "r3")
        throw new InvalidOperationException("Automation results must preserve versioned structured data and follow-up action payloads over RPC.");
}
checks++;
Console.WriteLine("PASS module-action-result-preserves-structured-json-over-rpc");

var wireLimits = root.GetProperty("wireLimits");
var requestBody = new string(wireLimits.GetProperty("controlCharacter").GetString()![0], wireLimits.GetProperty("requestBodyBytes").GetInt32());
var requestLine = RpcProtocol.Serialize(new
{
    jsonrpc = "2.0",
    id = "wire-request",
    method = RpcMethods.HttpRequest,
    @params = new
    {
        moduleId = "launcher",
        requestId = "wire-request",
        request = new AutomationHttpRequest("https://api.example.test/", "POST", new Dictionary<string, string>(), requestBody)
    }
});
if (Encoding.UTF8.GetByteCount(requestLine) > RpcProtocol.MaximumRequestBytes)
    throw new InvalidOperationException("Escaped 1 MiB request body exceeds the configured JSON-RPC request line cap.");
RpcParameterValidator.Validate(RpcProtocol.ParseRequest(requestLine));
checks++;
Console.WriteLine("PASS escaped-1-MiB-http-request-fits-rpc-line-cap");

var oversizedBodyLine = RpcProtocol.Serialize(new
{
    jsonrpc = "2.0",
    id = "oversized-request",
    method = RpcMethods.HttpRequest,
    @params = new
    {
        moduleId = "launcher",
        requestId = "oversized-request",
        request = new AutomationHttpRequest("https://api.example.test/", "POST", new Dictionary<string, string>(),
            new string('x', wireLimits.GetProperty("requestBodyBytes").GetInt32() + 1))
    }
});
try
{
    RpcParameterValidator.Validate(RpcProtocol.ParseRequest(oversizedBodyLine));
    throw new InvalidOperationException("Expected an HTTP request body size validation error.");
}
catch (RpcProtocolException exception) when (exception.Code == -32602) { }
checks++;
Console.WriteLine("PASS oversized-http-request-is-rejected-before-dispatch");

using var oversizedModuleInput = JsonDocument.Parse($"\"{new string('x', 64 * 1024)}\"");
var moduleActionRequest = RpcProtocol.ParseRequest(RpcProtocol.Serialize(new
{
    jsonrpc = "2.0",
    id = "oversized-module-input",
    method = RpcMethods.ModuleAction,
    @params = new AutomationModuleActionParams("fixture-request", 1, "fixture-module", "summarize", 1, oversizedModuleInput.RootElement.Clone())
}));
try
{
    RpcParameterValidator.Validate(moduleActionRequest);
    throw new InvalidOperationException("Expected an oversized module action input to be rejected.");
}
catch (RpcProtocolException exception) when (exception.Code == -32602) { }
checks++;
Console.WriteLine("PASS oversized-module-action-input-is-rejected-before-dispatch");

using var oversizedSettingsValue = JsonDocument.Parse($"\"{new string('x', 64 * 1024)}\"");
var moduleSettingsUpdateRequest = RpcProtocol.ParseRequest(RpcProtocol.Serialize(new
{
    jsonrpc = "2.0",
    id = "oversized-module-settings",
    method = RpcMethods.ModuleSettingsUpdate,
    @params = new ModuleSettingsUpdateParams(1, "fixture-module", 2, oversizedSettingsValue.RootElement.Clone())
}));
try
{
    RpcParameterValidator.Validate(moduleSettingsUpdateRequest);
    throw new InvalidOperationException("Expected an oversized module settings value to be rejected.");
}
catch (RpcProtocolException exception) when (exception.Code == -32602) { }
checks++;
Console.WriteLine("PASS oversized-module-settings-are-rejected-before-dispatch");

var responseBody = new string(wireLimits.GetProperty("controlCharacter").GetString()![0], wireLimits.GetProperty("responseBodyBytes").GetInt32());
var maximumResponse = RpcProtocol.Serialize(RpcProtocol.Success(RpcProtocol.ParseRequest("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"getState\",\"params\":{}}"),
    new AutomationHttpResult(200, new Dictionary<string, string>(), "text/plain", responseBody)));
if (Encoding.UTF8.GetByteCount(maximumResponse) > RpcProtocol.MaximumResponseBytes)
    throw new InvalidOperationException("Escaped 4 MiB HTTP response body exceeds the configured JSON-RPC response line cap.");
checks++;
Console.WriteLine("PASS escaped-4-MiB-http-response-fits-rpc-line-cap");

Console.WriteLine($"{checks}/{checks} checks passed");
