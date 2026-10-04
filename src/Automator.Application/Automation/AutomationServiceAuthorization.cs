using Automator.Core.Automation;

namespace Automator.Application.Automation;

public enum AutomationServiceAuthorizationFailure
{
    None,
    InactiveModule,
    CapabilityNotGranted,
    ContextNotEligible,
}

/// <summary>Checks the active module grant before the backend dispatches a service operation.</summary>
public static class AutomationServiceAuthorization
{
    public static AutomationServiceAuthorizationFailure Evaluate(
        string requestedModuleId,
        string activeModuleId,
        IReadOnlyList<AutomationCapabilityRequirement> capabilities,
        string requiredCapability,
        bool contextEligible,
        bool requireEligibleContext = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedModuleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(activeModuleId);
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredCapability);

        if (!string.Equals(requestedModuleId, activeModuleId, StringComparison.Ordinal))
            return AutomationServiceAuthorizationFailure.InactiveModule;
        if (!capabilities.Any(capability => string.Equals(capability.Id, requiredCapability, StringComparison.Ordinal)
            && capability.Version == 1))
            return AutomationServiceAuthorizationFailure.CapabilityNotGranted;
        return !requireEligibleContext || contextEligible
            ? AutomationServiceAuthorizationFailure.None
            : AutomationServiceAuthorizationFailure.ContextNotEligible;
    }
}
