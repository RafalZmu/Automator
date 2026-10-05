using System.Text.Json;
using Automator.Application.Automation;
using Automator.Core.Automation;
using Automator.Core.Plugins;

namespace Automator.Application.Launcher;

/// <summary>A platform and presentation independent description of one launcher action.</summary>
public sealed record LauncherModuleAction(
    string Id,
    int Version,
    string? Command,
    IReadOnlyList<AutomationCapabilityRequirement> RequiredCapabilities);

/// <summary>Serializable module state. New renderer modules can evolve their state without coupling the host to UI controls.</summary>
public sealed record LauncherModuleState(string ModuleId, int Version, IReadOnlyDictionary<string, string> Values);

/// <summary>Metadata sent to the renderer. Commands are protocol names, never UI framework types.</summary>
public sealed record LauncherTabMetadata(
    int Slot,
    string Id,
    string Title,
    string IconKey,
    string Kind,
    bool SearchEnabled,
    int ContractVersion,
    int SettingsVersion,
    IReadOnlyList<LauncherModuleAction> Actions,
    IReadOnlyList<AutomationCapabilityRequirement> Capabilities);

/// <summary>Legacy UI state supplied by a module provider for the existing launcher shell.</summary>
public interface ILauncherTabModuleProvider : IAutomationModule
{
    LauncherModuleState CreateInitialState();
}

public static class LauncherTabRegistry
{
    public const int Version = 3;

    private static readonly IReadOnlyList<ILauncherTabModuleProvider> Providers = CreateProviders();

    // Renderer descriptors and backend action handlers are built from the same module definitions.
    public static IReadOnlyList<LauncherTabMetadata> Tabs { get; } = Array.AsReadOnly(Providers.Select(ToMetadata).ToArray());
    public static IReadOnlyList<LauncherModuleState> States => Array.AsReadOnly(Providers.Select(provider => provider.CreateInitialState()).ToArray());

    public static AutomationModuleRegistry CreateAutomationRegistry(
        AutomationCapabilityRegistry capabilities,
        IAutomationVariableProvider? variables = null,
        string? dataDirectory = null) =>
        new(CreateProviders(variables, dataDirectory), capabilities);

    public static bool TryGetAction(string moduleId, string actionId, out LauncherModuleAction? action)
    {
        var provider = Providers.FirstOrDefault(candidate => string.Equals(candidate.Id, moduleId, StringComparison.Ordinal));
        action = provider?.Definition.Actions
            .FirstOrDefault(candidate => string.Equals(candidate.Id, actionId, StringComparison.Ordinal)) is { } registered
                ? new LauncherModuleAction(registered.Id, registered.Version, registered.LegacyCommand, registered.RequiredCapabilities)
                : null;
        return action is not null;
    }

    private static LauncherTabMetadata ToMetadata(ILauncherTabModuleProvider provider)
    {
        var definition = provider.Definition;
        return new LauncherTabMetadata(
            definition.Slot,
            definition.Id,
            definition.Title,
            definition.IconKey,
            definition.ViewKind,
            definition.SearchEnabled,
            definition.ContractVersion,
            definition.SettingsVersion,
            Array.AsReadOnly(definition.Actions.Select(action =>
                new LauncherModuleAction(action.Id, action.Version, action.LegacyCommand, action.RequiredCapabilities)).ToArray()),
            definition.Capabilities);
    }

    private static IReadOnlyList<ILauncherTabModuleProvider> CreateProviders(IAutomationVariableProvider? variables = null,
        string? dataDirectory = null)
    {
        var providers = new List<ILauncherTabModuleProvider>
        {
            new BundledModuleProvider(
                new AutomationModuleDefinition(1, "launcher", "Launcher", "sparkles", "launcher", true, 1, 1, [],
                [
                    new("search", 1, [], "launcher/setQuery"),
                    new("selectBinding", 1, [], "binding/select"),
                    new("openCatalog", 1, [], "launcher/openCatalog"),
                    new("addCustom", 1, [], "catalog/addCustom")
                ]),
                new Dictionary<string, string>()),
            new ScriptRunnerModule(variables, new ScriptRunnerTemplateInstaller(
                dataDirectory is null ? null : Path.Combine(dataDirectory, "script-templates"))),
            new ApiModule(),
            new BrowserAutomationModule(variables),
            new WorkflowModule(),
            new SchedulerModule(),
            new FocusSessionsModule(),
        };

        for (var slot = 8; slot <= 9; slot++)
        {
            var id = $"reserved-{slot}";
            providers.Add(new BundledModuleProvider(
                new AutomationModuleDefinition(slot, id, $"Tab {slot}", "grid", "reserved", false, 1, 1, [], []),
                new Dictionary<string, string> { ["status"] = "reserved" }));
        }

        return providers.AsReadOnly();
    }

    private sealed class BundledModuleProvider(
        AutomationModuleDefinition definition,
        IReadOnlyDictionary<string, string> initialValues) : ILauncherTabModuleProvider
    {
        public AutomationModuleDefinition Definition { get; } = definition;
        public int ContractVersion => Definition.ContractVersion;
        public string Id => Definition.Id;
        public string Title => Definition.Title;

        public LauncherModuleState CreateInitialState() =>
            new(Id, Version, new Dictionary<string, string>(initialValues, StringComparer.Ordinal));

        public JsonElement CreateDefaultSettings()
        {
            using var document = JsonDocument.Parse("{}");
            return document.RootElement.Clone();
        }

        public JsonElement MigrateSettings(int fromVersion, JsonElement value) =>
            throw new InvalidOperationException($"Module '{Id}' does not support settings migration from version {fromVersion}.");

        public ValueTask<AutomationResult> ExecuteAsync(
            string actionId,
            JsonElement input,
            JsonElement moduleSettings,
            AutomationServicesContext services,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException($"Legacy action '{actionId}' is handled by the existing launcher command route.");
    }
}
