using System.Text.Json;
using Automator.Application.Launcher;
using Automator.Core.Automation;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

/// <summary>
/// Stores short markdown notes. The module keeps its notes as opaque JSON module preferences that the renderer validates, edits,
/// renders, and saves, so it registers no actions and needs no host capability.
/// </summary>
public sealed class NotesModule : ILauncherTabModuleProvider
{
    public const string IdValue = "notes";
    public const int SettingsVersionValue = 1;
    public const int SlotValue = 10;

    public AutomationModuleDefinition Definition { get; } = new(SlotValue, IdValue, "Quick notes", "notebook", "notes", false,
        1, SettingsVersionValue, [], []);
    public int ContractVersion => Definition.ContractVersion;
    public string Id => Definition.Id;
    public string Title => Definition.Title;
    public LauncherModuleState CreateInitialState() => new(Id, LauncherTabRegistry.Version,
        new Dictionary<string, string>(StringComparer.Ordinal) { ["status"] = "ready" });

    public JsonElement CreateDefaultSettings()
    {
        using var document = JsonDocument.Parse("{\"notes\":[]}");
        return document.RootElement.Clone();
    }

    public JsonElement MigrateSettings(int fromVersion, JsonElement value) =>
        throw new InvalidOperationException($"Quick notes settings version {fromVersion} cannot be migrated.");

    public ValueTask<AutomationResult> ExecuteAsync(string actionId, JsonElement input, JsonElement moduleSettings,
        AutomationServicesContext services, CancellationToken cancellationToken) =>
        throw new InvalidOperationException($"Quick notes has no module action '{actionId}'.");
}
