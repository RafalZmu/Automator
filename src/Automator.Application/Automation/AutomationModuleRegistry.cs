using System.Text.Json;
using System.Text.RegularExpressions;
using Automator.Core.Automation;
using Automator.Core.Configuration;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

/// <summary>One action exposed by a bundled module. Payload validation remains owned by that action.</summary>
public sealed record AutomationModuleActionDescriptor(
    string Id,
    int Version,
    IReadOnlyList<AutomationCapabilityRequirement> RequiredCapabilities,
    string? LegacyCommand = null);

/// <summary>Portable metadata shared by the backend registry and the renderer's bundled view registry.</summary>
public sealed record AutomationModuleDefinition(
    int Slot,
    string Id,
    string Title,
    string IconKey,
    string ViewKind,
    bool SearchEnabled,
    int ContractVersion,
    int SettingsVersion,
    IReadOnlyList<AutomationCapabilityRequirement> Capabilities,
    IReadOnlyList<AutomationModuleActionDescriptor> Actions);

public sealed record AutomationModuleSettingsSnapshot(
    string ModuleId,
    int ContractVersion,
    int SettingsVersion,
    JsonElement Value);

/// <summary>A compiled-in module. No dynamic assembly or renderer code loading is supported.</summary>
public interface IAutomationModule : IAutomationTab
{
    AutomationModuleDefinition Definition { get; }

    /// <summary>Returns a fresh JSON value used when this module has no saved preferences.</summary>
    JsonElement CreateDefaultSettings();

    /// <summary>Migrates one older settings value directly to Definition.SettingsVersion.</summary>
    JsonElement MigrateSettings(int fromVersion, JsonElement value);

    ValueTask<AutomationResult> ExecuteAsync(
        string actionId,
        JsonElement input,
        JsonElement moduleSettings,
        AutomationServicesContext services,
        CancellationToken cancellationToken);
}

/// <summary>Validates bundled declarations and dispatches only registered, active module actions.</summary>
public sealed class AutomationModuleRegistry
{
    private static readonly Regex StableId = new("^[a-z][a-z0-9.-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex ActionId = new("^[a-z][A-Za-z0-9._-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private readonly IReadOnlyDictionary<string, IAutomationModule> _modules;
    private readonly AutomationCapabilityRegistry _capabilities;

    public AutomationModuleRegistry(IEnumerable<IAutomationModule> modules, AutomationCapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(modules);
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
        var entries = modules.ToArray();
        Validate(entries);
        _modules = entries.ToDictionary(module => module.Id, StringComparer.Ordinal);
        Modules = Array.AsReadOnly(entries.Select(module => module.Definition).OrderBy(module => module.Slot).ToArray());
    }

    public IReadOnlyList<AutomationModuleDefinition> Modules { get; }

    public async Task<AutomationResult> DispatchAsync(
        string moduleId,
        string actionId,
        int contractVersion,
        int actionVersion,
        JsonElement input,
        string activeModuleId,
        AutomationServicesContext services,
        IReadOnlyList<ModuleSettingsEntry> moduleSettings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(moduleId, activeModuleId, StringComparison.Ordinal)
            || !string.Equals(services.ModuleId, activeModuleId, StringComparison.Ordinal))
            throw new InvalidOperationException("The requested automation module is not active.");
        if (!_modules.TryGetValue(moduleId, out var module))
            throw new InvalidOperationException($"Automation module '{moduleId}' is not registered.");
        var definition = module.Definition;
        if (contractVersion != definition.ContractVersion || contractVersion != AutomationTabContract.CurrentVersion)
            throw new InvalidOperationException($"Automation module '{moduleId}' contract version {contractVersion} is not supported.");
        var action = definition.Actions.FirstOrDefault(candidate => string.Equals(candidate.Id, actionId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Action '{actionId}' is not registered for module '{moduleId}'.");
        if (actionVersion != action.Version)
            throw new InvalidOperationException($"Action '{actionId}' version {actionVersion} is not supported for module '{moduleId}'.");
        if (action.LegacyCommand is not null)
            throw new InvalidOperationException($"Action '{actionId}' is routed through the existing host command and is not a module action.");

        foreach (var required in action.RequiredCapabilities)
        {
            var declared = definition.Capabilities.Any(capability => capability == required);
            if (!declared || !services.HasCapability(required.Id))
                throw new InvalidOperationException($"Action '{actionId}' is missing its declared '{required.Id}' capability grant.");
        }

        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, services.LifetimeToken);
        var settings = GetSettings(moduleId, moduleSettings);
        var result = await module.ExecuteAsync(action.Id, input.Clone(), settings, services, linkedCancellation.Token).ConfigureAwait(false);
        ValidateResult(result);
        if (result.ContractVersion != AutomationTabContract.CurrentVersion)
            throw new InvalidOperationException("The module returned an unsupported automation result contract version.");
        return result;
    }

    /// <summary>Updates known older values and copies unknown module entries through unchanged.</summary>
    public List<ModuleSettingsEntry> MigrateModuleSettings(IReadOnlyList<ModuleSettingsEntry> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var migrated = new List<ModuleSettingsEntry>(settings.Count);
        foreach (var entry in settings)
        {
            if (!_modules.TryGetValue(entry.ModuleId, out var module))
            {
                migrated.Add(entry with { Value = entry.Value.Clone() });
                continue;
            }

            var currentVersion = module.Definition.SettingsVersion;
            if (entry.SchemaVersion > currentVersion)
                throw new InvalidOperationException($"Module '{entry.ModuleId}' settings version {entry.SchemaVersion} is newer than the supported version {currentVersion}.");
            if (entry.SchemaVersion == currentVersion)
            {
                migrated.Add(entry with { Value = entry.Value.Clone() });
                continue;
            }

            var value = module.MigrateSettings(entry.SchemaVersion, entry.Value.Clone());
            if (value.ValueKind == JsonValueKind.Undefined)
                throw new InvalidOperationException($"Module '{entry.ModuleId}' settings migration returned no JSON value.");
            migrated.Add(new ModuleSettingsEntry(entry.ModuleId, currentVersion, value.Clone()));
        }
        return migrated;
    }

    public JsonElement GetSettings(string moduleId, IReadOnlyList<ModuleSettingsEntry> settings)
    {
        if (!_modules.TryGetValue(moduleId, out var module))
            throw new InvalidOperationException($"Automation module '{moduleId}' is not registered.");
        var stored = settings.FirstOrDefault(entry => string.Equals(entry.ModuleId, moduleId, StringComparison.Ordinal));
        if (stored is null) return module.CreateDefaultSettings().Clone();
        return MigrateModuleSettings([stored]).Single().Value.Clone();
    }

    /// <summary>Reads default or stored settings only for the active registered module version.</summary>
    public AutomationModuleSettingsSnapshot GetActiveSettings(
        string moduleId,
        int contractVersion,
        int settingsVersion,
        string activeModuleId,
        IReadOnlyList<ModuleSettingsEntry> currentSettings)
    {
        ArgumentNullException.ThrowIfNull(currentSettings);
        if (!string.Equals(moduleId, activeModuleId, StringComparison.Ordinal))
            throw new InvalidOperationException("The requested automation module is not active.");
        if (!_modules.TryGetValue(moduleId, out var module))
            throw new InvalidOperationException($"Automation module '{moduleId}' is not registered.");
        var definition = module.Definition;
        if (contractVersion != definition.ContractVersion || settingsVersion != definition.SettingsVersion)
            throw new InvalidOperationException($"Automation module '{moduleId}' settings version is not supported.");
        return new AutomationModuleSettingsSnapshot(moduleId, definition.ContractVersion, definition.SettingsVersion,
            GetSettings(moduleId, currentSettings).Clone());
    }

    /// <summary>Creates a replacement preference list for a validated active module settings write.</summary>
    public List<ModuleSettingsEntry> CreateUpdatedModuleSettings(
        string moduleId,
        int contractVersion,
        int settingsVersion,
        JsonElement value,
        string activeModuleId,
        IReadOnlyList<ModuleSettingsEntry> currentSettings)
    {
        ArgumentNullException.ThrowIfNull(currentSettings);
        if (!string.Equals(moduleId, activeModuleId, StringComparison.Ordinal))
            throw new InvalidOperationException("The requested automation module is not active.");
        if (!_modules.TryGetValue(moduleId, out var module))
            throw new InvalidOperationException($"Automation module '{moduleId}' is not registered.");
        if (contractVersion != module.Definition.ContractVersion
            || settingsVersion != module.Definition.SettingsVersion)
            throw new InvalidOperationException($"Automation module '{moduleId}' settings version is not supported.");
        if (value.ValueKind == JsonValueKind.Undefined
            || System.Text.Encoding.UTF8.GetByteCount(value.GetRawText()) > ModuleSettingsEntry.MaximumPayloadBytes)
            throw new InvalidOperationException("Module settings are missing or exceed the 64 KiB limit.");

        var updated = currentSettings
            .Where(entry => !string.Equals(entry.ModuleId, moduleId, StringComparison.Ordinal))
            .Select(entry => entry with { Value = entry.Value.Clone() })
            .ToList();
        updated.Add(new ModuleSettingsEntry(moduleId, settingsVersion, value.Clone()));
        return updated;
    }

    private void Validate(IReadOnlyList<IAutomationModule> modules)
    {
        if (modules.Count > 9) throw new InvalidOperationException("At most nine bundled automation modules can be registered.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var slots = new HashSet<int>();
        foreach (var module in modules)
        {
            var definition = module.Definition ?? throw new InvalidOperationException("An automation module needs a definition.");
            if (module.ContractVersion != definition.ContractVersion || module.Id != definition.Id || module.Title != definition.Title)
                throw new InvalidOperationException("The module identity must match its registered definition.");
            if (!StableId.IsMatch(definition.Id) || !ids.Add(definition.Id))
                throw new InvalidOperationException($"Automation module id '{definition.Id}' is invalid or duplicated.");
            if (definition.Slot is < 1 or > 9 || !slots.Add(definition.Slot))
                throw new InvalidOperationException($"Automation module '{definition.Id}' has an invalid or duplicated slot.");
            if (string.IsNullOrWhiteSpace(definition.Title) || definition.Title.Length > 128
                || string.IsNullOrWhiteSpace(definition.IconKey) || definition.IconKey.Length > 64
                || string.IsNullOrWhiteSpace(definition.ViewKind) || definition.ViewKind.Length > 64)
                throw new InvalidOperationException($"Automation module '{definition.Id}' has invalid view metadata.");
            if (definition.ContractVersion != AutomationTabContract.CurrentVersion || definition.SettingsVersion <= 0)
                throw new InvalidOperationException($"Automation module '{definition.Id}' uses an unsupported contract or settings version.");

            _capabilities.ValidateRequirements(definition.Id, definition.Capabilities);
            var actionIds = new HashSet<string>(StringComparer.Ordinal);
            if (definition.Actions is null)
                throw new InvalidOperationException($"Automation module '{definition.Id}' is missing its action list.");
            foreach (var action in definition.Actions)
            {
                if (!ActionId.IsMatch(action.Id) || action.Version <= 0 || !actionIds.Add(action.Id))
                    throw new InvalidOperationException($"Automation module '{definition.Id}' has an invalid or duplicated action id '{action.Id}'.");
                if (action.LegacyCommand is { Length: 0 or > 128 })
                    throw new InvalidOperationException($"Automation module '{definition.Id}' has an invalid legacy command mapping.");
                _capabilities.ValidateRequirements($"{definition.Id}/{action.Id}", action.RequiredCapabilities);
                if (action.RequiredCapabilities.Any(required => !definition.Capabilities.Contains(required)))
                    throw new InvalidOperationException($"Action '{action.Id}' requires a capability not declared by module '{definition.Id}'.");
            }
        }
    }

    private static void ValidateResult(AutomationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!Enum.IsDefined(result.Status) || result.Message is null || result.Message.Length > 4096)
            throw new InvalidOperationException("The module returned an invalid result status or message.");
        if (result.Data.ValueKind == JsonValueKind.Undefined
            || System.Text.Encoding.UTF8.GetByteCount(result.Data.GetRawText()) > 512 * 1024)
            throw new InvalidOperationException("The module returned missing or oversized structured data.");
        if (result.Actions is null || result.Actions.Count > 32)
            throw new InvalidOperationException("The module returned too many follow-up actions.");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var action in result.Actions)
        {
            if (action is null || !ActionId.IsMatch(action.Id) || action.Version <= 0
                || action.Label is not { Length: > 0 and <= 128 }
                || action.Payload.ValueKind == JsonValueKind.Undefined
                || System.Text.Encoding.UTF8.GetByteCount(action.Payload.GetRawText()) > ModuleSettingsEntry.MaximumPayloadBytes
                || !ids.Add(action.Id))
                throw new InvalidOperationException("The module returned an invalid or duplicated follow-up action.");
        }
    }
}
