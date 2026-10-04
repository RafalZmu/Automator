using System.Text.Json;
using System.Text.Json.Serialization;
using Automator.Core.Launcher;

namespace Automator.Core.Configuration;

public enum AppTheme
{
    Light,
    Dark
}

public sealed record LauncherSettings
{
    public const int CurrentSchemaVersion = 2;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public string Hotkey { get; init; } = "RightControl";
    public AppTheme Theme { get; init; } = AppTheme.Light;
    public bool StartWithWindows { get; init; } = true;
    public List<AppBinding> Bindings { get; init; } = [];
    public List<ModuleSettingsEntry> ModuleSettings { get; init; } = [];
}

/// <summary>Opaque JSON preferences owned by a single automation module.</summary>
public sealed record ModuleSettingsEntry(string ModuleId, int SchemaVersion, JsonElement Value)
{
    public const int MaximumPayloadBytes = 64 * 1024;
}

public static class SettingsSerializer
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string Serialize(LauncherSettings settings)
    {
        Validate(settings);
        return JsonSerializer.Serialize(settings, Options);
    }

    public static LauncherSettings Deserialize(string json)
    {
        LauncherSettings settings;
        try
        {
            settings = JsonSerializer.Deserialize<LauncherSettings>(json, Options)
                ?? throw new InvalidDataException("The settings file is empty or invalid.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The settings JSON is malformed or contains an unsupported value.", exception);
        }

        // Schema 1 contained only launcher fields. The added module-settings collection
        // defaults to empty, so copying the known fields preserves every prior preference.
        if (settings.SchemaVersion == 1)
        {
            settings = settings with { SchemaVersion = LauncherSettings.CurrentSchemaVersion };
        }

        Validate(settings);
        return settings;
    }

    public static int ReadSchemaVersion(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("The settings document must be a JSON object.");
            if (!document.RootElement.TryGetProperty(nameof(LauncherSettings.SchemaVersion), out var version))
                return 1; // Schema 1 historically treated an omitted version as its default.
            if (!version.TryGetInt32(out var value))
                throw new InvalidDataException("The settings schema version is invalid.");
            return value;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The settings JSON is malformed or contains an unsupported value.", exception);
        }
    }

    public static IReadOnlyList<string> FindMissingPaths(LauncherSettings settings, Func<string, bool> pathExists)
    {
        ArgumentNullException.ThrowIfNull(pathExists);
        return settings.Bindings
            .Select(binding => binding.TargetPath)
            .Where(path => !pathExists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void Validate(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.SchemaVersion != LauncherSettings.CurrentSchemaVersion)
        {
            throw new InvalidDataException($"Settings schema version {settings.SchemaVersion} is not supported.");
        }

        if (string.IsNullOrWhiteSpace(settings.Hotkey) || settings.Hotkey.Length > 32)
        {
            throw new InvalidDataException("The opener key must be a single-key name.");
        }

        if (!HotkeyKeyNameValidator.IsSupported(settings.Hotkey))
        {
            throw new InvalidDataException($"The opener key '{settings.Hotkey}' is not supported.");
        }

        if (!Enum.IsDefined(settings.Theme))
        {
            throw new InvalidDataException($"Theme value '{settings.Theme}' is not supported.");
        }

        if (settings.Bindings is null)
        {
            throw new InvalidDataException("The bindings list is missing.");
        }

        var aliases = new List<string>();
        var bindingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var binding in settings.Bindings)
        {
            if (binding is null)
            {
                throw new InvalidDataException("The bindings list contains a null entry.");
            }

            if (string.IsNullOrWhiteSpace(binding.Id) || string.IsNullOrWhiteSpace(binding.Name) || string.IsNullOrWhiteSpace(binding.TargetPath))
            {
                throw new InvalidDataException("Each app binding needs an id, display name, and target path.");
            }

            if (binding.Arguments is null)
            {
                throw new InvalidDataException($"App binding '{binding.Id}' has null arguments.");
            }

            if (!bindingIds.Add(binding.Id))
            {
                throw new InvalidDataException($"App binding id '{binding.Id}' is duplicated.");
            }

            var error = AliasValidator.Validate(binding.Alias, aliases);
            if (error is not null)
            {
                throw new InvalidDataException($"App alias '{binding.Alias}' is invalid: {error}.");
            }

            aliases.Add(binding.Alias);
        }

        ValidateModuleSettings(settings.ModuleSettings);
    }

    private static void ValidateModuleSettings(List<ModuleSettingsEntry>? moduleSettings)
    {
        if (moduleSettings is null || moduleSettings.Count > 64)
            throw new InvalidDataException("Module settings must be present and contain at most 64 entries.");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var totalBytes = 0;
        foreach (var entry in moduleSettings)
        {
            if (entry is null || entry.ModuleId is not { Length: > 0 and <= 64 }
                || !System.Text.RegularExpressions.Regex.IsMatch(entry.ModuleId, "^[a-z][a-z0-9.-]*$", System.Text.RegularExpressions.RegexOptions.CultureInvariant))
                throw new InvalidDataException("Each module settings entry needs a stable lowercase module id.");
            if (!ids.Add(entry.ModuleId))
                throw new InvalidDataException($"Module settings for '{entry.ModuleId}' are duplicated.");
            if (entry.SchemaVersion <= 0)
                throw new InvalidDataException($"Module settings for '{entry.ModuleId}' have an invalid schema version.");
            if (entry.Value.ValueKind is JsonValueKind.Undefined)
                throw new InvalidDataException($"Module settings for '{entry.ModuleId}' contain no JSON value.");

            var bytes = System.Text.Encoding.UTF8.GetByteCount(entry.Value.GetRawText());
            if (bytes > ModuleSettingsEntry.MaximumPayloadBytes)
                throw new InvalidDataException($"Module settings for '{entry.ModuleId}' exceed the 64 KiB limit.");
            totalBytes = checked(totalBytes + bytes);
            if (totalBytes > 512 * 1024)
                throw new InvalidDataException("All module settings together exceed the 512 KiB limit.");
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
