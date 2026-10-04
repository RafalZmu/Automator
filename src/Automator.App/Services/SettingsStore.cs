using System.Reflection;
using Automator.Core.Configuration;
using Microsoft.Win32;

namespace Automator.Services;

public sealed class SettingsStore
{
    private const string StartupRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValueName = "Automator";
    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Automator",
        "settings.json");

    public LauncherSettings Load()
    {
        try
        {
            return File.Exists(_settingsPath)
                ? SettingsSerializer.Deserialize(File.ReadAllText(_settingsPath))
                : new LauncherSettings();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or System.Text.Json.JsonException)
        {
            AppLogger.Warning("Settings.LoadFailed", "Could not read saved settings; defaults will be used.", exception,
                new Dictionary<string, object?> { ["settingsPath"] = _settingsPath });
            return new LauncherSettings();
        }
    }

    public void Save(LauncherSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            File.WriteAllText(_settingsPath, SettingsSerializer.Serialize(settings));
            AppLogger.Info("Settings.Saved", "Application settings were saved.", new Dictionary<string, object?>
            {
                ["settingsPath"] = _settingsPath,
                ["bindingCount"] = settings.Bindings.Count
            });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            AppLogger.Error("Settings.SaveFailed", "Could not write application settings.", exception,
                new Dictionary<string, object?> { ["settingsPath"] = _settingsPath });
            throw;
        }
    }

    public void Export(string path, LauncherSettings settings)
    {
        try
        {
            File.WriteAllText(path, SettingsSerializer.Serialize(settings));
            AppLogger.Info("Settings.Exported", "Settings were exported.", new Dictionary<string, object?>
            {
                ["path"] = path,
                ["bindingCount"] = settings.Bindings.Count
            });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            AppLogger.Error("Settings.ExportFailed", "Could not export settings.", exception,
                new Dictionary<string, object?> { ["path"] = path });
            throw;
        }
    }

    public LauncherSettings Import(string path)
    {
        try
        {
            var settings = SettingsSerializer.Deserialize(File.ReadAllText(path));
            AppLogger.Info("Settings.Imported", "Settings were imported.", new Dictionary<string, object?>
            {
                ["path"] = path,
                ["bindingCount"] = settings.Bindings.Count
            });
            return settings;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or System.Text.Json.JsonException)
        {
            AppLogger.Error("Settings.ImportFailed", "Could not import settings.", exception,
                new Dictionary<string, object?> { ["path"] = path });
            throw;
        }
    }

    public IReadOnlyList<string> FindMissingPaths(LauncherSettings settings) =>
        SettingsSerializer.FindMissingPaths(settings, File.Exists);

    public void ApplyStartupSetting(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(StartupRegistryPath, writable: true);
        if (key is null)
        {
            AppLogger.Warning("Startup.RegistryUnavailable", "Could not open the current user's startup registry key.");
            return;
        }

        if (!enabled)
        {
            key.DeleteValue(StartupValueName, throwOnMissingValue: false);
            AppLogger.Info("Startup.Disabled", "Automator was removed from Windows startup.");
            return;
        }

        key.SetValue(StartupValueName, GetStartupCommand());
        AppLogger.Info("Startup.Enabled", "Automator was added to Windows startup.");
    }

    private static string GetStartupCommand()
    {
        var processPath = Environment.ProcessPath ?? throw new InvalidOperationException("The application path could not be determined.");
        var entryAssembly = Assembly.GetEntryAssembly()?.Location;
        if (Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(entryAssembly))
        {
            return $"\"{processPath}\" \"{entryAssembly}\"";
        }

        return $"\"{processPath}\"";
    }
}
