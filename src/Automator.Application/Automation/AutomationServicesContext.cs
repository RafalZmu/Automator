using Automator.Core.Automation;
using System.Text.Json;

namespace Automator.Application.Automation;

public sealed record AutomationHttpPolicy(IReadOnlyList<string> AllowedHosts, bool AllowLocalNetwork = false);

/// <summary>Host-owned module declaration. HTTP policy is deliberately not renderer state.</summary>
public sealed record AutomationModuleDescriptor(
    string ModuleId,
    IReadOnlyList<AutomationCapabilityRequirement> Capabilities,
    AutomationHttpPolicy? HttpPolicy = null);

public interface IAutomationKeyboardInput
{
    ValueTask<IAsyncDisposable> SubscribeAsync(Func<KeyInputEvent, ValueTask> receiver, CancellationToken cancellationToken);
}

public interface IAutomationHttpClient
{
    Task<AutomationHttpResult> SendAsync(AutomationHttpRequest request, CancellationToken cancellationToken);
}

public interface IAutomationProcessService
{
    Task<AutomationProcessResult> ExecuteAsync(AutomationProcessRequest request, CancellationToken cancellationToken);
}

/// <summary>Launches groups of HTTP(S) URLs through the host's default browser.</summary>
public interface IAutomationWebsiteLauncher
{
    Task LaunchAsync(IReadOnlyList<IReadOnlyList<Uri>> groups, CancellationToken cancellationToken);
}

/// <summary>Typed service grants scoped to one module activation.</summary>
public sealed class AutomationServicesContext : IAsyncDisposable
{
    private readonly CancellationTokenSource _lifetime = new();
    private readonly CancellationToken _lifetimeToken;
    private readonly object _subscriptionsGate = new();
    private readonly List<IAsyncDisposable> _subscriptions = [];
    private int _disposed;

    internal AutomationServicesContext(string moduleId, IAutomationKeyboardInput? keyboardInput, IAutomationHttpClient? http,
        IAutomationLibraryStore? libraryStore = null, IAutomationProcessService? processService = null,
        IAutomationSecretManager? secrets = null, IAutomationApiProfileRunner? apiProfiles = null,
        IAutomationBrowserService? browser = null, IAutomationWorkflowRunner? workflows = null,
        IAutomationSchedulerCoordinator? scheduler = null, IAutomationFocusSessionCoordinator? focusSessions = null,
        IAutomationWorkTimeCoordinator? workTime = null, IAutomationWebsiteLauncher? websiteLauncher = null,
        IAutomationCodexTaskService? codexTasks = null)
    {
        ModuleId = moduleId;
        _lifetimeToken = _lifetime.Token;
        KeyboardInput = keyboardInput is null ? null : new ContextKeyboardInput(this, keyboardInput);
        Http = http is null ? null : new ContextHttpClient(this, http);
        Library = libraryStore is null ? null : new ContextLibrary(this, libraryStore);
        Processes = processService is null ? null : new ContextProcessService(this, processService);
        Secrets = secrets is null ? null : new ContextSecretManager(this, secrets);
        ApiProfiles = apiProfiles is null ? null : new ContextApiProfileRunner(this, apiProfiles);
        Browser = browser is null ? null : new ContextBrowserService(this, browser);
        Workflows = workflows is null ? null : new ContextWorkflowRunner(this, workflows);
        Scheduler = scheduler is null ? null : new ContextSchedulerCoordinator(this, scheduler);
        FocusSessions = focusSessions is null ? null : new ContextFocusCoordinator(this, focusSessions);
        WorkTime = workTime is null ? null : new ContextWorkTimeCoordinator(this, workTime);
        WebsiteLauncher = websiteLauncher is null ? null : new ContextWebsiteLauncher(this, websiteLauncher);
        CodexTasks = moduleId == "codex" && codexTasks is not null ? new ContextCodexTaskService(this, codexTasks) : null;
    }

    public string ModuleId { get; }
    public IAutomationKeyboardInput? KeyboardInput { get; }
    public IAutomationHttpClient? Http { get; }
    public IAutomationLibrary? Library { get; }
    public IAutomationProcessService? Processes { get; }
    public IAutomationSecretManager? Secrets { get; }
    public IAutomationApiProfileRunner? ApiProfiles { get; }
    public IAutomationBrowserService? Browser { get; }
    public IAutomationWorkflowRunner? Workflows { get; }
    public IAutomationSchedulerCoordinator? Scheduler { get; }
    public IAutomationFocusSessionCoordinator? FocusSessions { get; }
    public IAutomationWorkTimeCoordinator? WorkTime { get; }
    public IAutomationWebsiteLauncher? WebsiteLauncher { get; }
    public IAutomationCodexTaskService? CodexTasks { get; }
    public CancellationToken LifetimeToken => _lifetimeToken;
    public bool HasCapability(string capabilityId) => capabilityId switch
    {
        AutomationCapabilityIds.KeyboardInput => KeyboardInput is not null,
        AutomationCapabilityIds.HttpRequest => Http is not null,
        AutomationCapabilityIds.LibraryStorage => Library is not null,
        AutomationCapabilityIds.ProcessExecution => Processes is not null,
        AutomationCapabilityIds.SecretManagement => Secrets is not null,
        AutomationCapabilityIds.ApiProfileHttp => ApiProfiles is not null,
        AutomationCapabilityIds.BrowserSession => Browser is not null,
        AutomationCapabilityIds.WorkflowExecution => Workflows is not null,
        AutomationCapabilityIds.SchedulerManagement => Scheduler is not null,
        AutomationCapabilityIds.FocusManagement => FocusSessions is not null,
        AutomationCapabilityIds.WorkTimeManagement => WorkTime is not null,
        AutomationCapabilityIds.WebsiteLaunch => WebsiteLauncher is not null,
        AutomationCapabilityIds.CodexTaskBuilder => CodexTasks is not null,
        _ => false,
    };

    private static CancellationTokenSource Link(AutomationServicesContext context, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref context._disposed) != 0, context);
        return CancellationTokenSource.CreateLinkedTokenSource(context.LifetimeToken, cancellationToken);
    }

    internal async ValueTask<IAsyncDisposable> TrackAsync(IAsyncDisposable subscription)
    {
        lock (_subscriptionsGate)
        {
            if (_disposed == 0)
            {
                _subscriptions.Add(subscription);
                return subscription;
            }
        }

        await subscription.DisposeAsync().ConfigureAwait(false);
        throw new ObjectDisposedException(nameof(AutomationServicesContext));
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _lifetime.Cancel();
        try
        {
            await UnsubscribeKeyboardAsync().ConfigureAwait(false);
        }
        finally
        {
            _lifetime.Dispose();
        }
    }

    public async ValueTask UnsubscribeKeyboardAsync()
    {
        IAsyncDisposable[] subscriptions;
        lock (_subscriptionsGate)
        {
            subscriptions = _subscriptions.ToArray();
            _subscriptions.Clear();
        }

        foreach (var subscription in subscriptions)
            await subscription.DisposeAsync().ConfigureAwait(false);
    }

    private sealed class ContextKeyboardInput(AutomationServicesContext context, IAutomationKeyboardInput inner) : IAutomationKeyboardInput
    {
        public async ValueTask<IAsyncDisposable> SubscribeAsync(Func<KeyInputEvent, ValueTask> receiver, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref context._disposed) != 0, context);
            var linked = CancellationTokenSource.CreateLinkedTokenSource(context.LifetimeToken, cancellationToken);
            IAsyncDisposable subscription;
            try
            {
                subscription = await inner.SubscribeAsync(receiver, linked.Token).ConfigureAwait(false);
            }
            catch
            {
                linked.Dispose();
                throw;
            }

            return await context.TrackAsync(new LinkedSubscription(subscription, linked)).ConfigureAwait(false);
        }
    }

    private sealed class ContextHttpClient(AutomationServicesContext context, IAutomationHttpClient inner) : IAutomationHttpClient
    {
        public async Task<AutomationHttpResult> SendAsync(AutomationHttpRequest request, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref context._disposed) != 0, context);
            using var linked = Link(context, cancellationToken);
            return await inner.SendAsync(request, linked.Token).ConfigureAwait(false);
        }
    }

    private sealed class ContextLibrary(AutomationServicesContext context, IAutomationLibraryStore inner) : IAutomationLibrary
    {
        public async Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(string collection, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.ListAsync(context.ModuleId, collection, linked.Token).ConfigureAwait(false);
        }

        public async Task<AutomationLibraryRecord?> GetAsync(string collection, string id, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.GetAsync(context.ModuleId, collection, id, linked.Token).ConfigureAwait(false);
        }

        public async Task UpsertAsync(string collection, string id, int schemaVersion, JsonElement data, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            await inner.UpsertAsync(new AutomationLibraryRecord(context.ModuleId, collection, id, schemaVersion,
                data.Clone(), DateTimeOffset.UtcNow), linked.Token).ConfigureAwait(false);
        }

        public async Task<bool> DeleteAsync(string collection, string id, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.DeleteAsync(context.ModuleId, collection, id, linked.Token).ConfigureAwait(false);
        }

    }

    private sealed class ContextProcessService(AutomationServicesContext context, IAutomationProcessService inner) : IAutomationProcessService
    {
        public async Task<AutomationProcessResult> ExecuteAsync(AutomationProcessRequest request, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref context._disposed) != 0, context);
            using var linked = Link(context, cancellationToken);
            return await inner.ExecuteAsync(request, linked.Token).ConfigureAwait(false);
        }
    }

    private sealed class ContextSecretManager(AutomationServicesContext context, IAutomationSecretManager inner) : IAutomationSecretManager
    {
        public async Task SetAsync(string profileId, string secretId, string value, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            await inner.SetAsync(profileId, secretId, value, linked.Token).ConfigureAwait(false);
        }

        public async Task<bool> ExistsAsync(string profileId, string secretId, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.ExistsAsync(profileId, secretId, linked.Token).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string profileId, string secretId, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            await inner.DeleteAsync(profileId, secretId, linked.Token).ConfigureAwait(false);
        }
    }

    private sealed class ContextApiProfileRunner(AutomationServicesContext context, IAutomationApiProfileRunner inner) : IAutomationApiProfileRunner
    {
        public async Task<AutomationApiProfileResponse> RunProfileAsync(string profileId, JsonElement? input, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.RunProfileAsync(profileId, input?.Clone(), linked.Token).ConfigureAwait(false);
        }
    }

    private sealed class ContextBrowserService(AutomationServicesContext context, IAutomationBrowserService inner) : IAutomationBrowserService
    {
        public async Task<AutomationBrowserRuntimeStatus> GetRuntimeStatusAsync(CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.GetRuntimeStatusAsync(linked.Token).ConfigureAwait(false);
        }

        public async Task<AutomationBrowserActionResult> ExecuteAsync(string profileId, AutomationBrowserAction action, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.ExecuteAsync(profileId, action, linked.Token).ConfigureAwait(false);
        }

        public async Task CloseSessionAsync(string profileId, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            await inner.CloseSessionAsync(profileId, linked.Token).ConfigureAwait(false);
        }
    }

    private sealed class ContextWorkflowRunner(AutomationServicesContext context, IAutomationWorkflowRunner inner) : IAutomationWorkflowRunner
    {
        public async Task<IReadOnlyList<AutomationSavedProfileSummary>> ListSavedProfilesAsync(string moduleId, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.ListSavedProfilesAsync(moduleId, linked.Token).ConfigureAwait(false);
        }

        public async Task<AutomationWorkflowRunResult> RunAsync(string workflowId, JsonElement? initialInput, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.RunAsync(workflowId, initialInput?.Clone(), linked.Token).ConfigureAwait(false);
        }
    }

    private sealed class ContextSchedulerCoordinator(AutomationServicesContext context, IAutomationSchedulerCoordinator inner) : IAutomationSchedulerCoordinator
    {
        public async Task<AutomationSchedulerSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.GetSnapshotAsync(linked.Token).ConfigureAwait(false);
        }

        public async Task SaveAsync(AutomationScheduleDefinition schedule, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            await inner.SaveAsync(schedule, linked.Token).ConfigureAwait(false);
        }

        public async Task<bool> DeleteAsync(string scheduleId, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.DeleteAsync(scheduleId, linked.Token).ConfigureAwait(false);
        }

        public async Task SetEnabledAsync(string scheduleId, bool enabled, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            await inner.SetEnabledAsync(scheduleId, enabled, linked.Token).ConfigureAwait(false);
        }

        public async Task<AutomationScheduleHistoryEntry> RunNowAsync(string scheduleId, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.RunNowAsync(scheduleId, linked.Token).ConfigureAwait(false);
        }
    }

    private sealed class ContextFocusCoordinator(AutomationServicesContext context, IAutomationFocusSessionCoordinator inner) : IAutomationFocusSessionCoordinator
    {
        public async Task<AutomationFocusSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.GetSnapshotAsync(linked.Token).ConfigureAwait(false);
        }

        public async Task<AutomationFocusSessionSnapshot> SaveSettingsAsync(AutomationFocusSettings settings, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.SaveSettingsAsync(settings, linked.Token).ConfigureAwait(false);
        }

        public async Task<AutomationFocusSessionSnapshot> StartAsync(CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.StartAsync(linked.Token).ConfigureAwait(false);
        }

        public async Task<AutomationFocusSessionSnapshot> PauseAsync(CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.PauseAsync(linked.Token).ConfigureAwait(false);
        }

        public async Task<AutomationFocusSessionSnapshot> ResumeAsync(CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.ResumeAsync(linked.Token).ConfigureAwait(false);
        }

        public async Task<AutomationFocusSessionSnapshot> SkipAsync(CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.SkipAsync(linked.Token).ConfigureAwait(false);
        }

        public async Task<AutomationFocusSessionSnapshot> EndAsync(CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.EndAsync(linked.Token).ConfigureAwait(false);
        }
    }

    private sealed class ContextWorkTimeCoordinator(AutomationServicesContext context, IAutomationWorkTimeCoordinator inner) : IAutomationWorkTimeCoordinator
    {
        public async Task<AutomationWorkTimeSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.GetSnapshotAsync(linked.Token).ConfigureAwait(false);
        }

        public async Task<AutomationWorkTimeSnapshot> StartAsync(CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.StartAsync(linked.Token).ConfigureAwait(false);
        }

        public async Task<AutomationWorkTimeSnapshot> StopAsync(CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.StopAsync(linked.Token).ConfigureAwait(false);
        }

        public async Task<AutomationWorkTimeSnapshot> SaveEntryAsync(string description, IReadOnlyList<string> tags, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.SaveEntryAsync(description, tags, linked.Token).ConfigureAwait(false);
        }

        public async Task<AutomationWorkTimeSnapshot> UpdateEntryAsync(string id, string description, IReadOnlyList<string> tags, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.UpdateEntryAsync(id, description, tags, linked.Token).ConfigureAwait(false);
        }

        public async Task<AutomationWorkTimeSnapshot> DeleteEntryAsync(string id, CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.DeleteEntryAsync(id, linked.Token).ConfigureAwait(false);
        }

        public async Task<AutomationWorkTimeSnapshot> ResumePendingAsync(CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.ResumePendingAsync(linked.Token).ConfigureAwait(false);
        }

        public async Task<AutomationWorkTimeSnapshot> DiscardPendingAsync(CancellationToken cancellationToken)
        {
            using var linked = Link(context, cancellationToken);
            return await inner.DiscardPendingAsync(linked.Token).ConfigureAwait(false);
        }
    }

    private sealed class ContextWebsiteLauncher(AutomationServicesContext context, IAutomationWebsiteLauncher inner) : IAutomationWebsiteLauncher
    {
        public async Task LaunchAsync(IReadOnlyList<IReadOnlyList<Uri>> groups, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref context._disposed) != 0, context);
            using var linked = Link(context, cancellationToken);
            await inner.LaunchAsync(groups, linked.Token).ConfigureAwait(false);
        }
    }

    private sealed class LinkedSubscription(IAsyncDisposable inner, CancellationTokenSource linked) : IAsyncDisposable
    {
        private int _disposed;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            linked.Cancel();
            try { await inner.DisposeAsync().ConfigureAwait(false); }
            finally { linked.Dispose(); }
        }
    }

    private sealed class ContextCodexTaskService(AutomationServicesContext context, IAutomationCodexTaskService inner) : IAutomationCodexTaskService
    {
        public async Task<CodexTaskStatus> GetStatusAsync(CancellationToken cancellationToken) { using var linked = Link(context, cancellationToken); return await inner.GetStatusAsync(linked.Token).ConfigureAwait(false); }
        public async Task<CodexTaskDraft> GenerateDraftAsync(CodexTaskGenerateRequest request, CancellationToken cancellationToken) { using var linked = Link(context, cancellationToken); return await inner.GenerateDraftAsync(request, linked.Token).ConfigureAwait(false); }
        public async Task<CodexTaskDraft?> GetDraftAsync(string id, CancellationToken cancellationToken) { using var linked = Link(context, cancellationToken); return await inner.GetDraftAsync(id, linked.Token).ConfigureAwait(false); }
        public async Task<IReadOnlyList<CodexTaskDraftSummary>> ListDraftsAsync(CancellationToken cancellationToken) { using var linked = Link(context, cancellationToken); return await inner.ListDraftsAsync(linked.Token).ConfigureAwait(false); }
        public async Task<CodexTaskRunResult> RunDraftAsync(string id, bool effectConfirmed, JsonElement? taskInput, CancellationToken cancellationToken) { using var linked = Link(context, cancellationToken); return await inner.RunDraftAsync(id, effectConfirmed, taskInput, linked.Token).ConfigureAwait(false); }
        public async Task<CodexTaskSaveResult> SaveDraftAsync(string id, CancellationToken cancellationToken) { using var linked = Link(context, cancellationToken); return await inner.SaveDraftAsync(id, linked.Token).ConfigureAwait(false); }
        public async Task<CodexTaskDraft> ApproveChangesAsync(string id, string? expectedReviewSourceHash, CancellationToken cancellationToken) { using var linked = Link(context, cancellationToken); return await inner.ApproveChangesAsync(id, expectedReviewSourceHash, linked.Token).ConfigureAwait(false); }
        public async Task<string> ExportDraftAsync(string id, CancellationToken cancellationToken) { using var linked = Link(context, cancellationToken); return await inner.ExportDraftAsync(id, linked.Token).ConfigureAwait(false); }
    }
}
