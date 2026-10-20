using System.Text.Json;
using Automator.Application.Launcher;
using Automator.Core.Automation;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

/// <summary>
/// Stores reusable text snippets, each with an optional typed shortcut. The module keeps its snippets as opaque JSON module
/// preferences that the renderer validates, and the renderer copies a snippet to the clipboard. It therefore registers no
/// actions and needs no host capability.
/// </summary>
public sealed class SnippetsModule : ILauncherTabModuleProvider
{
    public const string IdValue = "snippets";
    public const int SettingsVersionValue = 1;

    public AutomationModuleDefinition Definition { get; } = new(9, IdValue, "Snippets", "clipboard", "snippets", true,
        1, SettingsVersionValue, [], []);
    public int ContractVersion => Definition.ContractVersion;
    public string Id => Definition.Id;
    public string Title => Definition.Title;
    public LauncherModuleState CreateInitialState() => new(Id, LauncherTabRegistry.Version,
        new Dictionary<string, string>(StringComparer.Ordinal) { ["status"] = "ready" });

    public JsonElement CreateDefaultSettings()
    {
        using var document = JsonDocument.Parse("{\"snippets\":[]}");
        return document.RootElement.Clone();
    }

    public JsonElement MigrateSettings(int fromVersion, JsonElement value) =>
        throw new InvalidOperationException($"Snippets settings version {fromVersion} cannot be migrated.");

    public ValueTask<AutomationResult> ExecuteAsync(string actionId, JsonElement input, JsonElement moduleSettings,
        AutomationServicesContext services, CancellationToken cancellationToken) =>
        throw new InvalidOperationException($"Snippets has no module action '{actionId}'.");
}
