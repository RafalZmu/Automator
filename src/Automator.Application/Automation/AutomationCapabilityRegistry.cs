using Automator.Core.Automation;

namespace Automator.Application.Automation;

/// <summary>Validates module capability declarations and grants only host-registered typed adapters.</summary>
public sealed class AutomationCapabilityRegistry(
    Func<string, IAutomationKeyboardInput>? keyboardInputFactory = null,
    Func<string, AutomationHttpPolicy, IAutomationHttpClient>? httpClientFactory = null,
    Func<string, IAutomationLibraryStore>? libraryStoreFactory = null,
    Func<string, IAutomationProcessService>? processServiceFactory = null,
    Func<string, IAutomationSecretManager>? secretManagerFactory = null,
    Func<string, IAutomationApiProfileRunner>? apiProfileRunnerFactory = null,
    Func<string, IAutomationBrowserService>? browserServiceFactory = null,
    Func<string, IAutomationWorkflowRunner>? workflowRunnerFactory = null,
    Func<string, IAutomationSchedulerCoordinator>? schedulerCoordinatorFactory = null,
    Func<string, IAutomationFocusSessionCoordinator>? focusCoordinatorFactory = null,
    Func<string, IAutomationWorkTimeCoordinator>? workTimeCoordinatorFactory = null)
{
    private static readonly IReadOnlyDictionary<string, int> Supported = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        [AutomationCapabilityIds.KeyboardInput] = 1,
        [AutomationCapabilityIds.HttpRequest] = 1,
        [AutomationCapabilityIds.LibraryStorage] = 1,
        [AutomationCapabilityIds.ProcessExecution] = 1,
        [AutomationCapabilityIds.SecretManagement] = 1,
        [AutomationCapabilityIds.ApiProfileHttp] = 1,
        [AutomationCapabilityIds.BrowserSession] = 1,
        [AutomationCapabilityIds.WorkflowExecution] = 1,
        [AutomationCapabilityIds.SchedulerManagement] = 1,
        [AutomationCapabilityIds.FocusManagement] = 1,
        [AutomationCapabilityIds.WorkTimeManagement] = 1,
    };

    public AutomationServicesContext CreateContext(AutomationModuleDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (string.IsNullOrWhiteSpace(descriptor.ModuleId))
            throw new InvalidOperationException("An automation module must have a non-empty ID.");

        ValidateRequirements(descriptor.ModuleId, descriptor.Capabilities);
        var capabilityIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var capability in descriptor.Capabilities)
        {
            if (!capabilityIds.Add(capability.Id))
                throw new InvalidOperationException($"Automation module '{descriptor.ModuleId}' declares duplicate capability '{capability.Id}'.");
        }

        IAutomationHttpClient? http = null;
        if (capabilityIds.Contains(AutomationCapabilityIds.HttpRequest))
        {
            if (descriptor.HttpPolicy is null)
                throw new InvalidOperationException("The HTTP capability requires a host-owned policy.");
            if (httpClientFactory is null)
                throw new InvalidOperationException("The HTTP capability has no host service registered.");
            http = httpClientFactory(descriptor.ModuleId, descriptor.HttpPolicy);
        }

        IAutomationKeyboardInput? keyboard = null;
        if (capabilityIds.Contains(AutomationCapabilityIds.KeyboardInput))
        {
            keyboard = keyboardInputFactory?.Invoke(descriptor.ModuleId)
                ?? throw new InvalidOperationException("The keyboard capability has no host service registered.");
        }

        IAutomationLibraryStore? library = null;
        if (capabilityIds.Contains(AutomationCapabilityIds.LibraryStorage))
        {
            library = libraryStoreFactory?.Invoke(descriptor.ModuleId)
                ?? throw new InvalidOperationException("The library storage capability has no host service registered.");
        }

        IAutomationProcessService? process = null;
        if (capabilityIds.Contains(AutomationCapabilityIds.ProcessExecution))
        {
            process = processServiceFactory?.Invoke(descriptor.ModuleId)
                ?? throw new InvalidOperationException("The process execution capability has no host service registered.");
        }

        IAutomationSecretManager? secrets = null;
        if (capabilityIds.Contains(AutomationCapabilityIds.SecretManagement))
            secrets = secretManagerFactory?.Invoke(descriptor.ModuleId)
                ?? throw new InvalidOperationException("The secret management capability has no host service registered.");

        IAutomationApiProfileRunner? apiProfiles = null;
        if (capabilityIds.Contains(AutomationCapabilityIds.ApiProfileHttp))
            apiProfiles = apiProfileRunnerFactory?.Invoke(descriptor.ModuleId)
                ?? throw new InvalidOperationException("The API profile HTTP capability has no host service registered.");

        IAutomationBrowserService? browser = null;
        if (capabilityIds.Contains(AutomationCapabilityIds.BrowserSession))
            browser = browserServiceFactory?.Invoke(descriptor.ModuleId)
                ?? throw new InvalidOperationException("The browser session capability has no host service registered.");

        IAutomationWorkflowRunner? workflows = null;
        if (capabilityIds.Contains(AutomationCapabilityIds.WorkflowExecution))
            workflows = workflowRunnerFactory?.Invoke(descriptor.ModuleId)
                ?? throw new InvalidOperationException("The workflow execution capability has no host service registered.");

        IAutomationSchedulerCoordinator? scheduler = null;
        if (capabilityIds.Contains(AutomationCapabilityIds.SchedulerManagement))
            scheduler = schedulerCoordinatorFactory?.Invoke(descriptor.ModuleId)
                ?? throw new InvalidOperationException("The scheduler capability has no host service registered.");

        IAutomationFocusSessionCoordinator? focusSessions = null;
        if (capabilityIds.Contains(AutomationCapabilityIds.FocusManagement))
            focusSessions = focusCoordinatorFactory?.Invoke(descriptor.ModuleId)
                ?? throw new InvalidOperationException("The focus session capability has no host service registered.");

        IAutomationWorkTimeCoordinator? workTime = null;
        if (capabilityIds.Contains(AutomationCapabilityIds.WorkTimeManagement))
            workTime = workTimeCoordinatorFactory?.Invoke(descriptor.ModuleId)
                ?? throw new InvalidOperationException("The work-time capability has no host service registered.");

        return new AutomationServicesContext(descriptor.ModuleId, keyboard, http, library, process,
            secrets, apiProfiles, browser, workflows, scheduler, focusSessions, workTime);
    }

    public void ValidateRequirements(string moduleId, IReadOnlyList<AutomationCapabilityRequirement> capabilities)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        ArgumentNullException.ThrowIfNull(capabilities);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var capability in capabilities)
        {
            if (string.IsNullOrWhiteSpace(capability.Id) || !Supported.TryGetValue(capability.Id, out var supportedVersion))
                throw new InvalidOperationException($"Unknown automation capability '{capability.Id}'.");
            if (capability.Version != supportedVersion)
                throw new InvalidOperationException($"Automation capability '{capability.Id}' version {capability.Version} is not supported.");
            if (!ids.Add(capability.Id))
                throw new InvalidOperationException($"Automation module '{moduleId}' declares duplicate capability '{capability.Id}'.");
        }
    }
}
