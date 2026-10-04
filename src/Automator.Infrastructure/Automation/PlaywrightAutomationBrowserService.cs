using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Automator.Application.Automation;
using Automator.Application.Logging;
using Automator.Core.Automation;

namespace Automator.Infrastructure.Automation;

/// <summary>Runs one persistent Playwright worker per saved profile and confines Chromium traffic to its policy proxy.</summary>
public sealed class PlaywrightAutomationBrowserService : IAutomationBrowserService, IAsyncDisposable
{
    private const int MaximumWorkerMessageBytes = 512 * 1024;
    private static readonly Regex ProfileIdPattern = new("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
    private readonly string _nodeExecutablePath;
    private readonly string _workerScriptPath;
    private readonly string _playwrightModuleRoot;
    private readonly string _dataRoot;
    private readonly string _browserCachePath;
    private readonly IAutomationLibraryStore _library;
    private readonly IHostAddressResolver _resolver;
    private readonly IApplicationLog _log;
    private readonly ConcurrentDictionary<string, BrowserSession> _sessions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _profileLocks = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _setupLock = new(1, 1);
    private readonly SemaphoreSlim _sessionSlots = new(4, 4);
    private int _setupRunning;
    private int _disposed;

    public PlaywrightAutomationBrowserService(string nodeExecutablePath, string workerScriptPath, string playwrightModuleRoot,
        string dataRoot, IAutomationLibraryStore library, IHostAddressResolver resolver, IApplicationLog log)
    {
        _nodeExecutablePath = RequirePath(nodeExecutablePath, nameof(nodeExecutablePath));
        _workerScriptPath = RequirePath(workerScriptPath, nameof(workerScriptPath));
        _playwrightModuleRoot = RequirePath(playwrightModuleRoot, nameof(playwrightModuleRoot));
        _dataRoot = RequirePath(dataRoot, nameof(dataRoot));
        _browserCachePath = Path.Combine(_dataRoot, "browser-runtime", "playwright-browsers");
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public async Task<AutomationBrowserRuntimeStatus> GetRuntimeStatusAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        if (Interlocked.CompareExchange(ref _setupRunning, 0, 0) != 0)
            return new AutomationBrowserRuntimeStatus(false, false, "The browser is being prepared.", 50);
        try
        {
            var status = await ReadWorkerStatusAsync(cancellationToken).ConfigureAwait(false);
            return new AutomationBrowserRuntimeStatus(status.Installed, status.Ready,
                status.Ready ? "Browser runtime is ready." : "The browser will be installed automatically the first time you run an action.");
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            return new AutomationBrowserRuntimeStatus(false, false,
                "The browser runtime is unavailable. Check the host browser runtime files.");
        }
    }

    public async Task<AutomationBrowserActionResult> ExecuteAsync(string profileId, AutomationBrowserAction action,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        ValidateProfileId(profileId);
        ArgumentNullException.ThrowIfNull(action);
        var profileGate = _profileLocks.GetOrAdd(profileId, static _ => new SemaphoreSlim(1, 1));
        await profileGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var profile = await RequireProfileAsync(profileId, cancellationToken).ConfigureAwait(false);
            ValidateAction(action, profile);
            var policyKey = CreatePolicyKey(profile);
            if (_sessions.TryGetValue(profileId, out var oldSession) &&
                (!string.Equals(oldSession.PolicyKey, policyKey, StringComparison.Ordinal) || oldSession.Process.HasExited))
            {
                _sessions.TryRemove(profileId, out _);
                await oldSession.DisposeAsync().ConfigureAwait(false);
            }

            if (!_sessions.TryGetValue(profileId, out var session))
            {
                await EnsureBrowserInstalledAsync(cancellationToken).ConfigureAwait(false);
                await _sessionSlots.WaitAsync(cancellationToken).ConfigureAwait(false);
                try { session = await StartSessionAsync(profile, policyKey, cancellationToken).ConfigureAwait(false); }
                catch
                {
                    _sessionSlots.Release();
                    throw;
                }
                _sessions[profileId] = session;
                _log.Write(ApplicationLogLevel.Information, "automation.browser.session.started",
                    "A persistent browser session was started.", properties: new Dictionary<string, object?> { ["profileId"] = profileId });
            }

            var requestId = Guid.NewGuid().ToString("N");
            var command = JsonSerializer.Serialize(new WorkerCommand(requestId, action), JsonOptions);
            if (Encoding.UTF8.GetByteCount(command) > MaximumWorkerMessageBytes)
                throw new InvalidDataException("Browser action is too large.");
            try
            {
                await session.Input.WriteLineAsync(command.AsMemory(), cancellationToken).ConfigureAwait(false);
                await session.Input.FlushAsync(cancellationToken).ConfigureAwait(false);
                using var operationTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                operationTimeout.CancelAfter(TimeSpan.FromMilliseconds(action.TimeoutMilliseconds + 5000));
                var line = await session.Output.ReadLineAsync(operationTimeout.Token).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("The browser worker stopped unexpectedly.");
                if (Encoding.UTF8.GetByteCount(line) > MaximumWorkerMessageBytes)
                    throw new InvalidDataException("Browser worker output is too large.");
                var response = JsonSerializer.Deserialize<WorkerResponse>(line, JsonOptions)
                    ?? throw new InvalidDataException("Browser worker response is invalid.");
                if (!string.Equals(response.Id, requestId, StringComparison.Ordinal) || !response.Ok || response.Result is null)
                    throw new InvalidOperationException("The browser action failed. Check the profile network policy and browser runtime.");
                return response.Result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _sessions.TryRemove(profileId, out _);
                await session.DisposeAsync().ConfigureAwait(false);
                throw;
            }
            catch (OperationCanceledException)
            {
                _sessions.TryRemove(profileId, out _);
                await session.DisposeAsync().ConfigureAwait(false);
                throw new InvalidOperationException("The browser action timed out.");
            }
            catch (TimeoutException)
            {
                _sessions.TryRemove(profileId, out _);
                await session.DisposeAsync().ConfigureAwait(false);
                throw new InvalidOperationException("The browser action timed out.");
            }
            catch (Exception exception) when (exception is IOException or JsonException or InvalidDataException)
            {
                _sessions.TryRemove(profileId, out _);
                await session.DisposeAsync().ConfigureAwait(false);
                _log.Write(ApplicationLogLevel.Warning, "automation.browser.action.failed", "A browser action failed.",
                    properties: new Dictionary<string, object?> { ["profileId"] = profileId });
                throw new InvalidOperationException("The browser action failed. Check the profile network policy and browser runtime.");
            }
        }
        finally
        {
            profileGate.Release();
        }
    }

    public async Task CloseSessionAsync(string profileId, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        ValidateProfileId(profileId);
        var profileGate = _profileLocks.GetOrAdd(profileId, static _ => new SemaphoreSlim(1, 1));
        await profileGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_sessions.TryRemove(profileId, out var session))
            {
                await session.DisposeAsync().ConfigureAwait(false);
                _log.Write(ApplicationLogLevel.Information, "automation.browser.session.closed",
                    "A persistent browser session was closed.", properties: new Dictionary<string, object?> { ["profileId"] = profileId });
            }
        }
        finally { profileGate.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        var sessions = _sessions.ToArray();
        _sessions.Clear();
        foreach (var (_, session) in sessions)
        {
            try { await session.DisposeAsync().ConfigureAwait(false); }
            catch (Exception exception) when (exception is IOException or InvalidOperationException or System.ComponentModel.Win32Exception) { }
        }
    }

    private async Task<BrowserAutomationProfile> RequireProfileAsync(string id, CancellationToken cancellationToken)
    {
        var record = await _library.GetAsync(BrowserAutomationModule.IdValue, BrowserAutomationModule.ProfileCollection, id, cancellationToken)
            .ConfigureAwait(false);
        var profile = record is null ? null : BrowserAutomationModule.TryReadProfile(record);
        return profile is not null && string.Equals(profile.Id, id, StringComparison.Ordinal)
            ? profile
            : throw new InvalidOperationException("The selected browser profile does not exist or is invalid.");
    }

    private async Task EnsureBrowserInstalledAsync(CancellationToken cancellationToken)
    {
        await _setupLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var status = await ReadWorkerStatusAsync(cancellationToken).ConfigureAwait(false);
            if (status.Ready) return;
            ValidateRuntimeFiles();
            Interlocked.Exchange(ref _setupRunning, 1);
            try
            {
                Directory.CreateDirectory(_browserCachePath);
                var cliPath = Path.Combine(_playwrightModuleRoot, "cli.js");
                using var process = StartNode([cliPath, "install", "chromium"], CreateBaseEnvironment());
                var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
                var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
                try { await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false); }
                catch (OperationCanceledException)
                {
                    TryKill(process);
                    throw;
                }
                await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
                if (process.ExitCode != 0 || !(await ReadWorkerStatusAsync(cancellationToken).ConfigureAwait(false)).Ready)
                    throw new InvalidOperationException("Playwright Chromium setup failed.");
            }
            finally { Interlocked.Exchange(ref _setupRunning, 0); }
        }
        finally { _setupLock.Release(); }
    }

    private async Task<WorkerStatus> ReadWorkerStatusAsync(CancellationToken cancellationToken)
    {
        ValidateRuntimeFiles();
        Directory.CreateDirectory(_browserCachePath);
        using var process = StartNode([_workerScriptPath, "--status"], CreateBaseEnvironment());
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            var line = await process.StandardOutput.ReadLineAsync(timeout.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The browser worker returned no status.");
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            if (process.ExitCode != 0 || Encoding.UTF8.GetByteCount(line) > 1024)
                throw new InvalidOperationException("The browser worker status is invalid.");
            return JsonSerializer.Deserialize<WorkerStatus>(line, JsonOptions)
                ?? throw new InvalidOperationException("The browser worker status is invalid.");
        }
        catch
        {
            TryKill(process);
            throw;
        }
    }

    private async Task<BrowserSession> StartSessionAsync(BrowserAutomationProfile profile, string policyKey,
        CancellationToken cancellationToken)
    {
        var username = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var proxy = new BrowserAutomationProxy(_resolver, profile.AllowedHosts, profile.AllowLocalNetwork, username, password);
        await proxy.StartAsync(cancellationToken).ConfigureAwait(false);
        Process? worker = null;
        try
        {
            var profileDirectory = Path.Combine(_dataRoot, "browser-profiles", profile.Id);
            Directory.CreateDirectory(profileDirectory);
            var environment = CreateBaseEnvironment();
            environment["AUTOMATOR_BROWSER_PROFILE_DIR"] = profileDirectory;
            environment["AUTOMATOR_BROWSER_PROXY_SERVER"] = proxy.ProxyServer;
            environment["AUTOMATOR_BROWSER_PROXY_USERNAME"] = proxy.Username;
            environment["AUTOMATOR_BROWSER_PROXY_PASSWORD"] = proxy.Password;
            worker = StartNode([_workerScriptPath], environment);
            var output = worker.StandardOutput;
            var errorDrain = DrainAsync(worker.StandardError);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(45));
            var line = await output.ReadLineAsync(timeout.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The browser worker failed to start.");
            using var document = JsonDocument.Parse(line);
            if (!document.RootElement.TryGetProperty("type", out var type) || type.GetString() != "ready")
            {
                TryKill(worker);
                throw new InvalidOperationException("The browser worker failed to start.");
            }
            var session = new BrowserSession(policyKey, proxy, worker, worker.StandardInput, output, errorDrain, _sessionSlots);
            session.SetPolicyWatcher(WatchProfilePolicyAsync(profile.Id, proxy, session.PolicyToken));
            return session;
        }
        catch
        {
            if (worker is not null)
            {
                TryKill(worker);
                worker.Dispose();
            }
            await proxy.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private Dictionary<string, string> CreateBaseEnvironment()
    {
        Directory.CreateDirectory(_browserCachePath);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AUTOMATOR_PLAYWRIGHT_MODULE_ROOT"] = _playwrightModuleRoot,
            // The packaged runtime keeps Playwright and playwright-core in a `packages`
            // directory because electron-builder excludes development node_modules from resources.
            ["NODE_PATH"] = Path.GetDirectoryName(_playwrightModuleRoot) ?? _playwrightModuleRoot,
            ["PLAYWRIGHT_BROWSERS_PATH"] = _browserCachePath,
        };
    }

    private async Task WatchProfilePolicyAsync(string profileId, BrowserAutomationProxy proxy, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var record = await _library.GetAsync(BrowserAutomationModule.IdValue, BrowserAutomationModule.ProfileCollection,
                    profileId, cancellationToken).ConfigureAwait(false);
                var profile = record is null ? null : BrowserAutomationModule.TryReadProfile(record);
                if (profile is not null && string.Equals(profile.Id, profileId, StringComparison.Ordinal))
                    proxy.UpdatePolicy(profile.AllowedHosts, profile.AllowLocalNetwork);
                else
                    proxy.UpdatePolicy([], allowLocalNetwork: false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return; }
            catch
            {
                // A profile-store failure revokes network access until the host can re-read a valid grant.
                proxy.UpdatePolicy([], allowLocalNetwork: false);
            }
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        }
    }

    private void ValidateRuntimeFiles()
    {
        if (!File.Exists(_nodeExecutablePath) || !File.Exists(_workerScriptPath) ||
            !File.Exists(Path.Combine(_playwrightModuleRoot, "package.json")) ||
            !File.Exists(Path.Combine(_playwrightModuleRoot, "cli.js")))
            throw new FileNotFoundException("The host browser runtime files are unavailable.");
    }

    private Process StartNode(IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> environment)
    {
        var startInfo = new ProcessStartInfo(_nodeExecutablePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(_workerScriptPath) ?? _dataRoot,
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        foreach (var (key, value) in environment) startInfo.Environment[key] = value;
        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        if (!process.Start()) throw new InvalidOperationException("The browser worker could not be started.");
        return process;
    }

    private static async Task DrainAsync(StreamReader reader)
    {
        while (await reader.ReadLineAsync().ConfigureAwait(false) is not null) { }
    }

    private static string RequirePath(string path, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, parameterName);
        return Path.GetFullPath(path);
    }

    private static void ValidateProfileId(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId) || !ProfileIdPattern.IsMatch(profileId))
            throw new InvalidOperationException("The selected browser profile ID is invalid.");
    }

    private static void ValidateAction(AutomationBrowserAction action, BrowserAutomationProfile profile)
    {
        if (!Enum.IsDefined(action.Kind) || action.TimeoutMilliseconds is < 250 or > 30_000)
            throw new InvalidOperationException("The browser action is invalid.");
        if (action.Url is { Length: > 4096 } || action.Locator is { Length: > 2048 } || action.Value is { Length: > 32 * 1024 })
            throw new InvalidOperationException("The browser action is too large.");
        if (action.Kind == AutomationBrowserActionKind.Navigate && !IsAllowedWebUrl(action.Url, profile))
            throw new InvalidOperationException("The navigation URL is not allowed by this browser profile.");
        var needsLocator = action.Kind is AutomationBrowserActionKind.Click or AutomationBrowserActionKind.Fill or
            AutomationBrowserActionKind.Select or AutomationBrowserActionKind.WaitFor or AutomationBrowserActionKind.ReadText;
        if (needsLocator && (action.LocatorKind is null || !Enum.IsDefined(action.LocatorKind.Value) || string.IsNullOrWhiteSpace(action.Locator)))
            throw new InvalidOperationException("A supported browser locator is required.");
        if (action.Kind is AutomationBrowserActionKind.Fill or AutomationBrowserActionKind.Select && action.Value is null)
            throw new InvalidOperationException("A browser action value is required.");
    }

    private static bool IsAllowedWebUrl(string? value, BrowserAutomationProfile profile)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") ||
            !string.IsNullOrEmpty(uri.UserInfo) || uri.Port is < 1 or > 65535) return false;
        try
        {
            var host = NormalizeHost(uri.DnsSafeHost);
            return profile.AllowedHosts.Any(allowed => string.Equals(host, NormalizeHost(allowed), StringComparison.OrdinalIgnoreCase));
        }
        catch (ArgumentException) { return false; }
    }

    private static string CreatePolicyKey(BrowserAutomationProfile profile) => string.Join("\n",
        profile.AllowLocalNetwork ? "local" : "public", string.Join("\n", profile.AllowedHosts.Order(StringComparer.OrdinalIgnoreCase)));

    private static string NormalizeHost(string host)
    {
        var value = host.Trim().TrimEnd('.');
        if (IPAddress.TryParse(value, out var address)) return address.ToString().ToLowerInvariant();
        return new IdnMapping().GetAscii(value).ToLowerInvariant();
    }

    private static void TryKill(Process process)
    {
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException) { }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

    private sealed record WorkerCommand(string Id, AutomationBrowserAction Action);
    private sealed record WorkerStatus(bool Installed, bool Ready);
    private sealed record WorkerResponse(string Id, bool Ok, AutomationBrowserActionResult? Result, string? Error);

    private sealed class BrowserSession(string policyKey, BrowserAutomationProxy proxy, Process process, StreamWriter input,
        StreamReader output, Task errorDrain, SemaphoreSlim sessionSlots) : IAsyncDisposable
    {
        private readonly CancellationTokenSource _policyLifetime = new();
        private Task _policyWatcher = Task.CompletedTask;
        public string PolicyKey { get; } = policyKey;
        public BrowserAutomationProxy Proxy { get; } = proxy;
        public Process Process { get; } = process;
        public StreamWriter Input { get; } = input;
        public StreamReader Output { get; } = output;
        public CancellationToken PolicyToken => _policyLifetime.Token;

        public void SetPolicyWatcher(Task policyWatcher) => _policyWatcher = policyWatcher;

        public async ValueTask DisposeAsync()
        {
            _policyLifetime.Cancel();
            try { await _policyWatcher.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false); }
            catch (Exception exception) when (exception is OperationCanceledException or TimeoutException or IOException) { }
            try { Input.Close(); }
            catch (IOException) { }
            try
            {
                if (!Process.HasExited)
                    await Process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            }
            catch (TimeoutException) { TryKill(Process); }
            catch (InvalidOperationException) { }
            try { await errorDrain.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false); }
            catch (TimeoutException) { }
            try { Output.Dispose(); } catch (IOException) { }
            try { Process.Dispose(); } catch (InvalidOperationException) { }
            await Proxy.DisposeAsync().ConfigureAwait(false);
            _policyLifetime.Dispose();
            sessionSlots.Release();
        }
    }
}
