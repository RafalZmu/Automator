using Automator.Application.Automation;
using Automator.Windows;

static class FileExplorerMenuSpecs
{
    public static void ReconcilesOwnedMenus()
    {
        var store = new RegistryStore();
        var notifications = 0;
        var adapter = new WindowsAutomationFileExplorerMenu(store, () => notifications++);
        const string root = @"Software\Classes\SystemFileAssociations";
        const string fdb = root + @"\.fdb\shell\Automator.ScriptRunner";
        const string txt = root + @"\.txt\shell\Automator.ScriptRunner";
        store.Write(root + @"\.fdb", "", "Existing.ProgId");
        store.Write(root + @"\.fdb\shell\Foreign", "", "foreign menu");
        ExplorerActionDefinition[] actions = [new("backup", "profile", "Backup", [".FDB", ".txt"]), new("zip", "profile", "Backup and ZIP", [".fdb"])];
        adapter.ReconcileAsync(actions, @"C:\Program Files\Automator\Automator.exe", default).GetAwaiter().GetResult();
        Check.Equal("Automator", store.Read(fdb, "MUIVerb"));
        Check.Equal("", store.Read(fdb, "SubCommands"));
        Check.Equal("Single", store.Read(fdb, "MultiSelectModel"));
        Check.Equal("Backup", store.Read(fdb + @"\shell\backup", "MUIVerb"));
        Check.Equal("Backup and ZIP", store.Read(fdb + @"\shell\zip", "MUIVerb"));
        Check.Equal("Backup", store.Read(txt + @"\shell\backup", "MUIVerb"));
        Check.Equal("\"C:\\Program Files\\Automator\\Automator.exe\" --automator-file-action \"backup\" -- \"%1\"", store.Read(fdb + @"\shell\backup\command", ""));
        Check.Equal(1, notifications);
        adapter.ReconcileAsync(actions, @"C:\Program Files\Automator\Automator.exe", default).GetAwaiter().GetResult();
        Check.Equal(1, notifications);
        store.Write(fdb, "ForeignValue", "preserve");
        store.Write(fdb + @"\shell\backup", "ForeignValue", "preserve-child");
        store.Write(fdb + @"\shell\backup\command", "ForeignValue", "preserve-command-value");
        store.Write(fdb + @"\shell\backup\command\Foreign", "", "preserve-command-child");
        store.Write(fdb + @"\shell\Foreign", "", "preserve-key");
        adapter.ReconcileAsync([actions[1] with { Label = "ZIP updated", Extensions = [".fdb"] }], @"C:\Automator\Automator.exe", default).GetAwaiter().GetResult();
        Check.Equal("ZIP updated", store.Read(fdb + @"\shell\zip", "MUIVerb"));
        Check.False(store.Exists(txt));
        Check.Equal("preserve-child", store.Read(fdb + @"\shell\backup", "ForeignValue"));
        Check.Equal<string?>(null, store.Read(fdb + @"\shell\backup", "MUIVerb"));
        Check.Equal<string?>(null, store.Read(fdb + @"\shell\backup\command", ""));
        Check.Equal("preserve-command-value", store.Read(fdb + @"\shell\backup\command", "ForeignValue"));
        Check.Equal("preserve-command-child", store.Read(fdb + @"\shell\backup\command\Foreign", ""));
        Check.Equal("script-runner.explorer.v1", store.Read(fdb + @"\shell\backup\command", "AutomatorOwner"));
        adapter.ReconcileAsync([], @"C:\Automator\Automator.exe", default).GetAwaiter().GetResult();
        Check.Equal<string?>(null, store.Read(fdb, "MUIVerb"));
        Check.Equal("preserve", store.Read(fdb, "ForeignValue"));
        Check.Equal("preserve-key", store.Read(fdb + @"\shell\Foreign", ""));
        Check.Equal("Existing.ProgId", store.Read(root + @"\.fdb", ""));
        Check.Equal("foreign menu", store.Read(root + @"\.fdb\shell\Foreign", ""));
        Check.Equal(3, notifications);
        adapter.ReconcileAsync([actions[0] with { Extensions = [".fdb"] }], @"C:\Automator\Automator.exe", default).GetAwaiter().GetResult();
        Check.Equal("Backup", store.Read(fdb + @"\shell\backup", "MUIVerb"));
        Check.Equal("preserve-child", store.Read(fdb + @"\shell\backup", "ForeignValue"));
        Check.Equal("preserve-command-value", store.Read(fdb + @"\shell\backup\command", "ForeignValue"));
        Check.Equal("preserve-command-child", store.Read(fdb + @"\shell\backup\command\Foreign", ""));
        Check.Equal("\"C:\\Automator\\Automator.exe\" --automator-file-action \"backup\" -- \"%1\"", store.Read(fdb + @"\shell\backup\command", ""));
        Check.Equal(4, notifications);
    }

    public static void DisabledHostsAndCollisionsDoNotWrite()
    {
        var store = new RegistryStore();
        var notifications = 0;
        ExplorerActionDefinition[] actions = [new("backup", "profile", "Backup", [".fdb"])];
        Check.False(WindowsAutomationFileExplorerMenu.IsRegistrationEnabled(@"C:\Automator\Automator.exe", testMode: true, portable: false));
        Check.False(WindowsAutomationFileExplorerMenu.IsRegistrationEnabled(@"C:\Automator\Automator.exe", testMode: false, portable: true));
        Check.False(WindowsAutomationFileExplorerMenu.IsRegistrationEnabled(null, testMode: false, portable: false));
        Check.False(WindowsAutomationFileExplorerMenu.IsRegistrationEnabled(@"C:\node\electron.exe", testMode: false, portable: false));
        Check.True(WindowsAutomationFileExplorerMenu.IsRegistrationEnabled(@"C:\Automator\Automator.exe", testMode: false, portable: false));
        var disabled = new WindowsAutomationFileExplorerMenu(store, () => notifications++, enabled: false);
        disabled.ReconcileAsync(actions, @"C:\Automator\Automator.exe", default).GetAwaiter().GetResult();
        Check.Equal(0, store.Keys.Count);
        Check.Equal(0, notifications);
        var adapter = new WindowsAutomationFileExplorerMenu(store, () => notifications++);
        Check.Throws<ArgumentException>(() => adapter.ReconcileAsync(actions, "relative.exe", default).GetAwaiter().GetResult());
        Check.Equal(0, store.Keys.Count);
        const string collision = @"Software\Classes\SystemFileAssociations\.fdb\shell\Automator.ScriptRunner";
        store.Write(collision, "MUIVerb", "Other owner's menu");
        Check.Throws<InvalidOperationException>(() => adapter.ReconcileAsync(actions, @"C:\Automator\Automator.exe", default).GetAwaiter().GetResult());
        Check.Equal("Other owner's menu", store.Read(collision, "MUIVerb"));
        Check.Equal(0, notifications);
    }

    private sealed class RegistryStore : IFileExplorerRegistryStore
    {
        public Dictionary<string, Dictionary<string, string>> Keys { get; } = new(StringComparer.OrdinalIgnoreCase);
        public string[] SubKeys(string path) => Keys.Keys.Where(key => key.StartsWith(path + "\\", StringComparison.OrdinalIgnoreCase))
            .Select(key => key[(path.Length + 1)..].Split('\\')[0]).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        public string[] ValueNames(string path) => Keys.TryGetValue(path, out var values) ? values.Keys.ToArray() : [];
        public string? Read(string path, string name) => Keys.TryGetValue(path, out var values) && values.TryGetValue(name, out var value) ? value : null;
        public bool Exists(string path) => Keys.ContainsKey(path);
        public void Write(string path, string name, string value)
        {
            var parts = path.Split('\\');
            for (var count = 1; count <= parts.Length; count++) Keys.TryAdd(string.Join("\\", parts.Take(count)), new(StringComparer.OrdinalIgnoreCase));
            Keys[path][name] = value;
        }
        public void DeleteValue(string path, string name) { if (Keys.TryGetValue(path, out var values)) values.Remove(name); }
        public void DeleteEmptyKey(string path) { if (ValueNames(path).Length == 0 && SubKeys(path).Length == 0) Keys.Remove(path); }
    }
}
