using System.Runtime.InteropServices;
using Automator.Application.Automation;
using Microsoft.Win32;

namespace Automator.Windows;

/// <summary>Registry operations relative to the current-user hive; injectable so specs never touch real associations.</summary>
public interface IFileExplorerRegistryStore
{
    string[] SubKeys(string path);
    string[] ValueNames(string path);
    string? Read(string path, string name);
    bool Exists(string path);
    void Write(string path, string name, string value);
    void DeleteValue(string path, string name);
    void DeleteEmptyKey(string path);
}

/// <summary>Per-user classic menu registration without replacing file associations.</summary>
public sealed class WindowsAutomationFileExplorerMenu : IAutomationFileExplorerMenu
{
    private const string Root = @"Software\Classes\SystemFileAssociations";
    private const string MenuKey = "Automator.ScriptRunner";
    private const string OwnerName = "AutomatorOwner";
    private const string OwnerValue = "script-runner.explorer.v1";
    private readonly IFileExplorerRegistryStore _store;
    private readonly Action _notify;
    private readonly bool _enabled;
    private readonly object _gate = new();

    public WindowsAutomationFileExplorerMenu(bool enabled) : this(new CurrentUserRegistryStore(), NotifyExplorer, enabled) { }

    public WindowsAutomationFileExplorerMenu(IFileExplorerRegistryStore store, Action notify, bool enabled = true)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _notify = notify ?? throw new ArgumentNullException(nameof(notify));
        _enabled = enabled;
    }

    public static bool IsRegistrationEnabled(string? executablePath, bool testMode, bool portable) =>
        !testMode && !portable && !string.IsNullOrWhiteSpace(executablePath)
        && Path.IsPathFullyQualified(executablePath)
        && Path.GetFileName(executablePath).Equals("Automator.exe", StringComparison.OrdinalIgnoreCase);

    public Task ReconcileAsync(IReadOnlyList<ExplorerActionDefinition> entries, string executablePath, CancellationToken cancellationToken)
    {
        // This check precedes every registry operation, including cleanup, for isolated hosts.
        if (!_enabled) return Task.CompletedTask;
        ArgumentNullException.ThrowIfNull(entries);
        if (!Path.IsPathFullyQualified(executablePath) || !executablePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            || executablePath.Any(character => char.IsControl(character) || character is '"' or '%'))
            throw new ArgumentException("Explorer menu requires an absolute executable path.", nameof(executablePath));
        foreach (var entry in entries) ScriptRunnerModule.ValidateExplorerAction(entry);
        if (entries.Select(entry => entry.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != entries.Count)
            throw new ArgumentException("Explorer action IDs must be unique.", nameof(entries));
        var groups = entries.SelectMany(entry => entry.Extensions.Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(extension => (Extension: extension.ToLowerInvariant(), Entry: entry)))
            .GroupBy(item => item.Extension, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Entry).OrderBy(entry => entry.Id, StringComparer.Ordinal).ToArray(), StringComparer.OrdinalIgnoreCase);
        lock (_gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Detect collisions before writing anything. A matching key name alone never proves ownership.
            foreach (var (extension, actions) in groups)
            {
                var parent = Parent(extension);
                RequireOwnedOrAbsent(parent);
                foreach (var action in actions)
                {
                    RequireOwnedOrAbsent(parent + @"\shell\" + action.Id);
                    RequireOwnedOrAbsent(parent + @"\shell\" + action.Id + @"\command");
                }
            }
            var changed = false;
            try
            {
                foreach (var extension in _store.SubKeys(Root))
                {
                    var parent = Parent(extension);
                    if (!Owned(parent)) continue;
                    if (!groups.TryGetValue(extension, out var actions))
                    {
                        foreach (var child in _store.SubKeys(parent + @"\shell")) RemoveAction(parent + @"\shell\" + child, ref changed);
                        foreach (var name in new[] { "MUIVerb", "SubCommands", "MultiSelectModel" }) RemoveValue(parent, name, ref changed);
                        Prune(parent + @"\shell", ref changed);
                        ReleaseEmptyOwnedKey(parent, ref changed);
                        continue;
                    }
                    foreach (var child in _store.SubKeys(parent + @"\shell"))
                        if (!actions.Any(action => action.Id.Equals(child, StringComparison.OrdinalIgnoreCase)))
                            RemoveAction(parent + @"\shell\" + child, ref changed);
                }
                foreach (var (extension, actions) in groups)
                {
                    var parent = Parent(extension);
                    Set(parent, OwnerName, OwnerValue, ref changed);
                    Set(parent, "MUIVerb", "Automator", ref changed);
                    // Empty SubCommands uses the verbs directly beneath this menu's shell key.
                    Set(parent, "SubCommands", "", ref changed);
                    Set(parent, "MultiSelectModel", "Single", ref changed);
                    foreach (var action in actions)
                    {
                        var child = parent + @"\shell\" + action.Id;
                        Set(child, OwnerName, OwnerValue, ref changed);
                        Set(child, "MUIVerb", action.Label, ref changed);
                        Set(child, "MultiSelectModel", "Single", ref changed);
                        Set(child + @"\command", OwnerName, OwnerValue, ref changed);
                        Set(child + @"\command", "", $"\"{executablePath}\" --automator-file-action \"{action.Id}\" -- \"%1\"", ref changed);
                    }
                }
            }
            finally { if (changed) _notify(); }
        }
        return Task.CompletedTask;
    }

    private static string Parent(string extension) => Root + "\\" + extension + @"\shell\" + MenuKey;
    private bool Owned(string path) => _store.Read(path, OwnerName) == OwnerValue;
    private void RequireOwnedOrAbsent(string path)
    {
        if (_store.Exists(path) && !Owned(path)) throw new InvalidOperationException("An Explorer menu key is owned by another application. Existing entries were preserved.");
    }
    private void Set(string path, string name, string value, ref bool changed)
    {
        if (_store.Read(path, name) == value) return;
        _store.Write(path, name, value);
        changed = true;
    }
    private void RemoveValue(string path, string name, ref bool changed)
    {
        if (!_store.ValueNames(path).Contains(name, StringComparer.OrdinalIgnoreCase)) return;
        _store.DeleteValue(path, name);
        changed = true;
    }
    private void Prune(string path, ref bool changed)
    {
        if (!_store.Exists(path) || _store.ValueNames(path).Length != 0 || _store.SubKeys(path).Length != 0) return;
        _store.DeleteEmptyKey(path);
        changed = true;
    }
    private void ReleaseEmptyOwnedKey(string path, ref bool changed)
    {
        // Keep proof of ownership when unrelated values or subkeys prevent removing the key.
        // This lets a later mapping safely reuse it without claiming a foreign key.
        if (_store.ValueNames(path).Any(name => !name.Equals(OwnerName, StringComparison.OrdinalIgnoreCase)) || _store.SubKeys(path).Length != 0) return;
        RemoveValue(path, OwnerName, ref changed);
        Prune(path, ref changed);
    }
    private void RemoveAction(string path, ref bool changed)
    {
        if (!Owned(path)) return;
        var command = path + @"\command";
        if (Owned(command))
        {
            RemoveValue(command, "", ref changed);
            RemoveValue(command, OwnerName, ref changed);
            Prune(command, ref changed);
        }
        foreach (var name in new[] { "MUIVerb", "MultiSelectModel" }) RemoveValue(path, name, ref changed);
        ReleaseEmptyOwnedKey(path, ref changed);
    }

    private static void NotifyExplorer() => SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint eventId, uint flags, IntPtr item1, IntPtr item2);

    private sealed class CurrentUserRegistryStore : IFileExplorerRegistryStore
    {
        private static void ValidatePath(string path)
        {
            if (path != Root && !path.StartsWith(Root + "\\", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Explorer menu registry path is outside SystemFileAssociations.", nameof(path));
        }
        public string[] SubKeys(string path) { ValidatePath(path); using var key = Registry.CurrentUser.OpenSubKey(path); return key?.GetSubKeyNames() ?? []; }
        public string[] ValueNames(string path) { ValidatePath(path); using var key = Registry.CurrentUser.OpenSubKey(path); return key?.GetValueNames() ?? []; }
        public string? Read(string path, string name) { ValidatePath(path); using var key = Registry.CurrentUser.OpenSubKey(path); return key?.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string; }
        public bool Exists(string path) { ValidatePath(path); using var key = Registry.CurrentUser.OpenSubKey(path); return key is not null; }
        public void Write(string path, string name, string value) { ValidatePath(path); using var key = Registry.CurrentUser.CreateSubKey(path); key.SetValue(name, value, RegistryValueKind.String); }
        public void DeleteValue(string path, string name) { ValidatePath(path); using var key = Registry.CurrentUser.OpenSubKey(path, writable: true); key?.DeleteValue(name, throwOnMissingValue: false); }
        public void DeleteEmptyKey(string path)
        {
            ValidatePath(path);
            using (var key = Registry.CurrentUser.OpenSubKey(path))
                if (key is null || key.ValueCount != 0 || key.SubKeyCount != 0) return;
            Registry.CurrentUser.DeleteSubKey(path, throwOnMissingSubKey: false);
        }
    }
}
