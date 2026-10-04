using System.Text;
using System.Text.Json;
using Automator.Application.Logging;
using Automator.Application.Automation;
using Automator.Core.Configuration;
using Microsoft.Win32;

namespace Automator.Windows;

public sealed record SettingsLoadResult(LauncherSettings Settings, bool IsValid, bool Exists, string? Error);

/// <summary>Persists schema-2 settings, upgrading schema-1 files with corruption handling and pre-write backups.</summary>
public sealed class WindowsSettingsStore
{
    private const string StartupRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValueName = "Automator";
    private readonly bool _testMode;
    private readonly IApplicationLog _log;
    private readonly AutomationModuleRegistry? _moduleRegistry;
    private readonly string _settingsPath;
    private string? _desktopExecutable;
    private bool _corruptAtLoad;

    public WindowsSettingsStore(bool testMode, string? dataDirectory, IApplicationLog log, AutomationModuleRegistry? moduleRegistry = null)
    {
        _testMode = testMode;
        _log = log;
        _moduleRegistry = moduleRegistry;
        if (testMode)
        {
            if (string.IsNullOrWhiteSpace(dataDirectory) || !Path.IsPathFullyQualified(dataDirectory))
                throw new ArgumentException("Test mode requires an absolute isolated data directory.", nameof(dataDirectory));
            _settingsPath = Path.Combine(dataDirectory, "settings.json");
        }
        else
        {
            _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Automator", "settings.json");
        }
    }

    public string SettingsPath => _settingsPath;
    public string DataDirectory => Path.GetDirectoryName(_settingsPath)!;

    public SettingsLoadResult Load()
    {
        if (!File.Exists(_settingsPath))
        {
            _corruptAtLoad = false;
            return new SettingsLoadResult(new LauncherSettings(), IsValid: true, Exists: false, Error: null);
        }

        try
        {
            var json = File.ReadAllText(_settingsPath, Encoding.UTF8);
            var storedVersion = SettingsSerializer.ReadSchemaVersion(json);
            var settings = ApplyModuleMigrations(SettingsSerializer.Deserialize(json), out var moduleSettingsChanged);
            var migrated = storedVersion != LauncherSettings.CurrentSchemaVersion || moduleSettingsChanged;
            if (migrated)
            {
                WriteWithBackup(SettingsSerializer.Serialize(settings));
                _log.Write(ApplicationLogLevel.Information, "Settings.Migrated", "Saved settings were migrated to the current schema.",
                    properties: new Dictionary<string, object?> { ["fromSchemaVersion"] = storedVersion, ["toSchemaVersion"] = settings.SchemaVersion });
            }
            _corruptAtLoad = false;
            return new SettingsLoadResult(settings, IsValid: true, Exists: true, Error: null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or System.Security.SecurityException)
        {
            _corruptAtLoad = true;
            _log.Write(ApplicationLogLevel.Error, "Settings.LoadFailed", "Saved settings are invalid; defaults remain in memory and the file was left unchanged.", exception,
                new Dictionary<string, object?> { ["settingsPath"] = _settingsPath });
            return new SettingsLoadResult(new LauncherSettings(), IsValid: false, Exists: true,
                Error: "Saved settings could not be loaded. Import a valid settings file to replace them.");
        }
    }

    public void SetDesktopExecutable(string hostExecutablePath, string? portableExecutablePath)
    {
        if (!Path.IsPathFullyQualified(hostExecutablePath)) throw new InvalidDataException("The host executable path must be absolute.");
        _desktopExecutable = !string.IsNullOrWhiteSpace(portableExecutablePath)
            && Path.IsPathFullyQualified(portableExecutablePath)
            && File.Exists(portableExecutablePath)
                ? portableExecutablePath
                : hostExecutablePath;
    }

    public void Save(LauncherSettings settings, bool replacingCorruptFile = false)
    {
        var json = SettingsSerializer.Serialize(settings);
        if (_corruptAtLoad && !replacingCorruptFile)
            throw new InvalidDataException("The existing settings file is invalid. Import a valid settings file before saving changes.");

        WriteWithBackup(json);
        _corruptAtLoad = false;
    }

    public LauncherSettings Import(string path)
    {
        if (!Path.IsPathFullyQualified(path)) throw new InvalidDataException("Import path must be absolute.");
        var imported = ApplyModuleMigrations(SettingsSerializer.Deserialize(File.ReadAllText(path, Encoding.UTF8)), out _);
        WriteWithBackup(SettingsSerializer.Serialize(imported));
        _corruptAtLoad = false;
        _log.Write(ApplicationLogLevel.Information, "Settings.Imported", "Validated settings were imported.",
            properties: new Dictionary<string, object?> { ["sourcePath"] = path, ["bindingCount"] = imported.Bindings.Count });
        return imported;
    }

    /// <summary>Imports already-parsed settings from a versioned Automator backup.</summary>
    public LauncherSettings Import(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var imported = ApplyModuleMigrations(settings, out _);
        Save(imported, replacingCorruptFile: true);
        _log.Write(ApplicationLogLevel.Information, "Settings.Imported", "Validated settings were imported from an Automator backup.",
            properties: new Dictionary<string, object?> { ["bindingCount"] = imported.Bindings.Count });
        return imported;
    }

    public void Export(string path, LauncherSettings settings)
    {
        if (!Path.IsPathFullyQualified(path)) throw new InvalidDataException("Export path must be absolute.");
        var json = SettingsSerializer.Serialize(settings);
        File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        _log.Write(ApplicationLogLevel.Information, "Settings.Exported", "Settings were exported.",
            properties: new Dictionary<string, object?> { ["path"] = path, ["bindingCount"] = settings.Bindings.Count });
    }

    public void ApplyStartupSetting(bool enabled)
    {
        if (_testMode)
        {
            _log.Write(ApplicationLogLevel.Information, "Startup.WriteSkippedTestMode", "Startup registration is disabled for isolated tests.");
            return;
        }

        using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(StartupRegistryPath, writable: true);
        if (key is null) throw new IOException("Could not open the current user's startup registry key.");

        if (!enabled)
        {
            key.DeleteValue(StartupValueName, throwOnMissingValue: false);
            _log.Write(ApplicationLogLevel.Information, "Startup.Disabled", "Automator was removed from Windows startup.");
            return;
        }

        var executable = _desktopExecutable ?? throw new InvalidOperationException("The desktop executable path is not configured.");
        key.SetValue(StartupValueName, $"\"{executable}\"");
        _log.Write(ApplicationLogLevel.Information, "Startup.Enabled", "Automator was added to Windows startup.",
            properties: new Dictionary<string, object?> { ["desktopExecutable"] = executable });
    }

    private void WriteWithBackup(string json)
    {
        Directory.CreateDirectory(DataDirectory);
        var tempPath = _settingsPath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(tempPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            if (File.Exists(_settingsPath))
            {
                var backupPath = _settingsPath + ".bak-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", System.Globalization.CultureInfo.InvariantCulture);
                File.Copy(_settingsPath, backupPath, overwrite: false);
                _log.Write(ApplicationLogLevel.Information, "Settings.BackupCreated", "A settings backup was created before replacement.",
                    properties: new Dictionary<string, object?> { ["backupPath"] = backupPath });
            }

            File.Move(tempPath, _settingsPath, overwrite: true);
            _log.Write(ApplicationLogLevel.Information, "Settings.Saved", "Settings were atomically replaced.",
                properties: new Dictionary<string, object?> { ["settingsPath"] = _settingsPath });
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private LauncherSettings ApplyModuleMigrations(LauncherSettings settings, out bool changed)
    {
        changed = false;
        if (_moduleRegistry is null || settings.ModuleSettings.Count == 0) return settings;
        List<ModuleSettingsEntry> migrated;
        try
        {
            migrated = _moduleRegistry.MigrateModuleSettings(settings.ModuleSettings);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or JsonException)
        {
            throw new InvalidDataException("A module settings migration failed. The saved settings file was left unchanged.", exception);
        }
        changed = settings.ModuleSettings.Count != migrated.Count || settings.ModuleSettings.Where((entry, index) =>
            entry.ModuleId != migrated[index].ModuleId || entry.SchemaVersion != migrated[index].SchemaVersion
            || entry.Value.GetRawText() != migrated[index].Value.GetRawText()).Any();
        return changed ? settings with { ModuleSettings = migrated } : settings;
    }
}
