using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using Automator.Core.Automation;
using Automator.Core.Launcher;

namespace Automator.Protocol;

public sealed record RpcRequest(JsonElement Id, string Method, JsonElement Parameters);
public sealed record RpcError(int Code, string Message, object? Data = null);
public sealed record RpcResponse(
    string Jsonrpc,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] JsonElement? Id,
    object? Result = null,
    RpcError? Error = null);
public sealed record RpcNotification(string Jsonrpc, string Method, [property: JsonPropertyName("params")] object Parameters);

public sealed class RpcProtocolException(int code, string message, object? errorData = null, Exception? innerException = null, JsonElement? requestId = null)
    : Exception(message, innerException)
{
    public int Code { get; } = code;
    public object? ErrorData { get; } = errorData;
    public JsonElement? RequestId { get; } = requestId;
}

/// <summary>Shared JSON-RPC 2.0 envelope validation used before backend command dispatch.</summary>
public static class RpcProtocol
{
    public const int Version = 3;
    // A control character may expand from one UTF-8 byte to the six ASCII bytes of "\\u0001".
    public const int MaximumRequestBytes = 7 * 1024 * 1024;
    public const int MaximumResponseBytes = 28 * 1024 * 1024;
    public const int MaximumHttpRequestBodyBytes = 1 * 1024 * 1024;
    public const int MaximumHttpResponseBodyBytes = 4 * 1024 * 1024;

    public static bool MustDispatchOutsideCommandQueue(string method)
        => method is RpcMethods.HttpRequest or RpcMethods.HttpCancel or RpcMethods.ModuleAction or RpcMethods.ModuleActionCancel;

    private static readonly HashSet<string> Methods = new(StringComparer.Ordinal)
    {
        "initialize", "getState", "launcher/open", "launcher/close", "launcher/setWindowContext", "launcher/verifyForeground", "launcher/acquireForeground", "launcher/restoreForeground", "launcher/setMode",
        "launcher/selectTab", "launcher/setQuery", "launcher/openCatalog", "catalog/select",
        "catalog/addCustom", "binding/saveAlias", "binding/select", "binding/remove", "settings/update",
        "settings/import", "settings/export", "settings/openLogFolder", "settings/relink",
        "hotkey/startRecording", "hotkey/cancelRecording", "launcher/activateBinding",
        "automation/keyboardEligibility", "automation/keyboardSubscribe", "automation/keyboardUnsubscribe", "automation/httpRequest", "automation/httpCancel", "automation/moduleAction", "automation/moduleActionCancel", "module/settingsGet", "module/settingsUpdate",
        "variables/get", "variables/set", "activity/list"
    };

    public static JsonSerializerOptions SerializerOptions { get; } = CreateOptions();

    public static RpcRequest ParseRequest(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        if (System.Text.Encoding.UTF8.GetByteCount(line) > MaximumRequestBytes)
            throw new RpcProtocolException(-32600, "Request exceeds the maximum message size.");

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(line, new JsonDocumentOptions { MaxDepth = 32 });
        }
        catch (JsonException exception)
        {
            throw new RpcProtocolException(-32700, "Request JSON is malformed.", null, exception);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("jsonrpc", out var version)
                || version.ValueKind != JsonValueKind.String
                || version.GetString() != "2.0"
                || !root.TryGetProperty("id", out var id)
                || id.ValueKind is not (JsonValueKind.String or JsonValueKind.Number)
                || !root.TryGetProperty("method", out var methodElement)
                || methodElement.ValueKind != JsonValueKind.String)
            {
                throw new RpcProtocolException(-32600, "Request must contain jsonrpc '2.0', an id, and a method.");
            }

            var method = methodElement.GetString()!;
            if (method.Length is 0 or > 120 || !Methods.Contains(method))
                throw new RpcProtocolException(-32601, "Unknown or unsupported method.", new { method }, requestId: id.Clone());

            var parameters = root.TryGetProperty("params", out var paramsElement)
                ? paramsElement.Clone()
                : CreateEmptyObject();
            if (parameters.ValueKind != JsonValueKind.Object)
                throw new RpcProtocolException(-32602, "Method parameters must be a JSON object.", new { method }, requestId: id.Clone());

            return new RpcRequest(id.Clone(), method, parameters);
        }
    }

    public static T ParseParameters<T>(RpcRequest request)
    {
        try
        {
            return request.Parameters.Deserialize<T>(SerializerOptions)
                ?? throw new RpcProtocolException(-32602, "Method parameters are missing.", new { request.Method }, requestId: request.Id);
        }
        catch (JsonException exception)
        {
            throw new RpcProtocolException(-32602, "Method parameters have an invalid shape.", new { request.Method }, exception, request.Id);
        }
    }

    public static string Serialize(object value)
    {
        var line = JsonSerializer.Serialize(value, SerializerOptions);
        if (Encoding.UTF8.GetByteCount(line) > MaximumResponseBytes)
            throw new InvalidOperationException("Response exceeds the maximum JSON-RPC message size.");
        return line;
    }

    public static RpcResponse Success(RpcRequest request, object result) => new("2.0", request.Id, result);

    public static RpcResponse Failure(JsonElement? id, RpcProtocolException exception) =>
        new("2.0", id ?? exception.RequestId, Error: new RpcError(exception.Code, exception.Message, exception.ErrorData));

    private static JsonElement CreateEmptyObject()
    {
        using var document = JsonDocument.Parse("{}");
        return document.RootElement.Clone();
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            MaxDepth = 32,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new AutomationKeyModifiersConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private sealed class AutomationKeyModifiersConverter : JsonConverter<AutomationKeyModifiers>
    {
        public override AutomationKeyModifiers Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var value) || value is < 0 or > 15)
                throw new JsonException("Keyboard modifier flags must be a 0-to-15 integer.");
            return (AutomationKeyModifiers)value;
        }

        public override void Write(Utf8JsonWriter writer, AutomationKeyModifiers value, JsonSerializerOptions options) =>
            writer.WriteNumberValue((int)value);
    }
}

public sealed record InitializeParams(int ProtocolVersion, string BuildId, int HostProcessId, string HostExecutablePath, string? PortableExecutablePath, bool TestMode, string? DataDirectory);
public sealed record WindowContextParams(string WindowHandleHex, bool Visible, bool RendererFocused, bool NativeDialogActive, string Mode);
public sealed record ForegroundCheckParams(string WindowHandleHex);
public sealed record SetModeParams(string Mode);
public sealed record SelectTabParams(int Tab);
public sealed record SetQueryParams(string Query);
public sealed record SelectBindingParams(string BindingId);
public sealed record CustomAppParams(string Path);
public sealed record SaveAliasParams(string BindingId, string Alias);
public sealed record RemoveBindingParams(string BindingId);
public sealed record SettingsUpdateParams(string Hotkey, string Theme, bool StartWithWindows, IReadOnlyList<BindingParams> Bindings);
public sealed record BindingParams(string Id, string Name, string TargetPath, string Alias, string Arguments);
public sealed record SettingsFileParams(string Path);
public sealed record RelinkParams(string BindingId, string Path);
public sealed record ActivateBindingParams(string BindingId);
public sealed record EmptyParams();
public sealed record HostWindowContextParams(string Role, int SelectedTab);
public sealed record AutomationModuleParams(string ModuleId, HostWindowContextParams? HostWindowContext = null);
public sealed record AutomationHttpRequestParams(string ModuleId, string RequestId, AutomationHttpRequest Request, HostWindowContextParams? HostWindowContext = null);
public sealed record AutomationHttpCancelParams(string ModuleId, string RequestId, HostWindowContextParams? HostWindowContext = null);
public sealed record AutomationModuleActionParams(string RequestId, int ContractVersion, string ModuleId, string ActionId, int ActionVersion, JsonElement Input, HostWindowContextParams? HostWindowContext = null);
public sealed record AutomationModuleActionCancelParams(string ModuleId, string RequestId, HostWindowContextParams? HostWindowContext = null);
public sealed record ModuleSettingsUpdateParams(int ContractVersion, string ModuleId, int SettingsVersion, JsonElement Value, HostWindowContextParams? HostWindowContext = null);
public sealed record ModuleSettingsGetParams(int ContractVersion, string ModuleId, int SettingsVersion, HostWindowContextParams? HostWindowContext = null);
public sealed record GlobalVariablesGetParams(int Version);
public sealed record GlobalVariablesSetParams(int Version, Dictionary<string, JsonElement> Values);
public sealed record AutomationKeyboardInputNotification(string ModuleId, KeyInputEvent Event);
public sealed record AutomationKeyboardEligibilityResult(bool Eligible, long Revision);
public sealed record AutomationKeyboardAvailabilityNotification(string ModuleId, bool Eligible, long Revision);

public static class RpcParameterValidator
{
    public static void Validate(RpcRequest request)
    {
        switch (request.Method)
        {
            case RpcMethods.Initialize:
            {
                var value = RpcProtocol.ParseParameters<InitializeParams>(request);
                Require(value.ProtocolVersion == RpcProtocol.Version, "Requested protocol version is not supported.", request);
                Require(value.BuildId is { Length: > 0 and <= 128 }, "Build id is required and must be at most 128 characters.", request);
                Require(value.HostProcessId > 0, "Host process id is invalid.", request);
                Require(IsAbsoluteFilePath(value.HostExecutablePath), "Host executable path must be absolute.", request);
                Require(string.IsNullOrWhiteSpace(value.PortableExecutablePath) || IsAbsoluteFilePath(value.PortableExecutablePath), "Portable executable path must be absolute.", request);
                Require(value.TestMode ? IsAbsoluteFilePath(value.DataDirectory) : string.IsNullOrWhiteSpace(value.DataDirectory),
                    "Test mode requires an isolated absolute data directory; production cannot override its data directory.", request);
                break;
            }
            case RpcMethods.SetWindowContext:
            {
                var value = RpcProtocol.ParseParameters<WindowContextParams>(request);
                Require(TryParseHwnd(value.WindowHandleHex, out _), "Window handle is invalid.", request);
                Require(value.Mode is "launcher" or "catalog" or "settings" or "recordingHotkey" or "aliasEditing", "Launcher mode is invalid.", request);
                break;
            }
            case RpcMethods.VerifyForeground:
            case RpcMethods.RestoreForeground:
                Require(TryParseHwnd(RpcProtocol.ParseParameters<ForegroundCheckParams>(request).WindowHandleHex, out _), "Window handle is invalid.", request);
                break;
            case RpcMethods.AcquireForeground:
                _ = RpcProtocol.ParseParameters<EmptyParams>(request);
                break;
            case RpcMethods.SetMode:
                Require(IsKnownMode(RpcProtocol.ParseParameters<SetModeParams>(request).Mode), "Launcher mode is invalid.", request);
                break;
            case RpcMethods.SelectTab:
                Require(RpcProtocol.ParseParameters<SelectTabParams>(request).Tab is >= 1 and <= 9, "Tab must be between 1 and 9.", request);
                break;
            case RpcMethods.SetQuery:
                Require(RpcProtocol.ParseParameters<SetQueryParams>(request).Query is { Length: <= 512 }, "Query must be present and at most 512 characters.", request);
                break;
            case RpcMethods.SelectCatalogApp:
            case RpcMethods.SelectBinding:
                Require(RpcProtocol.ParseParameters<SelectBindingParams>(request).BindingId is { Length: > 0 and <= 128 }, "Binding id is required.", request);
                break;
            case RpcMethods.AddCustomApp:
            {
                var path = RpcProtocol.ParseParameters<CustomAppParams>(request).Path;
                Require(IsAbsoluteFilePath(path) && (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)), "Custom target must be an absolute .exe or .lnk path.", request);
                break;
            }
            case RpcMethods.SaveAlias:
            {
                var value = RpcProtocol.ParseParameters<SaveAliasParams>(request);
                Require(value.BindingId is { Length: > 0 and <= 128 }, "Binding id is required.", request);
                Require(AliasValidator.Validate(value.Alias, []) is null, "Alias must contain letters only and cannot be empty.", request);
                break;
            }
            case RpcMethods.ActivateBinding:
                Require(RpcProtocol.ParseParameters<ActivateBindingParams>(request).BindingId is { Length: > 0 and <= 128 }, "Binding id is required.", request);
                break;
            case RpcMethods.RemoveBinding:
                Require(RpcProtocol.ParseParameters<RemoveBindingParams>(request).BindingId is { Length: > 0 and <= 128 }, "Binding id is required.", request);
                break;
            case RpcMethods.KeyboardEligibility:
            case RpcMethods.KeyboardSubscribe:
            case RpcMethods.KeyboardUnsubscribe:
            {
                var value = RpcProtocol.ParseParameters<AutomationModuleParams>(request);
                ValidateModuleId(value.ModuleId, request);
                ValidateHostWindowContext(value.HostWindowContext, request);
                break;
            }
            case RpcMethods.HttpRequest:
            {
                var value = RpcProtocol.ParseParameters<AutomationHttpRequestParams>(request);
                ValidateModuleId(value.ModuleId, request);
                ValidateHostWindowContext(value.HostWindowContext, request);
                Require(value.RequestId is { Length: > 0 and <= 128 }, "Request id is required and must be at most 128 characters.", request);
                var httpRequest = value.Request ?? throw new RpcProtocolException(-32602, "HTTP request is required.",
                    new { request.Method }, requestId: request.Id);
                Require(httpRequest.Uri is { Length: > 0 and <= 8192 }, "HTTP URI is required and must be at most 8192 characters.", request);
                Require(httpRequest.Method is { Length: > 0 and <= 32 }, "HTTP method is required and must be at most 32 characters.", request);
                Require(httpRequest.Headers is { Count: <= 64 }, "HTTP headers are required and may contain at most 64 entries.", request);
                var headerBytes = 0;
                foreach (var (name, headerValue) in httpRequest.Headers)
                {
                    Require(name is { Length: > 0 and <= 128 } && headerValue is { Length: <= 8192 }, "An HTTP header is invalid.", request);
                    headerBytes = checked(headerBytes + Encoding.UTF8.GetByteCount(name) + Encoding.UTF8.GetByteCount(headerValue));
                }
                Require(headerBytes <= 256 * 1024, "HTTP headers exceed the maximum size.", request);
                Require(httpRequest.Body is null || Encoding.UTF8.GetByteCount(httpRequest.Body) <= RpcProtocol.MaximumHttpRequestBodyBytes,
                    "HTTP request body exceeds the 1 MiB limit.", request);
                break;
            }
            case RpcMethods.HttpCancel:
            {
                var value = RpcProtocol.ParseParameters<AutomationHttpCancelParams>(request);
                ValidateModuleId(value.ModuleId, request);
                ValidateHostWindowContext(value.HostWindowContext, request);
                Require(value.RequestId is { Length: > 0 and <= 128 }, "Request id is required and must be at most 128 characters.", request);
                break;
            }
            case RpcMethods.ModuleAction:
            {
                var value = RpcProtocol.ParseParameters<AutomationModuleActionParams>(request);
                ValidateModuleId(value.ModuleId, request);
                ValidateHostWindowContext(value.HostWindowContext, request);
                Require(value.RequestId is { Length: > 0 and <= 128 }, "Request id is required and must be at most 128 characters.", request);
                Require(value.ContractVersion is > 0 and <= 16, "Module contract version is invalid.", request);
                Require(value.ActionId is { Length: > 0 and <= 64 }
                    && System.Text.RegularExpressions.Regex.IsMatch(value.ActionId, "^[a-z][A-Za-z0-9._-]*$", System.Text.RegularExpressions.RegexOptions.CultureInvariant),
                    "Module action id is invalid.", request);
                Require(value.ActionVersion is > 0 and <= 1024, "Module action version is invalid.", request);
                Require(value.Input.ValueKind is not JsonValueKind.Undefined, "Module action input is required.", request);
                Require(Encoding.UTF8.GetByteCount(value.Input.GetRawText()) <= 64 * 1024,
                    "Module action input exceeds the 64 KiB limit.", request);
                break;
            }
            case RpcMethods.ModuleActionCancel:
            {
                var value = RpcProtocol.ParseParameters<AutomationModuleActionCancelParams>(request);
                ValidateModuleId(value.ModuleId, request);
                ValidateHostWindowContext(value.HostWindowContext, request);
                Require(value.RequestId is { Length: > 0 and <= 128 }, "Request id is required and must be at most 128 characters.", request);
                break;
            }
            case RpcMethods.ModuleSettingsUpdate:
            {
                var value = RpcProtocol.ParseParameters<ModuleSettingsUpdateParams>(request);
                ValidateModuleId(value.ModuleId, request);
                ValidateHostWindowContext(value.HostWindowContext, request);
                Require(value.ContractVersion is > 0 and <= 16, "Module contract version is invalid.", request);
                Require(value.SettingsVersion is > 0 and <= 1024, "Module settings version is invalid.", request);
                Require(value.Value.ValueKind is not JsonValueKind.Undefined, "Module settings value is required.", request);
                Require(Encoding.UTF8.GetByteCount(value.Value.GetRawText()) <= 64 * 1024,
                    "Module settings exceed the 64 KiB limit.", request);
                break;
            }
            case RpcMethods.ModuleSettingsGet:
            {
                var value = RpcProtocol.ParseParameters<ModuleSettingsGetParams>(request);
                ValidateModuleId(value.ModuleId, request);
                ValidateHostWindowContext(value.HostWindowContext, request);
                Require(value.ContractVersion is > 0 and <= 16, "Module contract version is invalid.", request);
                Require(value.SettingsVersion is > 0 and <= 1024, "Module settings version is invalid.", request);
                break;
            }
            case RpcMethods.GlobalVariablesGet:
                Require(RpcProtocol.ParseParameters<GlobalVariablesGetParams>(request).Version == 1, "Global variable contract version is unsupported.", request);
                break;
            case RpcMethods.GlobalVariablesSet:
            {
                var value = RpcProtocol.ParseParameters<GlobalVariablesSetParams>(request);
                Require(value.Version == 1 && value.Values is { Count: <= 256 }, "Global variable contract or value count is invalid.", request);
                foreach (var (name, json) in value.Values)
                    Require(name is { Length: > 0 and <= 192 }
                        && System.Text.RegularExpressions.Regex.IsMatch(name, "^[A-Za-z_][A-Za-z0-9_.-]*$", System.Text.RegularExpressions.RegexOptions.CultureInvariant)
                        && name is not ("input" or "variables" or "__proto__" or "constructor" or "prototype")
                        && !name.StartsWith("secret.", StringComparison.OrdinalIgnoreCase)
                        && !name.StartsWith("system.", StringComparison.OrdinalIgnoreCase)
                        && json.ValueKind is not JsonValueKind.Undefined,
                        "A global variable name or JSON value is invalid.", request);
                Require(Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(value.Values)) <= 64 * 1024,
                    "Global variables exceed 64 KiB.", request);
                break;
            }
            case RpcMethods.ActivityList:
                _ = RpcProtocol.ParseParameters<EmptyParams>(request);
                break;
            case RpcMethods.UpdateSettings:
            {
                var value = RpcProtocol.ParseParameters<SettingsUpdateParams>(request);
                Require(HotkeyKeyNameValidator.IsSupported(value.Hotkey), "Opener key is unsupported.", request);
                Require(value.Theme is "Light" or "Dark", "Theme must be Light or Dark.", request);
                Require(value.Bindings is { Count: <= 256 }, "Bindings list is required and may contain at most 256 entries.", request);
                foreach (var binding in value.Bindings)
                {
                    Require(binding is not null && binding.Id is { Length: > 0 and <= 128 }
                        && !string.IsNullOrWhiteSpace(binding.Name) && binding.Name.Length <= 256
                        && IsAbsoluteFilePath(binding.TargetPath)
                        && binding.Alias is not null && binding.Arguments is not null && binding.Arguments.Length <= 2048,
                        "A binding has invalid or missing fields.", request);
                }
                break;
            }
            case RpcMethods.ImportSettings:
            case RpcMethods.ExportSettings:
            {
                var path = RpcProtocol.ParseParameters<SettingsFileParams>(request).Path;
                Require(IsAbsoluteFilePath(path), "Settings path must be absolute.", request);
                break;
            }
            case RpcMethods.Relink:
            {
                var value = RpcProtocol.ParseParameters<RelinkParams>(request);
                Require(value.BindingId is { Length: > 0 and <= 128 }, "Binding id is required.", request);
                Require(IsAbsoluteFilePath(value.Path), "Relink path must be absolute.", request);
                break;
            }
            case RpcMethods.Open:
            case RpcMethods.Close:
            case RpcMethods.GetState:
            case RpcMethods.OpenCatalog:
            case RpcMethods.OpenLogFolder:
            case RpcMethods.StartHotkeyRecording:
            case RpcMethods.CancelHotkeyRecording:
                _ = RpcProtocol.ParseParameters<EmptyParams>(request);
                break;
            default:
                throw new RpcProtocolException(-32601, "Unknown or unsupported method.", new { request.Method }, requestId: request.Id);
        }
    }

    private static bool IsAbsoluteFilePath(string? path) => !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path) && path.Length <= 4096;

    private static void ValidateModuleId(string? moduleId, RpcRequest request) =>
        Require(moduleId is { Length: > 0 and <= 64 }
            && System.Text.RegularExpressions.Regex.IsMatch(moduleId, "^[a-z][a-z0-9.-]*$", System.Text.RegularExpressions.RegexOptions.CultureInvariant),
            "Module id is invalid.", request);

    private static void ValidateHostWindowContext(HostWindowContextParams? context, RpcRequest request)
    {
        if (context is null) return;
        Require(context.Role is "launcher" or "workspace", "Host window role is invalid.", request);
        Require(context.SelectedTab is >= 1 and <= 9, "Host window tab must be between 1 and 9.", request);
    }

    private static bool IsKnownMode(string? mode) => mode is "launcher" or "catalog" or "settings" or "recordingHotkey" or "aliasEditing";

    public static bool TryParseHwnd(string? value, out IntPtr handle)
    {
        handle = IntPtr.Zero;
        if (value is null || value.Length is < 3 or > 18 || !value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return false;
        if (!ulong.TryParse(value.AsSpan(2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var raw)) return false;
        handle = new IntPtr(unchecked((long)raw));
        return handle != IntPtr.Zero;
    }

    private static void Require(bool condition, string message, RpcRequest request)
    {
        if (!condition) throw new RpcProtocolException(-32602, message, new { request.Method }, requestId: request.Id);
    }
}

public static class RpcMethods
{
    public const string Initialize = "initialize";
    public const string GetState = "getState";
    public const string Open = "launcher/open";
    public const string Close = "launcher/close";
    public const string SetWindowContext = "launcher/setWindowContext";
    public const string VerifyForeground = "launcher/verifyForeground";
    public const string AcquireForeground = "launcher/acquireForeground";
    public const string RestoreForeground = "launcher/restoreForeground";
    public const string SetMode = "launcher/setMode";
    public const string SelectTab = "launcher/selectTab";
    public const string SetQuery = "launcher/setQuery";
    public const string OpenCatalog = "launcher/openCatalog";
    public const string SelectCatalogApp = "catalog/select";
    public const string AddCustomApp = "catalog/addCustom";
    public const string SaveAlias = "binding/saveAlias";
    public const string RemoveBinding = "binding/remove";
    public const string SelectBinding = "binding/select";
    public const string UpdateSettings = "settings/update";
    public const string ImportSettings = "settings/import";
    public const string ExportSettings = "settings/export";
    public const string OpenLogFolder = "settings/openLogFolder";
    public const string Relink = "settings/relink";
    public const string StartHotkeyRecording = "hotkey/startRecording";
    public const string CancelHotkeyRecording = "hotkey/cancelRecording";
    public const string ActivateBinding = "launcher/activateBinding";
    public const string KeyboardEligibility = "automation/keyboardEligibility";
    public const string KeyboardSubscribe = "automation/keyboardSubscribe";
    public const string KeyboardUnsubscribe = "automation/keyboardUnsubscribe";
    public const string HttpRequest = "automation/httpRequest";
    public const string HttpCancel = "automation/httpCancel";
    public const string ModuleAction = "automation/moduleAction";
    public const string ModuleActionCancel = "automation/moduleActionCancel";
    public const string ModuleSettingsUpdate = "module/settingsUpdate";
    public const string ModuleSettingsGet = "module/settingsGet";
    public const string GlobalVariablesGet = "variables/get";
    public const string GlobalVariablesSet = "variables/set";
    public const string ActivityList = "activity/list";
    public const string ActivityChanged = "activity/changed";
    public const string VariablesChanged = "variables/changed";
    public const string KeyboardInput = "automation/keyboardInput";
    public const string KeyboardAvailability = "automation/keyboardAvailability";
    public const string AutomationNotification = "automation/notification";
}
