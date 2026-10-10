using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Automator.Application.Automation;
using Automator.Application.Launcher;
using Automator.Application.Logging;
using Automator.Infrastructure.Automation;
using Automator.Core.Automation;
using Automator.Core.Configuration;
using Automator.Core.Launcher;
using Automator.Core.Plugins;
using Automator.Protocol;
using Automator.Windows;

namespace Automator.Backend;

/// <summary>Standalone backend process. The redirected stdout stream contains JSON-RPC lines only.</summary>
public sealed class BackendServer : IAsyncDisposable
{
    private static readonly IReadOnlyDictionary<string, AutomationHttpPolicy> HostHttpPolicies =
        new Dictionary<string, AutomationHttpPolicy>(StringComparer.Ordinal);

    private readonly SerializedCommandQueue _commands;
    private readonly SemaphoreSlim _stdoutLock = new(1, 1);
    // Keep module preference reads coherent with the atomic settings write path, even if either RPC
    // is moved outside the launcher command queue in the future.
    private readonly SemaphoreSlim _moduleSettingsGate = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly AutomationRequestExecutor _automationRequests = new();
    private readonly ConcurrentDictionary<long, Task> _serviceRequestTasks = new();
    private readonly object _keyboardAvailabilityGate = new();
    private readonly PanelActivationEpoch _activationEpoch = new();
    private NativeChromeController _nativeChrome = new();
    private LauncherSession? _session;
    private LauncherSettings? _settings;
    private WindowsSettingsStore? _settingsStore;
    private FileApplicationLog? _log;
    private WindowsAppCatalog? _catalog;
    private WindowsIconCache? _iconCache;
    private ForegroundWindowMonitor? _foregroundMonitor;
    private KeyboardHookService? _keyboardHook;
    private AutomationKeyboardEventHub? _keyboardEventHub;
    private SharedHttpService? _sharedHttpService;
    private AutomationSchedulerCoordinator? _schedulerCoordinator;
    private AutomationWorkTimeCoordinator? _workTimeCoordinator;
    private PlaywrightAutomationBrowserService? _browserAutomationService;
    private AutomationLibraryTransferService? _libraryTransferService;
    private AutomationVariableService? _variableService;
    private AutomationRunActivityService? _runActivityService;
    private AutomationCapabilityRegistry? _capabilityRegistry;
    private AutomationModuleRegistry? _automationModules;
    private AutomationServicesContext? _activeServices;
    private WindowsApplicationActivator? _platformActivator;
    private AliasLaunchScheduler? _launchScheduler;
    private LauncherWindowContext _windowContext = new(IntPtr.Zero, false, false, false, "launcher");
    private LauncherModeCoordinator? _modeCoordinator;
    private Process? _hostProcess;
    private Task? _hostMonitorTask;
    private string _buildId = "uninitialized";
    private string _sessionId = Guid.NewGuid().ToString("N");
    private bool _initialized;
    private bool _testMode;
    private bool _settingsValid;
    private string? _settingsError;
    private bool _catalogLoading;
    private bool _busy;
    private string? _error;
    private long _stateRevision;
    private long _catalogGeneration;
    private long _serviceRequestSequence;
    private long _keyboardAvailabilityRevision;
    private string? _keyboardAvailabilityModuleId;
    private bool _keyboardAvailable;
    private int _disposeStarted;

    public BackendServer() => _commands = new SerializedCommandQueue(exception =>
        _log?.Write(ApplicationLogLevel.Error, "Backend.CoordinatorFailed", "A queued backend operation failed.", exception));

    public async Task<int> RunAsync()
    {
        try
        {
            while (!_lifetime.IsCancellationRequested)
            {
                // StreamReader cancellation can remain pending on an idle redirected pipe on Windows.
                // WaitAsync lets the host-process monitor stop this loop even when no input arrives.
                var line = await Task.Run(Console.In.ReadLine).WaitAsync(_lifetime.Token).ConfigureAwait(false);
                if (line is null) break;
                if (line.Length == 0) continue;
                await HandleLineAsync(line).ConfigureAwait(false);
            }

            return 0;
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            return 0;
        }
        catch (Exception exception)
        {
            _log?.Write(ApplicationLogLevel.Critical, "Backend.Fatal", "The backend exited unexpectedly.", exception);
            await Console.Error.WriteLineAsync($"Automator backend fatal: {exception}").ConfigureAwait(false);
            return 1;
        }
        finally
        {
            await DisposeAsync().ConfigureAwait(false);
        }
    }

    private Task HandleLineAsync(string line)
    {
        try
        {
            if (RpcProtocol.MustDispatchOutsideCommandQueue(RpcProtocol.ParseRequest(line).Method))
            {
                var taskId = Interlocked.Increment(ref _serviceRequestSequence);
                // Start dispatch on the reader turn so requests register before a following cancel,
                // and cancellations do not wait behind launcher state work. HTTP dispatch yields at
                // network I/O, so the request itself still runs outside the reader and command queue.
                var task = HandleLineCoreAsync(line);
                _serviceRequestTasks[taskId] = task;
                _ = ObserveServiceRequestAsync(taskId, task);
                return Task.CompletedTask;
            }
        }
        catch (RpcProtocolException)
        {
            // Malformed lines still pass through the serialized request path for the normal JSON-RPC error response.
        }

        return _commands.EnqueueAsync<bool>(async () =>
        {
            await HandleLineCoreAsync(line).ConfigureAwait(false);
            return true;
        }, _lifetime.Token);
    }

    private async Task ObserveServiceRequestAsync(long taskId, Task task)
    {
        try { await task.ConfigureAwait(false); }
        catch (Exception exception)
        {
            _log?.Write(ApplicationLogLevel.Error, "Backend.AutomationServiceDispatchFailed",
                "An automation service operation failed outside the launcher command queue.", exception);
        }
        finally { _serviceRequestTasks.TryRemove(taskId, out _); }
    }

    private async Task HandleLineCoreAsync(string line)
    {
        RpcRequest? request = null;
        try
        {
            request = RpcProtocol.ParseRequest(line);
            RpcParameterValidator.Validate(request);
            var result = await DispatchAsync(request).ConfigureAwait(false);
            await WriteProtocolAsync(RpcProtocol.Success(request, result)).ConfigureAwait(false);
        }
        catch (RpcProtocolException exception)
        {
            await WriteProtocolAsync(RpcProtocol.Failure(request?.Id, exception)).ConfigureAwait(false);
        }
        catch (AutomationServiceException exception)
        {
            var rpcException = new RpcProtocolException(-32020, exception.Message,
                new { category = ToWireCategory(exception.Category) }, requestId: request?.Id);
            await WriteProtocolAsync(RpcProtocol.Failure(request?.Id, rpcException)).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _log?.Write(ApplicationLogLevel.Error, "Backend.RequestFailed", "A backend command failed.", exception,
                request is null ? null : new Dictionary<string, object?> { ["method"] = request.Method });
            _error = UserFacingError(exception);
            var rpcException = new RpcProtocolException(-32000, _error, new { category = "command" }, requestId: request?.Id);
            await WriteProtocolAsync(RpcProtocol.Failure(request?.Id, rpcException)).ConfigureAwait(false);
            if (_initialized) await PublishStateAsync().ConfigureAwait(false);
        }
    }

    private async Task<object> DispatchAsync(RpcRequest request)
    {
        if (!_initialized)
        {
            if (request.Method != RpcMethods.Initialize)
                throw new RpcProtocolException(-32002, "The backend has not completed initialization.", requestId: request.Id);
            var initialized = await InitializeAsync(RpcProtocol.ParseParameters<InitializeParams>(request)).ConfigureAwait(false);
            _initialized = true;
            return initialized;
        }

        if (request.Method == RpcMethods.Initialize)
            throw new RpcProtocolException(-32600, "The backend can only be initialized once.", requestId: request.Id);

        switch (request.Method)
        {
            case RpcMethods.GetState:
                return GetState();
            case RpcMethods.Open:
                await OpenPanelAsync(captureUnderCursor: true).ConfigureAwait(false);
                return GetState();
            case RpcMethods.Close:
                await HidePanelAsync().ConfigureAwait(false);
                return GetState();
            case RpcMethods.SetWindowContext:
                SetWindowContext(RpcProtocol.ParseParameters<WindowContextParams>(request));
                return new { accepted = true };
            case RpcMethods.VerifyForeground:
            {
                var parameters = RpcProtocol.ParseParameters<ForegroundCheckParams>(request);
                _ = RpcParameterValidator.TryParseHwnd(parameters.WindowHandleHex, out var expected);
                var actual = WindowsForegroundWindow.CurrentHandle;
                return new
                {
                    foreground = expected != IntPtr.Zero && actual == expected,
                    windowHandleHex = parameters.WindowHandleHex,
                    foregroundHwnd = WindowsForegroundWindow.CurrentHandleHex
                };
            }
            case RpcMethods.RestoreForeground:
                return RestorePriorForeground(RpcProtocol.ParseParameters<ForegroundCheckParams>(request));
            case RpcMethods.AcquireForeground:
                return AcquirePanelForeground();
            case RpcMethods.SetMode:
                await SetModeAsync(ParseMode(RpcProtocol.ParseParameters<SetModeParams>(request).Mode)).ConfigureAwait(false);
                return GetState();
            case RpcMethods.SelectTab:
                _launchScheduler!.CancelPending();
                _session!.SelectTab(RpcProtocol.ParseParameters<SelectTabParams>(request).Tab);
                await ActivateCurrentServicesAsync().ConfigureAwait(false);
                InvalidateCatalogLoad();
                ClearError();
                await PublishStateAsync().ConfigureAwait(false);
                return GetState();
            case RpcMethods.SetQuery:
                return await SetQueryAsync(RpcProtocol.ParseParameters<SetQueryParams>(request).Query).ConfigureAwait(false);
            case RpcMethods.OpenCatalog:
                await BeginCatalogLoadingAsync().ConfigureAwait(false);
                return GetState();
            case RpcMethods.SelectCatalogApp:
            case RpcMethods.SelectBinding:
            {
                var id = RpcProtocol.ParseParameters<SelectBindingParams>(request).BindingId;
                var source = request.Method == RpcMethods.SelectCatalogApp ? _session!.Snapshot.CatalogApps : _session!.Snapshot.Bindings;
                var binding = source.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal))
                    ?? throw new RpcProtocolException(-32004, "The selected application is no longer available.", new { bindingId = id }, requestId: request.Id);
                _launchScheduler!.CancelPending();
                InvalidateCatalogLoad();
                if (!_session!.BeginAliasEdit(binding)) throw new RpcProtocolException(-32005, "The launcher is not open.", requestId: request.Id);
                await ActivateCurrentServicesAsync().ConfigureAwait(false);
                await PublishStateAsync().ConfigureAwait(false);
                return GetState();
            }
            case RpcMethods.AddCustomApp:
            {
                var path = RpcProtocol.ParseParameters<CustomAppParams>(request).Path;
                var binding = _catalog!.FromCustomPath(path);
                InvalidateCatalogLoad();
                if (_session!.Snapshot.Mode == LauncherMode.Catalog) _session.AddCatalogApp(binding);
                if (!_session.BeginAliasEdit(binding)) throw new RpcProtocolException(-32005, "Open the catalog before adding an app.", requestId: request.Id);
                await ActivateCurrentServicesAsync().ConfigureAwait(false);
                await PublishStateAsync().ConfigureAwait(false);
                return GetState();
            }
            case RpcMethods.SaveAlias:
                return await SaveAliasAsync(RpcProtocol.ParseParameters<SaveAliasParams>(request)).ConfigureAwait(false);
            case RpcMethods.RemoveBinding:
                return await RemoveBindingAsync(RpcProtocol.ParseParameters<RemoveBindingParams>(request)).ConfigureAwait(false);
            case RpcMethods.ActivateBinding:
            {
                var id = RpcProtocol.ParseParameters<ActivateBindingParams>(request).BindingId;
                var binding = _session!.Snapshot.Bindings.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal))
                    ?? throw new RpcProtocolException(-32004, "The selected binding no longer exists.", new { bindingId = id }, requestId: request.Id);
                _launchScheduler!.CancelPending();
                var activation = await StartTrackedActivationAsync(binding, _session.Snapshot.PreviousForegroundHwnd).ConfigureAwait(false);
                _ = ObserveActivationAsync(activation, binding.Id);
                return GetState();
            }
            case RpcMethods.UpdateSettings:
                return await UpdateSettingsAsync(RpcProtocol.ParseParameters<SettingsUpdateParams>(request)).ConfigureAwait(false);
            case RpcMethods.ImportSettings:
                return await ImportSettingsAsync(RpcProtocol.ParseParameters<SettingsFileParams>(request).Path).ConfigureAwait(false);
            case RpcMethods.ExportSettings:
                return await ExportBackupAsync(RpcProtocol.ParseParameters<SettingsFileParams>(request).Path).ConfigureAwait(false);
            case RpcMethods.OpenLogFolder:
                OpenLogFolder();
                return new { opened = true };
            case RpcMethods.Relink:
                return await RelinkAsync(RpcProtocol.ParseParameters<RelinkParams>(request)).ConfigureAwait(false);
            case RpcMethods.StartHotkeyRecording:
                _modeCoordinator!.SetMode(LauncherMode.RecordingHotkey);
                _keyboardHook!.StartRecording();
                await ActivateCurrentServicesAsync().ConfigureAwait(false);
                InvalidateCatalogLoad();
                await PublishStateAsync().ConfigureAwait(false);
                return GetState();
            case RpcMethods.CancelHotkeyRecording:
                _keyboardHook!.CancelRecording();
                _modeCoordinator!.SetMode(LauncherMode.Settings);
                await ActivateCurrentServicesAsync().ConfigureAwait(false);
                InvalidateCatalogLoad();
                await PublishStateAsync().ConfigureAwait(false);
                return GetState();
            case RpcMethods.KeyboardEligibility:
                return GetAutomationKeyboardEligibility(request);
            case RpcMethods.KeyboardSubscribe:
                return await SubscribeAutomationKeyboardAsync(request).ConfigureAwait(false);
            case RpcMethods.KeyboardUnsubscribe:
                return await UnsubscribeAutomationKeyboardAsync(request).ConfigureAwait(false);
            case RpcMethods.HttpRequest:
                return await SendAutomationHttpRequestAsync(request).ConfigureAwait(false);
            case RpcMethods.HttpCancel:
                return CancelAutomationHttpRequest(request);
            case RpcMethods.ModuleAction:
                return await ExecuteModuleActionAsync(request).ConfigureAwait(false);
            case RpcMethods.ModuleActionCancel:
                return CancelAutomationModuleAction(request);
            case RpcMethods.ModuleSettingsUpdate:
                return await UpdateModuleSettingsAsync(request).ConfigureAwait(false);
            case RpcMethods.ModuleSettingsGet:
                return await GetModuleSettingsAsync(request).ConfigureAwait(false);
            case RpcMethods.GlobalVariablesGet:
                return await GetGlobalVariablesAsync(request).ConfigureAwait(false);
            case RpcMethods.GlobalVariablesSet:
                return await SetGlobalVariablesAsync(request).ConfigureAwait(false);
            case RpcMethods.ActivityList:
                return await GetRunActivityAsync(request).ConfigureAwait(false);
            default:
                throw new RpcProtocolException(-32601, "Unknown or unsupported method.", requestId: request.Id);
        }
    }

    private async Task<object> SubscribeAutomationKeyboardAsync(RpcRequest request)
    {
        var parameters = RpcProtocol.ParseParameters<AutomationModuleParams>(request);
        var context = RequireActiveServiceContext(parameters.ModuleId, AutomationCapabilityIds.KeyboardInput, request,
            parameters.HostWindowContext)!;
        await context.KeyboardInput!.SubscribeAsync(async inputEvent =>
        {
            await SendNotificationAsync(RpcMethods.KeyboardInput,
                new AutomationKeyboardInputNotification(context.ModuleId, inputEvent)).ConfigureAwait(false);
        }, CancellationToken.None).ConfigureAwait(false);
        return new { subscribed = true };
    }

    private AutomationKeyboardEligibilityResult GetAutomationKeyboardEligibility(RpcRequest request)
    {
        var parameters = RpcProtocol.ParseParameters<AutomationModuleParams>(request);
        var tab = ResolveRequestedTab(parameters.ModuleId, parameters.HostWindowContext, request);
        if (parameters.HostWindowContext?.Role == "workspace")
            throw new RpcProtocolException(-32010, "The workspace cannot subscribe to global keyboard input.", new { category = "contextNotEligible" }, requestId: request.Id);
        var failure = AutomationServiceAuthorization.Evaluate(parameters.ModuleId, tab.Id, tab.Capabilities,
            AutomationCapabilityIds.KeyboardInput, contextEligible: true);
        if (failure != AutomationServiceAuthorizationFailure.None)
        {
            var (category, message) = failure switch
            {
                AutomationServiceAuthorizationFailure.InactiveModule => ("inactiveModule", "The requested module is not active."),
                AutomationServiceAuthorizationFailure.CapabilityNotGranted => ("capabilityNotGranted", "The active module does not have keyboard input."),
                _ => ("contextNotEligible", "Keyboard input is not available in this context."),
            };
            throw new RpcProtocolException(-32010, message, new { category }, requestId: request.Id);
        }

        var active = Volatile.Read(ref _activeServices);
        var delivery = CreateKeyboardDeliveryState(tab.Id);
        var eligible = active is { KeyboardInput: not null }
            && string.Equals(active.ModuleId, tab.Id, StringComparison.Ordinal)
            && delivery?.CanDeliver == true;
        if (active is { KeyboardInput: not null } && string.Equals(active.ModuleId, tab.Id, StringComparison.Ordinal))
            _keyboardEventHub?.SetDeliveryContext(delivery);
        UpdateKeyboardAvailability(eligible ? tab.Id : null, eligible);
        lock (_keyboardAvailabilityGate)
            return new AutomationKeyboardEligibilityResult(eligible, _keyboardAvailabilityRevision);
    }

    private async Task<object> UnsubscribeAutomationKeyboardAsync(RpcRequest request)
    {
        var parameters = RpcProtocol.ParseParameters<AutomationModuleParams>(request);
        var context = RequireActiveServiceContext(parameters.ModuleId, AutomationCapabilityIds.KeyboardInput, request,
            parameters.HostWindowContext, requireEligibleContext: false, allowMissingContext: true);
        if (context is not null) await context.UnsubscribeKeyboardAsync().ConfigureAwait(false);
        return new { unsubscribed = true };
    }

    private async Task<object> SendAutomationHttpRequestAsync(RpcRequest request)
    {
        var parameters = RpcProtocol.ParseParameters<AutomationHttpRequestParams>(request);
        var context = RequireActiveServiceContext(parameters.ModuleId, AutomationCapabilityIds.HttpRequest, request,
            parameters.HostWindowContext)!;
        try
        {
            return await _automationRequests.ExecuteAsync(parameters.ModuleId, parameters.RequestId,
                context.LifetimeToken, token => context.Http!.SendAsync(parameters.Request, token)).ConfigureAwait(false);
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("already active for module", StringComparison.Ordinal))
        {
            throw new RpcProtocolException(-32021, "An HTTP request with this id is already active for the module.",
                new { category = "duplicateRequestId" }, requestId: request.Id);
        }
        finally
        {
            if (parameters.HostWindowContext?.Role == "workspace") await context.DisposeAsync().ConfigureAwait(false);
        }
    }

    private object CancelAutomationHttpRequest(RpcRequest request)
    {
        var parameters = RpcProtocol.ParseParameters<AutomationHttpCancelParams>(request);
        ValidateRequestedCapability(parameters.ModuleId, AutomationCapabilityIds.HttpRequest,
            parameters.HostWindowContext, request, allowInactiveCleanup: true);
        return new { canceled = _automationRequests.Cancel(parameters.ModuleId, parameters.RequestId) };
    }

    private async Task<AutomationResult> ExecuteModuleActionAsync(RpcRequest request)
    {
        var parameters = RpcProtocol.ParseParameters<AutomationModuleActionParams>(request);
        var activeTab = ResolveRequestedTab(parameters.ModuleId, parameters.HostWindowContext, request);
        var registered = ValidateRegisteredModule(activeTab, request, requireSettingsVersion: false);
        var workspace = parameters.HostWindowContext?.Role == "workspace";
        var context = workspace
            ? CreateModuleServices(activeTab, request)
            : RequireActiveServiceContext(parameters.ModuleId, null, request, parameters.HostWindowContext)!;

        try
        {
            return await _automationRequests.ExecuteAsync(parameters.ModuleId, parameters.RequestId,
                context.LifetimeToken, async token =>
                {
                    var observed = AutomationRunActivityService.IsObservedAction(parameters.ModuleId, parameters.ActionId);
                    var startedUtc = DateTimeOffset.UtcNow;
                    var timer = Stopwatch.StartNew();
                    var status = AutomationStatus.Error;
                    try
                    {
                        var result = await _automationModules!.DispatchAsync(parameters.ModuleId, parameters.ActionId,
                            parameters.ContractVersion, parameters.ActionVersion, parameters.Input, activeTab.Id,
                            context, _settings!.ModuleSettings, token).ConfigureAwait(false);
                        status = result.Status;
                        return result;
                    }
                    catch (OperationCanceledException)
                    {
                        status = AutomationStatus.Warning;
                        throw;
                    }
                    finally
                    {
                        timer.Stop();
                        if (observed)
                        {
                            var finishedUtc = DateTimeOffset.UtcNow;
                            await RecordRunActivitySafelyAsync(new AutomationRunActivityEntry(
                                parameters.RequestId, parameters.ModuleId, parameters.ActionId,
                                AutomationRunActivityService.ReadProfileId(parameters.Input), startedUtc, finishedUtc,
                                status, Math.Max(0, (long)timer.Elapsed.TotalMilliseconds), AutomationExecutionOrigin.Manual))
                                .ConfigureAwait(false);
                        }
                        else if (parameters.ModuleId == "workflows" && parameters.ActionId == "runWorkflow")
                        {
                            // The engine already retains its metadata; refresh the renderer without writing another record.
                            await PublishActivityChangedSafelyAsync().ConfigureAwait(false);
                        }
                    }
                }).ConfigureAwait(false);
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("already active for module", StringComparison.Ordinal))
        {
            throw new RpcProtocolException(-32021, "A module action with this id is already active for the module.",
                new { category = "duplicateRequestId" }, requestId: request.Id);
        }
        catch (OperationCanceledException exception)
        {
            throw new RpcProtocolException(-32800, "The module action was canceled because its active context ended.",
                new { category = "canceled" }, exception, request.Id);
        }
        catch (InvalidOperationException exception)
        {
            throw new RpcProtocolException(-32011, exception.Message,
                new { category = "moduleActionRejected" }, exception, request.Id);
        }
        finally
        {
            if (workspace) await context.DisposeAsync().ConfigureAwait(false);
        }
    }

    private Task<AutomationVariableSnapshot> GetGlobalVariablesAsync(RpcRequest request)
    {
        _ = RpcProtocol.ParseParameters<GlobalVariablesGetParams>(request);
        return _variableService!.GetAsync(_lifetime.Token);
    }

    private async Task<AutomationVariableSnapshot> SetGlobalVariablesAsync(RpcRequest request)
    {
        var parameters = RpcProtocol.ParseParameters<GlobalVariablesSetParams>(request);
        try
        {
            var snapshot = await _variableService!.SetAsync(parameters.Values, _lifetime.Token).ConfigureAwait(false);
            await PublishGlobalVariablesChangedSafelyAsync().ConfigureAwait(false);
            return snapshot;
        }
        catch (InvalidDataException exception)
        {
            throw new RpcProtocolException(-32602, exception.Message, innerException: exception, requestId: request.Id);
        }
    }

    private async Task<AutomationRunActivitySnapshot> GetRunActivityAsync(RpcRequest request)
    {
        _ = RpcProtocol.ParseParameters<EmptyParams>(request);
        return await _runActivityService!.GetSnapshotAsync(_lifetime.Token).ConfigureAwait(false);
    }

    private async Task RecordRunActivitySafelyAsync(AutomationRunActivityEntry entry)
    {
        try
        {
            await _runActivityService!.RecordAsync(entry, _lifetime.Token).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // Run-history failure must never change the automation's result or cancellation state.
            _log?.Write(ApplicationLogLevel.Warning, "Automation.ActivityWriteFailed",
                "Run activity metadata could not be saved.", exception,
                new Dictionary<string, object?> { ["moduleId"] = entry.ModuleId, ["actionId"] = entry.ActionId });
            return;
        }

        await PublishActivityChangedSafelyAsync().ConfigureAwait(false);
    }

    private async Task PublishActivityChangedSafelyAsync()
    {
        try { await SendNotificationAsync(RpcMethods.ActivityChanged, new { }).ConfigureAwait(false); }
        catch (Exception exception)
        {
            _log?.Write(ApplicationLogLevel.Warning, "Automation.ActivityNotificationFailed",
                "The renderer could not be notified that run activity changed.", exception);
        }
    }

    private async Task PublishGlobalVariablesChangedSafelyAsync()
    {
        try { await SendNotificationAsync(RpcMethods.VariablesChanged, new { }).ConfigureAwait(false); }
        catch (Exception exception)
        {
            _log?.Write(ApplicationLogLevel.Warning, "Automation.VariablesNotificationFailed",
                "The renderer could not be notified that global variables changed.", exception);
        }
    }

    private object CancelAutomationModuleAction(RpcRequest request)
    {
        var parameters = RpcProtocol.ParseParameters<AutomationModuleActionCancelParams>(request);
        try { _ = ResolveRequestedTab(parameters.ModuleId, parameters.HostWindowContext, request); }
        catch (RpcProtocolException) { return new { canceled = false }; }
        return new { canceled = _automationRequests.Cancel(parameters.ModuleId, parameters.RequestId) };
    }

    private async Task<object> UpdateModuleSettingsAsync(RpcRequest request)
    {
        await _moduleSettingsGate.WaitAsync(_lifetime.Token).ConfigureAwait(false);
        try
        {
            return await UpdateModuleSettingsCoreAsync(request).ConfigureAwait(false);
        }
        finally
        {
            _moduleSettingsGate.Release();
        }
    }

    private async Task<object> UpdateModuleSettingsCoreAsync(RpcRequest request)
    {
        var parameters = RpcProtocol.ParseParameters<ModuleSettingsUpdateParams>(request);
        var activeTab = ResolveRequestedTab(parameters.ModuleId, parameters.HostWindowContext, request);
        _ = ValidateRegisteredModule(activeTab, request, requireSettingsVersion: true);

        try
        {
            var moduleSettings = _automationModules!.CreateUpdatedModuleSettings(parameters.ModuleId,
                parameters.ContractVersion, parameters.SettingsVersion, parameters.Value, activeTab.Id, _settings!.ModuleSettings);
            var settings = _settings with { ModuleSettings = moduleSettings };
            _ = SettingsSerializer.Serialize(settings); // Validate the aggregate and all preserved entries before writing.
            _settingsStore!.Save(settings);
            _settings = settings;
            ClearError();
            await PublishStateAsync().ConfigureAwait(false);
            return new { saved = true, moduleId = parameters.ModuleId, settingsVersion = parameters.SettingsVersion };
        }
        catch (InvalidOperationException exception)
        {
            throw new RpcProtocolException(-32011, exception.Message,
                new { category = "moduleSettingsRejected" }, exception, request.Id);
        }
    }

    private async Task<AutomationModuleSettingsSnapshot> GetModuleSettingsAsync(RpcRequest request)
    {
        await _moduleSettingsGate.WaitAsync(_lifetime.Token).ConfigureAwait(false);
        try
        {
            return GetModuleSettingsCore(request);
        }
        finally
        {
            _moduleSettingsGate.Release();
        }
    }

    private AutomationModuleSettingsSnapshot GetModuleSettingsCore(RpcRequest request)
    {
        var parameters = RpcProtocol.ParseParameters<ModuleSettingsGetParams>(request);
        var activeTab = ResolveRequestedTab(parameters.ModuleId, parameters.HostWindowContext, request);
        _ = ValidateRegisteredModule(activeTab, request, requireSettingsVersion: true);

        try
        {
            return _automationModules!.GetActiveSettings(parameters.ModuleId, parameters.ContractVersion,
                parameters.SettingsVersion, activeTab.Id, _settings!.ModuleSettings);
        }
        catch (InvalidOperationException exception)
        {
            throw new RpcProtocolException(-32011, exception.Message,
                new { category = "moduleSettingsRejected" }, exception, request.Id);
        }
    }

    private AutomationServicesContext? RequireActiveServiceContext(
        string moduleId,
        string? capability,
        RpcRequest request,
        HostWindowContextParams? hostWindowContext = null,
        bool requireEligibleContext = true,
        bool allowMissingContext = false)
    {
        var selectedTab = ResolveRequestedTab(moduleId, hostWindowContext, request);
        if (hostWindowContext?.Role == "workspace")
        {
            if (string.Equals(capability, AutomationCapabilityIds.KeyboardInput, StringComparison.Ordinal))
                throw new RpcProtocolException(-32010, "The workspace cannot subscribe to global keyboard input.", new { category = "contextNotEligible" }, requestId: request.Id);
            ValidateRequestedCapability(moduleId, capability, hostWindowContext, request);
            return CreateModuleServices(selectedTab, request);
        }

        var delivery = CreateKeyboardDeliveryState(selectedTab.Id);
        RefreshKeyboardDeliveryContext();
        var activeContext = Volatile.Read(ref _activeServices);
        if (capability is not null)
        {
            var failure = AutomationServiceAuthorization.Evaluate(moduleId, selectedTab.Id, selectedTab.Capabilities,
                capability, delivery?.CanDeliver == true, requireEligibleContext);
            if (failure != AutomationServiceAuthorizationFailure.None)
            {
                var (category, message) = failure switch
                {
                    AutomationServiceAuthorizationFailure.InactiveModule => ("inactiveModule", "The requested module is not active."),
                    AutomationServiceAuthorizationFailure.CapabilityNotGranted => ("capabilityNotGranted", "The active module does not have this capability."),
                    _ => ("contextNotEligible", "Automation services require the visible, focused launcher context."),
                };
                throw new RpcProtocolException(-32010, message, new { category }, requestId: request.Id);
            }
        }
        if (activeContext is null || !string.Equals(activeContext.ModuleId, selectedTab.Id, StringComparison.Ordinal))
        {
            if (allowMissingContext) return null;
            throw new RpcProtocolException(-32010, "The active module services are unavailable.", new { category = "contextNotEligible" }, requestId: request.Id);
        }
        return activeContext;
    }

    private LauncherTabMetadata ResolveRequestedTab(string moduleId, HostWindowContextParams? hostWindowContext, RpcRequest request)
    {
        var snapshot = _session?.Snapshot
            ?? throw new RpcProtocolException(-32010, "No automation module is active.", new { category = "inactiveModule" }, requestId: request.Id);
        var slot = snapshot.SelectedTab;
        if (hostWindowContext is not null)
        {
            if (hostWindowContext.Role == "workspace") slot = hostWindowContext.SelectedTab;
            else if (hostWindowContext.Role == "launcher")
            {
                if (!snapshot.Visible || snapshot.Mode != LauncherMode.Launcher || hostWindowContext.SelectedTab != snapshot.SelectedTab)
                    throw new RpcProtocolException(-32010, "Module actions require the selected launcher tab.", new { category = "inactiveModule" }, requestId: request.Id);
            }
            else throw new RpcProtocolException(-32602, "The host window role is invalid.", new { category = "invalidWindowContext" }, requestId: request.Id);
        }
        else if (!snapshot.Visible || snapshot.Mode != LauncherMode.Launcher)
        {
            throw new RpcProtocolException(-32010, "Module actions require an active launcher tab.", new { category = "inactiveModule" }, requestId: request.Id);
        }

        var tab = LauncherTabRegistry.Tabs.FirstOrDefault(candidate => candidate.Slot == slot);
        if (tab is null || !string.Equals(tab.Id, moduleId, StringComparison.Ordinal))
            throw new RpcProtocolException(-32010, "The requested automation module is not selected in this window.", new { category = "inactiveModule" }, requestId: request.Id);
        return tab;
    }

    private void ValidateRequestedCapability(
        string moduleId,
        string? capability,
        HostWindowContextParams? hostWindowContext,
        RpcRequest request,
        bool allowInactiveCleanup = false)
    {
        LauncherTabMetadata tab;
        try { tab = ResolveRequestedTab(moduleId, hostWindowContext, request); }
        catch (RpcProtocolException) when (allowInactiveCleanup) { return; }
        if (capability is null) return;
        var failure = AutomationServiceAuthorization.Evaluate(moduleId, tab.Id, tab.Capabilities, capability, true, false);
        if (failure == AutomationServiceAuthorizationFailure.None) return;
        var message = failure == AutomationServiceAuthorizationFailure.CapabilityNotGranted
            ? "The active module does not have this capability."
            : "The requested module is not active.";
        throw new RpcProtocolException(-32010, message, new { category = failure == AutomationServiceAuthorizationFailure.CapabilityNotGranted ? "capabilityNotGranted" : "inactiveModule" }, requestId: request.Id);
    }

    private AutomationModuleDefinition ValidateRegisteredModule(LauncherTabMetadata tab, RpcRequest request, bool requireSettingsVersion)
    {
        var registered = _automationModules?.Modules.FirstOrDefault(module => string.Equals(module.Id, tab.Id, StringComparison.Ordinal));
        if (registered is null)
            throw new RpcProtocolException(-32011, $"Automation module '{tab.Id}' is not registered.", new { category = "unregisteredModule" }, requestId: request.Id);
        if (registered.Slot != tab.Slot || registered.ContractVersion != tab.ContractVersion
            || (requireSettingsVersion && registered.SettingsVersion != tab.SettingsVersion)
            || !registered.Capabilities.SequenceEqual(tab.Capabilities))
            throw new RpcProtocolException(-32011, "Registered module metadata does not match the selected tab.", new { category = "staleModule" }, requestId: request.Id);
        return registered;
    }

    private AutomationServicesContext CreateModuleServices(LauncherTabMetadata tab, RpcRequest request)
    {
        var hasHttpGrant = tab.Capabilities.Any(capability => capability.Id == AutomationCapabilityIds.HttpRequest);
        AutomationHttpPolicy? httpPolicy = null;
        if (hasHttpGrant && !HostHttpPolicies.TryGetValue(tab.Id, out httpPolicy))
            throw new RpcProtocolException(-32011, $"Module '{tab.Id}' has no host-owned HTTP policy.", new { category = "capabilityNotGranted" }, requestId: request.Id);
        try
        {
            return _capabilityRegistry!.CreateContext(new AutomationModuleDescriptor(tab.Id, tab.Capabilities, httpPolicy));
        }
        catch (InvalidOperationException exception)
        {
            throw new RpcProtocolException(-32010, exception.Message, new { category = "contextNotEligible" }, exception, request.Id);
        }
    }

    private KeyboardDeliveryState? CreateKeyboardDeliveryState(string moduleId)
    {
        if (_session is null) return null;
        var snapshot = _session.Snapshot;
        var window = Volatile.Read(ref _windowContext);
        var modeMatches = string.Equals(window.Mode, ToWireMode(snapshot.Mode), StringComparison.Ordinal);
        var nativeForeground = window.Handle != IntPtr.Zero && WindowsForegroundWindow.CurrentHandle == window.Handle;
        var keyRecording = (_keyboardHook?.IsRecording ?? false) || snapshot.Mode == LauncherMode.RecordingHotkey;
        return new KeyboardDeliveryState(moduleId,
            snapshot.Visible && snapshot.Mode == LauncherMode.Launcher && window.Visible && modeMatches,
            nativeForeground,
            window.RendererFocused && modeMatches,
            window.NativeDialogActive,
            keyRecording);
    }

    private void RefreshKeyboardDeliveryContext()
    {
        var hub = _keyboardEventHub;
        if (hub is null) return;
        var active = Volatile.Read(ref _activeServices);
        if (active is null || _session is null)
        {
            hub.SetDeliveryContext(null);
            UpdateKeyboardAvailability(null, false);
            return;
        }

        var selectedModuleId = LauncherTabRegistry.Tabs.First(tab => tab.Slot == _session.Snapshot.SelectedTab).Id;
        if (!string.Equals(active.ModuleId, selectedModuleId, StringComparison.Ordinal))
        {
            hub.SetDeliveryContext(null);
            UpdateKeyboardAvailability(null, false);
            return;
        }
        if (active.KeyboardInput is null)
        {
            hub.SetDeliveryContext(null);
            UpdateKeyboardAvailability(null, false);
            return;
        }

        var delivery = CreateKeyboardDeliveryState(active.ModuleId);
        hub.SetDeliveryContext(delivery);
        UpdateKeyboardAvailability(active.ModuleId, delivery?.CanDeliver == true);
    }

    private void UpdateKeyboardAvailability(string? moduleId, bool eligible)
    {
        List<AutomationKeyboardAvailabilityNotification>? notifications = null;
        lock (_keyboardAvailabilityGate)
        {
            if (_keyboardAvailabilityModuleId is { } previousModule
                && !string.Equals(previousModule, moduleId, StringComparison.Ordinal)
                && _keyboardAvailable)
            {
                notifications = [new AutomationKeyboardAvailabilityNotification(previousModule, false, ++_keyboardAvailabilityRevision)];
            }

            if (moduleId is null)
            {
                _keyboardAvailabilityModuleId = null;
                _keyboardAvailable = false;
            }
            else
            {
                var moduleChanged = !string.Equals(_keyboardAvailabilityModuleId, moduleId, StringComparison.Ordinal);
                var eligibilityChanged = moduleChanged || _keyboardAvailable != eligible;
                _keyboardAvailabilityModuleId = moduleId;
                _keyboardAvailable = eligible;
                if (eligibilityChanged && eligible)
                {
                    (notifications ??= []).Add(new AutomationKeyboardAvailabilityNotification(moduleId, true, ++_keyboardAvailabilityRevision));
                }
                else if (eligibilityChanged && !moduleChanged)
                {
                    (notifications ??= []).Add(new AutomationKeyboardAvailabilityNotification(moduleId, false, ++_keyboardAvailabilityRevision));
                }
            }
        }

        if (notifications is null) return;
        foreach (var notification in notifications)
            _commands.TryPost(async () => await SendNotificationAsync(RpcMethods.KeyboardAvailability, notification).ConfigureAwait(false));
    }

    private async Task ActivateCurrentServicesAsync()
    {
        if (_session is null || !_session.Snapshot.Visible || _session.Snapshot.Mode != LauncherMode.Launcher)
        {
            await DeactivateServicesAsync().ConfigureAwait(false);
            return;
        }

        var tab = LauncherTabRegistry.Tabs.First(candidate => candidate.Slot == _session.Snapshot.SelectedTab);
        var active = Volatile.Read(ref _activeServices);
        if (active is not null && string.Equals(active.ModuleId, tab.Id, StringComparison.Ordinal)) return;
        await DeactivateServicesAsync().ConfigureAwait(false);

        var hasHttpGrant = tab.Capabilities.Any(capability => capability.Id == AutomationCapabilityIds.HttpRequest);
        AutomationHttpPolicy? httpPolicy = null;
        if (hasHttpGrant && !HostHttpPolicies.TryGetValue(tab.Id, out httpPolicy))
            throw new InvalidOperationException($"Module '{tab.Id}' has an HTTP capability without a host-owned policy.");

        var descriptor = new AutomationModuleDescriptor(tab.Id, tab.Capabilities, httpPolicy);
        var context = _capabilityRegistry!.CreateContext(descriptor);
        Volatile.Write(ref _activeServices, context);
        RefreshKeyboardDeliveryContext();
    }

    private async Task DeactivateServicesAsync()
    {
        _keyboardEventHub?.SetDeliveryContext(null);
        var previous = Interlocked.Exchange(ref _activeServices, null);
        UpdateKeyboardAvailability(null, false);
        if (previous is null) return;
        _automationRequests.CancelModule(previous.ModuleId);
        await previous.DisposeAsync().ConfigureAwait(false);
    }

    private void OnAutomationInputReceived(KeyInputEvent inputEvent)
    {
        RefreshKeyboardDeliveryContext();
        _keyboardEventHub?.Publish(inputEvent);
    }

    private async Task<BackendInitializeResult> InitializeAsync(InitializeParams parameters)
    {
        // RpcParameterValidator has checked the version and required fields before any OS state is touched.
        _buildId = parameters.BuildId;
        _testMode = parameters.TestMode;
        _sessionId = Guid.NewGuid().ToString("N");
        try
        {
            _hostProcess = Process.GetProcessById(parameters.HostProcessId);
            if (_hostProcess.HasExited) throw new InvalidOperationException("The desktop host has already exited.");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            throw new RpcProtocolException(-32602, "The desktop host process is not available.", innerException: exception);
        }
        var dataDirectory = _testMode
            ? parameters.DataDirectory!
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Automator");
        _log = new FileApplicationLog(Path.Combine(dataDirectory, "Logs"), "backend", _buildId, _sessionId);
        var addressResolver = new HostAddressResolver();
        var publicNetworkClient = SharedHttpService.CreateProductionClient(addressResolver, allowLocalNetwork: false);
        var localNetworkClient = SharedHttpService.CreateProductionClient(addressResolver, allowLocalNetwork: true);
        _sharedHttpService = new SharedHttpService(publicNetworkClient, localNetworkClient, addressResolver, _log);
        _keyboardEventHub = new AutomationKeyboardEventHub(_log);
        var libraryStore = new SqliteAutomationLibraryStore(Path.Combine(dataDirectory, "library.db"));
        _variableService = new AutomationVariableService(libraryStore);
        _runActivityService = new AutomationRunActivityService(libraryStore);
        var processService = new LocalProcessExecutionService();
        IAutomationSecretManager secretManager;
        IAutomationSecretValueReader secretValueReader;
        if (_testMode)
        {
            var isolatedSecrets = new InMemoryAutomationSecretManager();
            secretManager = isolatedSecrets;
            secretValueReader = isolatedSecrets;
        }
        else
        {
            var windowsSecrets = new WindowsCredentialSecretManager();
            secretManager = windowsSecrets;
            secretValueReader = windowsSecrets;
        }
        var apiProfileRunner = new AutomationApiProfileRunner(libraryStore, secretValueReader, _sharedHttpService!, _log!, _variableService);
        _libraryTransferService = new AutomationLibraryTransferService(libraryStore,
            (profileId, secretId, cancellationToken) => secretManager.ExistsAsync(profileId, secretId, cancellationToken));
        var browserService = _browserAutomationService = new PlaywrightAutomationBrowserService(
            Environment.GetEnvironmentVariable("AUTOMATOR_BROWSER_NODE_PATH") ?? Path.Combine(AppContext.BaseDirectory, "node.exe"),
            Environment.GetEnvironmentVariable("AUTOMATOR_BROWSER_WORKER_PATH") ?? Path.Combine(AppContext.BaseDirectory, "browser-worker.cjs"),
            Environment.GetEnvironmentVariable("AUTOMATOR_PLAYWRIGHT_MODULE_ROOT") ?? Path.Combine(AppContext.BaseDirectory, "node_modules", "playwright"),
            Path.Combine(dataDirectory, "Browser"), libraryStore, addressResolver, _log!);
        var savedProfileExecutor = new AutomationSavedProfileExecutor(
        [
            new ScriptRunnerSavedProfileHandler(libraryStore, processService, _variableService),
            new ApiSavedProfileHandler(apiProfileRunner, libraryStore),
            new BrowserSavedProfileHandler(libraryStore, browserService, _variableService),
            new PlaywrightTaskSavedProfileHandler(libraryStore, processService),
        ]);
        var workflowEngine = new AutomationWorkflowEngine(libraryStore, savedProfileExecutor, _variableService);
        var schedulerCoordinator = _schedulerCoordinator = new AutomationSchedulerCoordinator(libraryStore, workflowEngine,
            log: _log!, savedProfiles: savedProfileExecutor, historyChanged: PublishActivityChangedSafelyAsync);
        // Slot seven now uses its independent work-time records. Preserve legacy Pomodoro records
        // verbatim in the shared library and do not resume the old background timer.
        var workTimeCoordinator = _workTimeCoordinator = new AutomationWorkTimeCoordinator(libraryStore);
        await workTimeCoordinator.InitializeAsync(_lifetime.Token).ConfigureAwait(false);
        await schedulerCoordinator.StartAsync(_lifetime.Token).ConfigureAwait(false);
        _capabilityRegistry = new AutomationCapabilityRegistry(
            moduleId => new BackendKeyboardInput(_keyboardEventHub!, moduleId),
            (moduleId, policy) => _sharedHttpService!.ForModule(moduleId, policy),
            _ => libraryStore,
            _ => processService,
            _ => secretManager,
            _ => apiProfileRunner,
            browserServiceFactory: _ => browserService,
            workflowRunnerFactory: _ => workflowEngine,
            schedulerCoordinatorFactory: _ => schedulerCoordinator,
            workTimeCoordinatorFactory: _ => workTimeCoordinator,
            websiteLauncherFactory: _ => new WindowsAutomationWebsiteLauncher(),
            codexTaskServiceFactory: moduleId => moduleId == "codex" ? new CodexTaskService(dataDirectory, processService,
                library: libraryStore, workflows: workflowEngine) : null);
        _automationModules = LauncherTabRegistry.CreateAutomationRegistry(_capabilityRegistry, _variableService, dataDirectory);
        _hostMonitorTask = MonitorHostProcessAsync(_hostProcess!);
        _nativeChrome = new NativeChromeController(_log);
        _log.Write(ApplicationLogLevel.Information, "Backend.Initialize", "Backend protocol initialization was accepted.",
            properties: new Dictionary<string, object?>
            {
                ["hostProcessId"] = parameters.HostProcessId,
                ["testMode"] = _testMode,
                ["protocolVersion"] = parameters.ProtocolVersion
            });
        _settingsStore = new WindowsSettingsStore(_testMode, _testMode ? dataDirectory : null, _log, _automationModules);
        _settingsStore.SetDesktopExecutable(parameters.HostExecutablePath, parameters.PortableExecutablePath);
        var loaded = _settingsStore.Load();
        _settings = loaded.Settings;
        _settingsValid = loaded.IsValid;
        _settingsError = loaded.Error;
        _session = new LauncherSession(_settings);
        _catalog = new WindowsAppCatalog(parameters.HostProcessId, parameters.HostExecutablePath, _log);
        _iconCache = new WindowsIconCache(_log);
        _foregroundMonitor = new ForegroundWindowMonitor(_log);
        var foregroundHookReady = _foregroundMonitor.Start(TimeSpan.FromSeconds(2));
        if (!foregroundHookReady)
            _log.Write(ApplicationLogLevel.Warning, "Windows.ForegroundHookUnavailable", "Foreground MRU history could not be installed.");

        _platformActivator = new WindowsApplicationActivator(_foregroundMonitor, parameters.HostProcessId, parameters.HostExecutablePath, _log);
        _launchScheduler = new AliasLaunchScheduler(new TaskLaunchDelay(), new TrackingActivator(this));
        _modeCoordinator = new LauncherModeCoordinator(_session, _launchScheduler);
        _keyboardHook = new KeyboardHookService(_settings.Hotkey, () => _session.NavigationState,
            () => Volatile.Read(ref _windowContext), _log);
        _keyboardHook.ToggleRequested += OnToggleRequested;
        _keyboardHook.NavigationRequested += OnNavigationRequested;
        _keyboardHook.KeyRecorded += OnKeyRecorded;
        _keyboardHook.InputReceived += OnAutomationInputReceived;
        var hookInstalled = _keyboardHook.Start(TimeSpan.FromSeconds(2));
        _log.Write(hookInstalled ? ApplicationLogLevel.Information : ApplicationLogLevel.Error,
            hookInstalled ? "Keyboard.HookReady" : "Keyboard.HookUnavailable",
            hookInstalled ? "The configured global opener is ready." : "The launcher is running without its global opener.",
            properties: new Dictionary<string, object?>
            {
                ["openerKey"] = _settings.Hotkey,
                ["hookInstallError"] = _keyboardHook.InstallErrorCode,
                ["foregroundMonitorReady"] = foregroundHookReady
            });

        if (!_testMode && _settingsValid)
        {
            try { _settingsStore.ApplyStartupSetting(_settings.StartWithWindows); }
            catch (Exception exception)
            {
                _log.Write(ApplicationLogLevel.Warning, "Startup.ApplyFailed", "Could not point Windows startup to the desktop host.", exception);
            }
        }

        return new BackendInitializeResult(GetState(), hookInstalled, _keyboardHook.InstallErrorCode, _sessionId);
    }

    private BackendUiState GetState()
    {
        var session = _session?.Snapshot ?? throw new InvalidOperationException("Backend is not initialized.");
        var settings = _settings ?? throw new InvalidOperationException("Backend settings are not loaded.");
        var match = _session.ResolveQuery();
        var missing = _settingsValid
            ? SettingsSerializer.FindMissingPaths(settings, File.Exists)
                .Select(path => settings.Bindings.FirstOrDefault(binding => string.Equals(binding.TargetPath, path, StringComparison.OrdinalIgnoreCase)))
                .Where(binding => binding is not null)
                .Select(binding => new MissingBindingState(binding!.Id, binding.Name))
                .ToArray()
            : [];
        var catalog = session.CatalogApps.Where(binding => string.IsNullOrWhiteSpace(session.Query)
            || binding.Name.Contains(session.Query, StringComparison.CurrentCultureIgnoreCase)
            || binding.Alias.Contains(session.Query, StringComparison.CurrentCultureIgnoreCase)).ToArray();
        var moduleStates = LauncherTabRegistry.States.ToArray();
        var websiteStateIndex = Array.FindIndex(moduleStates, item => item.ModuleId == WebsiteLauncherModule.IdValue);
        if (websiteStateIndex >= 0 && _automationModules is not null)
        {
            try
            {
                var websiteSettings = _automationModules.GetSettings(WebsiteLauncherModule.IdValue, settings.ModuleSettings);
                var values = new Dictionary<string, string>(moduleStates[websiteStateIndex].Values, StringComparer.Ordinal)
                {
                    ["globalActions"] = WebsiteLauncherModule.SerializeQuickActions(websiteSettings)
                };
                moduleStates[websiteStateIndex] = moduleStates[websiteStateIndex] with { Values = values };
            }
            catch (Exception exception) when (exception is InvalidOperationException or JsonException or InvalidDataException)
            {
                // Invalid imported preferences are not exposed to Global Action Search; the tab reports the settings issue.
            }
        }
        return new BackendUiState(
            RpcProtocol.Version,
            _buildId,
            Interlocked.Read(ref _stateRevision),
            LauncherTabRegistry.Version,
            moduleStates,
            session.Visible,
            session.SelectedTab,
            session.Query,
            session.PreviousForegroundHwnd,
            ToWireMode(session.Mode),
            LauncherTabRegistry.Tabs,
            session.Bindings.Select(ToBindingState).ToArray(),
            catalog.Take(256).Select(ToBindingState).ToArray(),
            catalog.Length,
            session.AliasEditCandidate is null ? null : ToBindingState(session.AliasEditCandidate),
            settings.Hotkey,
            settings.Theme.ToString(),
            settings.StartWithWindows,
            _settingsValid,
            _settingsError,
            missing,
            _catalogLoading,
            _keyboardHook?.IsInstalled ?? false,
            _busy,
            _error,
            match.Kind.ToString(),
            match.Binding?.Id);
    }

    private BindingUiState ToBindingState(AppBinding binding) => new(
        binding.Id,
        binding.Name,
        binding.TargetPath,
        binding.Alias,
        binding.Arguments,
        _iconCache?.GetDataUrl(binding.TargetPath));

    private void SetWindowContext(WindowContextParams parameters)
    {
        if (!RpcParameterValidator.TryParseHwnd(parameters.WindowHandleHex, out var hwnd))
            throw new RpcProtocolException(-32602, "Window handle is invalid.");
        var currentMode = ToWireMode(_session!.Snapshot.Mode);
        // A stale renderer context cannot authorize global navigation after a mode transition.
        var modeMatches = string.Equals(parameters.Mode, currentMode, StringComparison.Ordinal);
        Volatile.Write(ref _windowContext, new LauncherWindowContext(hwnd, parameters.Visible && modeMatches,
            parameters.RendererFocused && modeMatches, parameters.NativeDialogActive, parameters.Mode));
        if (hwnd != IntPtr.Zero) _nativeChrome.Apply(hwnd);
        RefreshKeyboardDeliveryContext();
    }

    private object AcquirePanelForeground()
    {
        var context = Volatile.Read(ref _windowContext);
        var snapshot = _session!.Snapshot;
        if (!snapshot.Visible || !context.Visible || context.Handle == IntPtr.Zero
            || context.NativeDialogActive || !string.Equals(context.Mode, ToWireMode(snapshot.Mode), StringComparison.Ordinal))
        {
            return new { acquired = false, foregroundHwnd = WindowsForegroundWindow.CurrentHandleHex, inputQueuesAttached = false, attachError = 0 };
        }

        var attempt = WindowsForegroundWindow.TryBringToForeground(context.Handle, _hostProcess?.Id ?? -1);
        _log!.Write(attempt.IsForeground ? ApplicationLogLevel.Information : ApplicationLogLevel.Warning,
            attempt.IsForeground ? "Panel.ForegroundAcquired" : "Panel.ForegroundAcquireFailed",
            attempt.IsForeground ? "Native input queues helped Windows activate the launcher." : "The native foreground handoff was not accepted.",
            properties: new Dictionary<string, object?>
            {
                ["windowHandle"] = $"0x{unchecked((ulong)context.Handle.ToInt64()):X}",
                ["foregroundHwnd"] = attempt.ForegroundWindowHex,
                ["inputQueuesAttached"] = attempt.InputQueuesAttached,
                ["attachError"] = attempt.AttachErrorCode,
                ["bringToTopSucceeded"] = attempt.BringToTopSucceeded,
                ["setForegroundSucceeded"] = attempt.SetForegroundSucceeded
            });
        return new
        {
            acquired = attempt.IsForeground,
            foregroundHwnd = attempt.ForegroundWindowHex,
            inputQueuesAttached = attempt.InputQueuesAttached,
            attachError = attempt.AttachErrorCode,
            attempt.BringToTopSucceeded,
            attempt.SetForegroundSucceeded
        };
    }

    private object RestorePriorForeground(ForegroundCheckParams parameters)
    {
        if (!RpcParameterValidator.TryParseHwnd(parameters.WindowHandleHex, out var target) || target == IntPtr.Zero)
            return new { restored = false, foregroundHwnd = WindowsForegroundWindow.CurrentHandleHex, reason = "target-unavailable" };

        var panel = Volatile.Read(ref _windowContext).Handle;
        var current = WindowsForegroundWindow.CurrentHandle;
        if (panel == IntPtr.Zero || current != panel)
            return new { restored = false, foregroundHwnd = WindowsForegroundWindow.CurrentHandleHex, reason = "foreground-already-changed" };

        var attempt = WindowsForegroundWindow.TryBringToForeground(target);
        _log!.Write(attempt.IsForeground ? ApplicationLogLevel.Information : ApplicationLogLevel.Warning,
            attempt.IsForeground ? "Panel.PreviousForegroundRestored" : "Panel.PreviousForegroundRestoreFailed",
            attempt.IsForeground ? "Restored the window that was active before the launcher opened." : "Windows did not accept restoration of the prior foreground window.",
            properties: new Dictionary<string, object?>
            {
                ["launcherHandle"] = $"0x{unchecked((ulong)panel.ToInt64()):X}",
                ["targetHandle"] = parameters.WindowHandleHex,
                ["foregroundHwnd"] = attempt.ForegroundWindowHex,
                ["inputQueuesAttached"] = attempt.InputQueuesAttached,
                ["attachError"] = attempt.AttachErrorCode,
                ["setForegroundSucceeded"] = attempt.SetForegroundSucceeded
            });
        return new { restored = attempt.IsForeground, foregroundHwnd = attempt.ForegroundWindowHex, reason = attempt.IsForeground ? "restored" : "activation-rejected" };
    }

    private async Task<BackendUiState> SetQueryAsync(string query)
    {
        _session!.SetQuery(query);
        ClearError();
        _launchScheduler!.CancelPending();
        var match = _session.ResolveQuery();
        if (match.Kind == AliasMatchKind.Exact && match.Binding is not null)
        {
            var previousHwnd = _session.Snapshot.PreviousForegroundHwnd;
            _ = ObserveScheduledActivationAsync(_launchScheduler.ScheduleAsync(match.Binding, previousHwnd));
        }
        await PublishStateAsync().ConfigureAwait(false);
        return GetState();
    }

    private async Task<BackendUiState> SaveAliasAsync(SaveAliasParams parameters)
    {
        var state = _session!.Snapshot;
        var candidate = state.AliasEditCandidate;
        if (candidate is null || !string.Equals(candidate.Id, parameters.BindingId, StringComparison.Ordinal))
            throw new RpcProtocolException(-32006, "The alias editor is no longer open for this application.");
        var existingAliases = state.Bindings.Where(binding => !string.Equals(binding.Id, candidate.Id, StringComparison.Ordinal)).Select(binding => binding.Alias);
        var validationError = AliasValidator.Validate(parameters.Alias, existingAliases);
        if (validationError is not null)
            throw new RpcProtocolException(-32007, $"Alias is invalid: {validationError}.", new { validationError });

        var updated = Clone(candidate);
        updated.Alias = parameters.Alias.Trim();
        var bindings = state.Bindings.Where(binding => !string.Equals(binding.Id, updated.Id, StringComparison.Ordinal))
            .Select(Clone).Append(updated).ToList();
        var settings = _settings! with { Bindings = bindings };
        _settingsStore!.Save(settings);
        _settings = settings;
        if (_session.SaveAlias(parameters.Alias) is not null)
            throw new InvalidOperationException("Alias validation changed while saving.");
        await ActivateCurrentServicesAsync().ConfigureAwait(false);
        ClearError();
        await PublishStateAsync().ConfigureAwait(false);
        return GetState();
    }

    private async Task<BackendUiState> RemoveBindingAsync(RemoveBindingParams parameters)
    {
        var state = _session!.Snapshot;
        if (!state.Bindings.Any(binding => string.Equals(binding.Id, parameters.BindingId, StringComparison.Ordinal)))
            throw new RpcProtocolException(-32004, "The selected binding no longer exists.", new { bindingId = parameters.BindingId });

        _launchScheduler!.CancelPending();
        var settings = _settings! with
        {
            Bindings = state.Bindings.Where(binding => !string.Equals(binding.Id, parameters.BindingId, StringComparison.Ordinal))
                .Select(Clone).ToList()
        };
        // Persist before changing live bindings so a failed write keeps the launcher intact.
        _settingsStore!.Save(settings);
        _settings = settings;
        _session.RemoveBinding(parameters.BindingId);
        await ActivateCurrentServicesAsync().ConfigureAwait(false);
        ClearError();
        await PublishStateAsync().ConfigureAwait(false);
        return GetState();
    }

    private async Task<BackendUiState> UpdateSettingsAsync(SettingsUpdateParams parameters)
    {
        var settings = new LauncherSettings
        {
            Hotkey = parameters.Hotkey,
            Theme = parameters.Theme == "Dark" ? AppTheme.Dark : AppTheme.Light,
            StartWithWindows = parameters.StartWithWindows,
            Bindings = parameters.Bindings.Select(binding => new AppBinding(
                binding.Id, binding.Name, binding.TargetPath, binding.Alias, binding.Arguments)).ToList(),
            ModuleSettings = _settings!.ModuleSettings.Select(entry => entry with { Value = entry.Value.Clone() }).ToList()
        };
        _ = SettingsSerializer.Serialize(settings); // Full schema validation before any persistent or hook mutation.
        _settingsStore!.Save(settings);
        if (!_testMode) _settingsStore.ApplyStartupSetting(settings.StartWithWindows);
        _settings = settings;
        _settingsValid = true;
        _settingsError = null;
        _keyboardHook!.SetOpener(settings.Hotkey);
        _session!.ReplaceBindings(settings.Bindings);
        _launchScheduler!.CancelPending();
        ClearError();
        await PublishStateAsync().ConfigureAwait(false);
        return GetState();
    }

    private const int MaximumBackupBytes = AutomationLibraryTransferService.MaximumImportBytes + 1024 * 1024;
    private const int MaximumImportWarningDetails = 250;

    private async Task<object> ImportSettingsAsync(string path)
    {
        if (!Path.IsPathFullyQualified(path)) throw new InvalidDataException("Import path must be absolute.");
        var file = new FileInfo(path);
        if (!file.Exists) throw new FileNotFoundException("The selected backup could not be found.");
        if (file.Length is <= 0 or > MaximumBackupBytes)
            throw new InvalidDataException("The selected backup is empty or exceeds the supported size.");

        AutomationLibraryImportResult? libraryResult = null;
        var text = File.ReadAllText(path, Encoding.UTF8);
        using (var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 64 }))
        {
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("format", out var format)
                && format.ValueKind == JsonValueKind.String
                && string.Equals(format.GetString(), "automator-backup", StringComparison.Ordinal))
            {
                if (!root.TryGetProperty("formatVersion", out var version) || !version.TryGetInt32(out var versionValue)
                    || versionValue != 1
                    || !root.TryGetProperty("settings", out var settingsElement) || settingsElement.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("automationLibrary", out var libraryElement) || libraryElement.ValueKind != JsonValueKind.Object)
                    throw new InvalidDataException("The Automator backup format or version is unsupported.");

                // Validate settings before the library import performs any writes.
                var importedSettings = SettingsSerializer.Deserialize(settingsElement.GetRawText());
                var libraryPayload = Encoding.UTF8.GetBytes(libraryElement.GetRawText());
                libraryResult = await _libraryTransferService!.ImportAsync(libraryPayload, _lifetime.Token).ConfigureAwait(false);
                _settings = _settingsStore!.Import(importedSettings);
            }
            else
            {
                // Continue to accept the earlier settings-only JSON export format.
                _settings = _settingsStore!.Import(path);
            }
        }

        // Imported workflows can contain legacy variables. Migrate them only after the library write succeeds.
        _ = await _variableService!.GetAsync(_lifetime.Token).ConfigureAwait(false);
        await PublishGlobalVariablesChangedSafelyAsync().ConfigureAwait(false);

        var settings = _settings!;
        if (!_testMode) _settingsStore!.ApplyStartupSetting(settings.StartWithWindows);
        _settingsValid = true;
        _settingsError = null;
        _keyboardHook!.SetOpener(settings.Hotkey);
        _session!.ReplaceBindings(settings.Bindings);
        _launchScheduler!.CancelPending();
        ClearError();
        await PublishStateAsync().ConfigureAwait(false);
        var warningCount = libraryResult?.Warnings.Count ?? 0;
        _log!.Write(ApplicationLogLevel.Information, "Backup.Imported", "An Automator backup was imported.",
            properties: new Dictionary<string, object?>
            {
                ["libraryRecordCount"] = libraryResult?.ImportedCount ?? 0,
                ["repairWarningCount"] = warningCount,
            });
        return new
        {
            state = GetState(),
            includesAutomationLibrary = libraryResult is not null,
            importedLibraryRecords = libraryResult?.ImportedCount ?? 0,
            warningCount,
            warnings = libraryResult?.Warnings.Take(MaximumImportWarningDetails).ToArray() ?? [],
            warningsTruncated = warningCount > MaximumImportWarningDetails,
        };
    }

    private async Task<object> ExportBackupAsync(string path)
    {
        if (!Path.IsPathFullyQualified(path)) throw new InvalidDataException("Export path must be absolute.");
        // Ensure legacy workflow variables are represented in the exported shared library.
        _ = await _variableService!.GetAsync(_lifetime.Token).ConfigureAwait(false);
        var libraryPayload = await _libraryTransferService!.ExportAsync(_lifetime.Token).ConfigureAwait(false);
        using var settingsDocument = JsonDocument.Parse(SettingsSerializer.Serialize(_settings!));
        using var libraryDocument = JsonDocument.Parse(libraryPayload);
        using var output = new MemoryStream();
        using (var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("format", "automator-backup");
            writer.WriteNumber("formatVersion", 1);
            writer.WritePropertyName("settings");
            settingsDocument.RootElement.WriteTo(writer);
            writer.WritePropertyName("automationLibrary");
            libraryDocument.RootElement.WriteTo(writer);
            writer.WriteEndObject();
        }

        if (output.Length > MaximumBackupBytes)
            throw new InvalidDataException("The Automator backup exceeds the supported size.");
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporaryPath = fullPath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            await File.WriteAllBytesAsync(temporaryPath, output.ToArray(), _lifetime.Token).ConfigureAwait(false);
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }

        _log!.Write(ApplicationLogLevel.Information, "Backup.Exported", "An Automator backup was exported.",
            properties: new Dictionary<string, object?>
            {
                ["libraryRecordCount"] = libraryDocument.RootElement.GetProperty("records").GetArrayLength(),
            });
        return new { exported = true };
    }

    private async Task<BackendUiState> RelinkAsync(RelinkParams parameters)
    {
        var existing = _session!.Snapshot.Bindings.FirstOrDefault(binding => string.Equals(binding.Id, parameters.BindingId, StringComparison.Ordinal))
            ?? throw new RpcProtocolException(-32004, "The selected binding no longer exists.");
        var resolved = _catalog!.FromCustomPath(parameters.Path);
        var replacement = new AppBinding(existing.Id, existing.Name, resolved.TargetPath, existing.Alias, resolved.Arguments);
        var bindings = _session.Snapshot.Bindings.Select(binding => binding.Id == existing.Id ? replacement : Clone(binding)).ToList();
        var settings = _settings! with { Bindings = bindings };
        _settingsStore!.Save(settings);
        _settings = settings;
        _settingsValid = true;
        _settingsError = null;
        _session.ReplaceBindings(bindings);
        ClearError();
        await PublishStateAsync().ConfigureAwait(false);
        return GetState();
    }

    private async Task BeginCatalogLoadingAsync()
    {
        _launchScheduler!.CancelPending();
        if (!_session!.BeginCatalogLoading()) return;
        await ActivateCurrentServicesAsync().ConfigureAwait(false);
        var generation = ++_catalogGeneration;
        _catalogLoading = true;
        ClearError();
        await PublishStateAsync().ConfigureAwait(false);
        _ = LoadCatalogAsync(generation);
    }

    private async Task LoadCatalogAsync(long generation)
    {
        IReadOnlyList<AppBinding>? apps = null;
        Exception? failure = null;
        try { apps = await _catalog!.DiscoverAsync(_lifetime.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { return; }
        catch (Exception exception) { failure = exception; }

        try
        {
            await _commands.EnqueueAsync<bool>(async () =>
            {
                if (generation != _catalogGeneration || _session!.Snapshot.Mode != LauncherMode.Catalog) return true;
                _catalogLoading = false;
                if (failure is null)
                {
                    _session.CompleteCatalogLoading(apps!);
                    ClearError();
                }
                else
                {
                    _log!.Write(ApplicationLogLevel.Error, "Catalog.DiscoveryFailed", "The application catalog could not be loaded.", failure);
                    _error = "Could not load installed applications. You can still add an app by browsing to its executable or shortcut.";
                }
                await PublishStateAsync().ConfigureAwait(false);
                return true;
            }, _lifetime.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (ObjectDisposedException) { }
    }

    private async Task OpenPanelAsync(bool captureUnderCursor)
    {
        var current = _session!.Snapshot;
        if (current.Visible) return;
        _keyboardHook?.CancelRecording();
        _activationEpoch.PanelChanged();
        _launchScheduler!.CancelPending();
        _modeCoordinator!.SetMode(LauncherMode.Launcher);
        InvalidateCatalogLoad();
        // Capture the pre-panel foreground and pointer target before asking Electron to show the window.
        var previous = WindowsNativeMethodsFacade.GetForegroundWindowHex();
        var captured = captureUnderCursor ? _catalog!.CaptureUnderCursor() : null;
        _session.ShowPanel(previous, captured);
        Volatile.Write(ref _windowContext, _windowContext with { Visible = false, RendererFocused = false, NativeDialogActive = false });
        await ActivateCurrentServicesAsync().ConfigureAwait(false);
        _busy = false;
        ClearError();
        _log!.Write(ApplicationLogLevel.Information, "Launcher.Opened", "Opened launcher state with the prior foreground context.",
            properties: new Dictionary<string, object?> { ["previousForegroundHwnd"] = previous, ["capturedApp"] = captured?.Name });
        await PublishStateAsync().ConfigureAwait(false);
    }

    private async Task HidePanelAsync()
    {
        _activationEpoch.PanelChanged();
        _keyboardHook?.CancelRecording();
        _launchScheduler?.CancelPending();
        _modeCoordinator?.SetMode(LauncherMode.Launcher);
        _session?.HidePanel();
        Volatile.Write(ref _windowContext, _windowContext with { Visible = false, RendererFocused = false, NativeDialogActive = false });
        await DeactivateServicesAsync().ConfigureAwait(false);
        InvalidateCatalogLoad();
        _busy = false;
        _error = null;
        await PublishStateAsync().ConfigureAwait(false);
    }

    private async Task SetModeAsync(LauncherMode mode)
    {
        var changed = _modeCoordinator!.SetMode(mode);
        if (mode != LauncherMode.RecordingHotkey) _keyboardHook!.CancelRecording();
        if (mode != LauncherMode.Catalog) InvalidateCatalogLoad();
        if (changed)
        {
            Volatile.Write(ref _windowContext, _windowContext with { RendererFocused = false });
            await ActivateCurrentServicesAsync().ConfigureAwait(false);
            await PublishStateAsync().ConfigureAwait(false);
        }
    }

    private void InvalidateCatalogLoad()
    {
        _catalogGeneration++;
        _catalogLoading = false;
    }

    private async Task ObserveScheduledActivationAsync(Task activation)
    {
        try { await activation.ConfigureAwait(false); }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception exception)
        {
            _log!.Write(ApplicationLogLevel.Error, "Launcher.AliasActivationFailed", "Automatic alias launch failed.", exception);
        }
    }

    /// <summary>Must be called on the serialized command reader.</summary>
    private async Task<Task> StartTrackedActivationAsync(AppBinding binding, string? previousForeground)
    {
        var ticket = _activationEpoch.BeginActivation();
        _launchScheduler!.CancelPending();
        InvalidateCatalogLoad();
        _session!.HidePanel();
        Volatile.Write(ref _windowContext, _windowContext with { Visible = false, RendererFocused = false, NativeDialogActive = false });
        await DeactivateServicesAsync().ConfigureAwait(false);
        _busy = true;
        _error = null;
        await PublishStateAsync().ConfigureAwait(false);

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = Task.Run(() => RunPlatformActivationAsync(binding, previousForeground, ticket, completion));
        return completion.Task;
    }

    private async Task RunPlatformActivationAsync(
        AppBinding binding,
        string? previousForeground,
        ActivationTicket ticket,
        TaskCompletionSource completion)
    {
        Exception? failure = null;
        try { await _platformActivator!.ActivateAsync(binding, previousForeground, _lifetime.Token).ConfigureAwait(false); }
        catch (Exception exception) { failure = exception; }

        try
        {
            await _commands.EnqueueAsync<bool>(async () =>
            {
                if (_activationEpoch.IsCurrent(ticket))
                {
                    _busy = false;
                    if (failure is null)
                    {
                        _error = null;
                    }
                    else if (!_lifetime.IsCancellationRequested)
                    {
                        _session!.ShowPanel(previousForeground);
                        Volatile.Write(ref _windowContext, _windowContext with { Visible = false, RendererFocused = false, NativeDialogActive = false });
                        _error = UserFacingError(failure);
                        _log!.Write(ApplicationLogLevel.Error, "Launcher.ActivationFailed", "Launching or restoring an app failed.", failure,
                            new Dictionary<string, object?> { ["bindingId"] = binding.Id, ["targetPath"] = binding.TargetPath });
                        _activationEpoch.PanelChanged();
                        await ActivateCurrentServicesAsync().ConfigureAwait(false);
                    }
                    await PublishStateAsync().ConfigureAwait(false);
                }

                if (failure is null) completion.TrySetResult();
                else completion.TrySetException(failure);
                return true;
            }, _lifetime.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException)
        {
            if (failure is not null) completion.TrySetException(failure);
            else completion.TrySetCanceled(_lifetime.Token);
        }
    }

    private Task BeginActivationFromSchedulerAsync(AppBinding binding, string? previousForeground, CancellationToken cancellationToken) =>
        AwaitActivationAsync(binding, previousForeground, cancellationToken);

    private async Task AwaitActivationAsync(AppBinding binding, string? previousForeground, CancellationToken cancellationToken)
    {
        var activation = await _commands.EnqueueAsync<Task>(
            async () =>
            {
                if (!_session!.Snapshot.Bindings.Any(current => string.Equals(current.Id, binding.Id, StringComparison.Ordinal)))
                    return Task.CompletedTask;
                return await StartTrackedActivationAsync(binding, previousForeground).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        await activation.ConfigureAwait(false);
    }

    private async Task ObserveActivationAsync(Task activation, string bindingId)
    {
        try { await activation.ConfigureAwait(false); }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception exception)
        {
            _log?.Write(ApplicationLogLevel.Error, "Launcher.ExplicitActivationFailed", "An explicit app activation failed.", exception,
                new Dictionary<string, object?> { ["bindingId"] = bindingId });
        }
    }

    private void OnToggleRequested(object? sender, EventArgs args) => _commands.TryPost(async () =>
    {
        if (_session?.Snapshot.Visible == true) await HidePanelAsync().ConfigureAwait(false);
        else await OpenPanelAsync(captureUnderCursor: true).ConfigureAwait(false);
    });

    private void OnNavigationRequested(object? sender, NavigationKeyRequested request) => _commands.TryPost(async () =>
    {
        var context = Volatile.Read(ref _windowContext);
        var modeMatches = string.Equals(context.Mode, ToWireMode(_session!.Snapshot.Mode), StringComparison.Ordinal);
        var nativeForeground = modeMatches && !context.NativeDialogActive && context.Visible
            && context.Handle != IntPtr.Zero && WindowsForegroundWindow.CurrentHandle == context.Handle;
        var action = NavigationKeyPolicy.Decide(_session.NavigationState, nativeForeground,
            context.RendererFocused && modeMatches, hasModifier: false, request.VirtualKey);
        switch (action)
        {
            case NavigationKeyAction.Close:
                await HidePanelAsync().ConfigureAwait(false);
                break;
            case NavigationKeyAction.SelectTab:
            {
                var digit = request.VirtualKey is >= 0x61 and <= 0x69
                    ? (char)('1' + request.VirtualKey - 0x61)
                    : (char)request.VirtualKey;
                _launchScheduler!.CancelPending();
                InvalidateCatalogLoad();
                if (_session.SelectTabFromGlobalKey(digit, launcherNativeForeground: nativeForeground))
                {
                    await ActivateCurrentServicesAsync().ConfigureAwait(false);
                    await PublishStateAsync().ConfigureAwait(false);
                }
                break;
            }
            case NavigationKeyAction.OpenCatalog:
                await BeginCatalogLoadingAsync().ConfigureAwait(false);
                break;
        }
    });

    private void OnKeyRecorded(object? sender, string key) => _commands.TryPost(async () =>
    {
        var snapshot = _session!.NavigationState;
        var context = Volatile.Read(ref _windowContext);
        var nativeForeground = context.Handle != IntPtr.Zero && WindowsForegroundWindow.CurrentHandle == context.Handle;
        var modeMatches = string.Equals(context.Mode, ToWireMode(snapshot.Mode), StringComparison.Ordinal);
        if (!HotkeyRecordingPolicy.ShouldCapture(snapshot, context.Visible, context.RendererFocused,
                context.NativeDialogActive, nativeForeground, modeMatches))
        {
            _log!.Write(ApplicationLogLevel.Information, "Keyboard.StaleRecordingIgnored",
                "Ignored an opener key reported after its focused recording context ended.");
            return;
        }

        _modeCoordinator!.SetMode(LauncherMode.Settings);
        await ActivateCurrentServicesAsync().ConfigureAwait(false);
        InvalidateCatalogLoad();
        await SendNotificationAsync("hotkey/recorded", new { key }).ConfigureAwait(false);
        await PublishStateAsync().ConfigureAwait(false);
    });

    private void OpenLogFolder()
    {
        var folder = Path.GetDirectoryName(_log!.LogPath)!;
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
    }

    private async Task PublishStateAsync()
    {
        if (!_initialized && _session is null) return;
        RefreshKeyboardDeliveryContext();
        _stateRevision = checked(_stateRevision + 1);
        var snapshot = GetState();
        await SendNotificationAsync("stateChanged", snapshot).ConfigureAwait(false);
    }

    private Task SendNotificationAsync(string method, object parameters) =>
        WriteProtocolAsync(new BackendNotification("2.0", method, parameters));

    private async Task WriteProtocolAsync(object value)
    {
        var line = RpcProtocol.Serialize(value);
        await _stdoutLock.WaitAsync(_lifetime.Token).ConfigureAwait(false);
        try
        {
            await Console.Out.WriteLineAsync(line).ConfigureAwait(false);
            await Console.Out.FlushAsync(_lifetime.Token).ConfigureAwait(false);
        }
        finally { _stdoutLock.Release(); }
    }

    private static string UserFacingError(Exception exception) => exception switch
    {
        FileNotFoundException => "The selected application or settings file could not be found.",
        UnauthorizedAccessException => "Windows denied access to the selected file or application.",
        TimeoutException => exception.Message,
        InvalidDataException => exception.Message,
        InvalidOperationException => exception.Message,
        System.ComponentModel.Win32Exception => $"Windows could not complete the action: {exception.Message}",
        _ => $"The requested action failed: {exception.Message}"
    };

    private static string ToWireMode(LauncherMode mode) => mode switch
    {
        LauncherMode.RecordingHotkey => "recordingHotkey",
        LauncherMode.AliasEditing => "aliasEditing",
        _ => mode.ToString().ToLowerInvariant()
    };

    private static string ToWireCategory(AutomationServiceErrorCategory category) => category switch
    {
        AutomationServiceErrorCategory.UnsupportedScheme => "unsupportedScheme",
        AutomationServiceErrorCategory.HostNotAllowed => "hostNotAllowed",
        AutomationServiceErrorCategory.NetworkNotAllowed => "networkNotAllowed",
        AutomationServiceErrorCategory.RequestTooLarge => "requestTooLarge",
        AutomationServiceErrorCategory.ResponseTooLarge => "responseTooLarge",
        AutomationServiceErrorCategory.TimedOut => "timedOut",
        AutomationServiceErrorCategory.Canceled => "canceled",
        AutomationServiceErrorCategory.TransportFailure => "transportFailure",
        _ => "invalidResponse",
    };

    private static LauncherMode ParseMode(string mode) => mode switch
    {
        "launcher" => LauncherMode.Launcher,
        "catalog" => LauncherMode.Catalog,
        "settings" => LauncherMode.Settings,
        "recordingHotkey" => LauncherMode.RecordingHotkey,
        "aliasEditing" => LauncherMode.AliasEditing,
        _ => throw new RpcProtocolException(-32602, "Launcher mode is invalid.")
    };

    private void ClearError() => _error = null;

    private async Task MonitorHostProcessAsync(Process hostProcess)
    {
        try
        {
            await hostProcess.WaitForExitAsync(_lifetime.Token).ConfigureAwait(false);
            if (!_lifetime.IsCancellationRequested)
            {
                _log?.Write(ApplicationLogLevel.Warning, "Backend.HostExited", "The desktop host exited; the backend is shutting down its hooks.");
                _lifetime.Cancel();
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception exception)
        {
            _log?.Write(ApplicationLogLevel.Error, "Backend.HostMonitorFailed", "Could not monitor the desktop host process.", exception);
            if (!_lifetime.IsCancellationRequested) _lifetime.Cancel();
        }
    }

    private static AppBinding Clone(AppBinding binding) => new(binding.Id, binding.Name, binding.TargetPath, binding.Alias, binding.Arguments);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0) return;
        _lifetime.Cancel();
        _automationRequests.CancelAll();
        if (_schedulerCoordinator is not null)
        {
            try { await _schedulerCoordinator.DisposeAsync().ConfigureAwait(false); }
            catch (Exception) { /* Shutdown continues after host-owned schedules are canceled. */ }
        }
        if (_workTimeCoordinator is not null)
        {
            try { await _workTimeCoordinator.DisposeAsync().ConfigureAwait(false); }
            catch (Exception) { /* Shutdown continues after the work-time service is stopped. */ }
        }
        _keyboardEventHub?.SetDeliveryContext(null);
        _launchScheduler?.Dispose();
        if (_keyboardHook is not null)
        {
            _keyboardHook.ToggleRequested -= OnToggleRequested;
            _keyboardHook.NavigationRequested -= OnNavigationRequested;
            _keyboardHook.KeyRecorded -= OnKeyRecorded;
            _keyboardHook.InputReceived -= OnAutomationInputReceived;
            _keyboardHook.Dispose();
        }
        _foregroundMonitor?.Dispose();
        await DeactivateServicesAsync().ConfigureAwait(false);
        await _commands.DisposeAsync().ConfigureAwait(false);
        try { await Task.WhenAll(_serviceRequestTasks.Values.ToArray()).ConfigureAwait(false); }
        catch (Exception) { /* Request observers log failures; shutdown continues after cancellation. */ }
        if (_browserAutomationService is not null)
        {
            try { await _browserAutomationService.DisposeAsync().ConfigureAwait(false); }
            catch (Exception exception)
            {
                _log?.Write(ApplicationLogLevel.Warning, "Browser.ShutdownFailed", "The browser runtime did not shut down cleanly.", exception);
            }
        }
        _sharedHttpService?.Dispose();
        if (_hostMonitorTask is not null)
        {
            try { await _hostMonitorTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }
        _hostProcess?.Dispose();
        await _stdoutLock.WaitAsync().ConfigureAwait(false);
        try
        {
            _log?.Write(ApplicationLogLevel.Information, "Backend.Stopped", "The backend is cleaning up native hooks and request state.");
            _log?.Dispose();
        }
        finally
        {
            _stdoutLock.Release();
            _stdoutLock.Dispose();
            _lifetime.Dispose();
        }
    }

    private sealed class TrackingActivator(BackendServer owner) : IApplicationActivator
    {
        public Task ActivateAsync(AppBinding binding, string? previousForegroundHwnd, CancellationToken cancellationToken) =>
            owner.BeginActivationFromSchedulerAsync(binding, previousForegroundHwnd, cancellationToken);
    }

    private sealed class TaskLaunchDelay : ILaunchDelay
    {
        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) => Task.Delay(delay, cancellationToken);
    }

    private sealed class BackendKeyboardInput(AutomationKeyboardEventHub hub, string moduleId) : IAutomationKeyboardInput
    {
        public ValueTask<IAsyncDisposable> SubscribeAsync(Func<KeyInputEvent, ValueTask> receiver, CancellationToken cancellationToken) =>
            hub.SubscribeAsync(moduleId, receiver, cancellationToken);
    }

    private sealed class BackendAutomationNotificationService(BackendServer owner) : IAutomationNotificationService
    {
        public Task ShowAsync(AutomationNotification notification, CancellationToken cancellationToken) =>
            owner.PublishAutomationNotificationAsync(notification, cancellationToken);
    }

    private Task PublishAutomationNotificationAsync(AutomationNotification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(notification.Title) || string.IsNullOrWhiteSpace(notification.Body))
            throw new InvalidDataException("The automation notification is invalid.");
        var title = notification.Title.Trim();
        var body = notification.Body.Trim();
        if (title.Length > 128 || body.Length > 256
            || title.Any(char.IsControl) || body.Any(char.IsControl))
            throw new InvalidDataException("The automation notification is invalid.");
        if (_testMode) return Task.CompletedTask;
        return SendNotificationAsync(RpcMethods.AutomationNotification,
            new AutomationNotification(title, body));
    }
}

internal static class WindowsNativeMethodsFacade
{
    public static string? GetForegroundWindowHex()
    {
        return WindowsForegroundWindow.CurrentHandleHex;
    }
}
