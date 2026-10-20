using System.Text.Json;
using System.Text.RegularExpressions;
using Automator.Application.Launcher;
using Automator.Core.Automation;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

/// <summary>
/// Stores reusable text snippets, each with an optional typed shortcut. The module keeps its snippets as opaque JSON module
/// preferences that the renderer validates, and the renderer copies a snippet to the clipboard. It therefore registers no
/// actions and needs no host capability. The snippets that have a shortcut are also published in the module state, so the
/// launch panel can copy one without opening this tab.
/// </summary>
public sealed class SnippetsModule : ILauncherTabModuleProvider
{
    public const string IdValue = "snippets";
    public const int SettingsVersionValue = 1;
    public const int MaximumSnippets = 100;
    public const int MaximumValueLength = 4000;
    /// <summary>The renderer rejects module state values longer than 2048 characters, so longer text is split into chunks.</summary>
    public const int MaximumStateValueLength = 2000;
    public const string MetaKeyPrefix = "meta/";
    public const string TextKeyPrefix = "text/";

    private static readonly Regex IdPattern = new("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex AliasPattern = new("^[a-z0-9]{1,32}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

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

    /// <summary>
    /// Builds the module state entries for every snippet that can be copied from the launch panel: a <c>meta/{id}</c> entry with
    /// its name, shortcut and chunk count, and its text in <c>text/{id}/{index}</c> chunks of at most
    /// <see cref="MaximumStateValueLength"/> characters. Invalid snippets and repeated shortcuts are skipped rather than reported,
    /// because the Snippets tab already reports invalid preferences and this must never stop the launcher state from publishing.
    /// </summary>
    public static IReadOnlyDictionary<string, string> CreateShortcutStateValues(JsonElement settings)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (settings.ValueKind != JsonValueKind.Object || !settings.TryGetProperty("snippets", out var list)
            || list.ValueKind != JsonValueKind.Array) return values;

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var snippet in list.EnumerateArray().Take(MaximumSnippets))
        {
            if (snippet.ValueKind != JsonValueKind.Object
                || ReadString(snippet, "id") is not { } id || !IdPattern.IsMatch(id)
                || ReadString(snippet, "name") is not { } name || string.IsNullOrWhiteSpace(name) || name.Length > 128 || name.Any(char.IsControl)
                || ReadString(snippet, "alias") is not { } alias || !AliasPattern.IsMatch(alias)
                || ReadString(snippet, "value") is not { } text || string.IsNullOrWhiteSpace(text) || text.Length > MaximumValueLength
                || text.Any(character => char.IsControl(character) && character is not ('\t' or '\n' or '\r'))
                || ids.Contains(id) || aliases.Contains(alias)) continue;

            var chunks = Chunk(text).ToArray();
            ids.Add(id);
            aliases.Add(alias);
            values[MetaKeyPrefix + id] = JsonSerializer.Serialize(new { name, alias = alias.ToLowerInvariant(), chunks = chunks.Length });
            for (var index = 0; index < chunks.Length; index++) values[$"{TextKeyPrefix}{id}/{index}"] = chunks[index];
        }
        return values;
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static IEnumerable<string> Chunk(string text)
    {
        for (var start = 0; start < text.Length;)
        {
            var length = Math.Min(MaximumStateValueLength, text.Length - start);
            // Do not split a surrogate pair across two chunks.
            if (start + length < text.Length && char.IsHighSurrogate(text[start + length - 1])) length--;
            yield return text.Substring(start, length);
            start += length;
        }
    }
}
