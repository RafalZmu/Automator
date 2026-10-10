using Automator.Application.Launcher;
using Automator.Application.Automation;
using Automator.Application.Logging;
using Automator.Core.Automation;
using Automator.Core.Configuration;
using Automator.Core.Plugins;
using Automator.Windows;

var checks = new (string Name, Action Run)[]
{
    ("keyboard normalizer emits stable key codes and modifier sides", NormalizerMapsStableKeysAndModifiers),
    ("keyboard normalizer tracks transitions repeats and system-key messages", NormalizerTracksTransitionsAndSystemKeys),
    ("hook reset clears held modifier state", NormalizerResetClearsHeldModifiers),
    ("keyboard normalization leaves launcher recording decisions intact", RecordingDecisionStillRequiresTrustedContext),
    ("closing a recording context disarms modifier capture", RecordingCanBeCancelled),
    ("invalid app paths return no icon instead of breaking backend state", InvalidIconPathIsIgnored),
    ("schema 1 settings load upgrades atomically while preserving launcher fields", LegacySettingsUpgradeIsAtomic),
    ("failed module settings migration leaves the saved file untouched", FailedModuleMigrationPreservesSource),
    ("website launcher redirects direct browser output", WebsiteLauncherRedirectsDirectBrowserOutput),
    ("website launcher capability is module scoped and opens only HTTP URLs in group order", WebsiteLauncherOpensValidatedGroups),
};

static void NormalizerMapsStableKeysAndModifiers()
{
    var normalizer = new KeyboardInputNormalizer();
    var letter = normalizer.Normalize(0x41, 0, 0, isDown: true, isRepeat: false, sequence: 1);
    var digit = normalizer.Normalize(0x31, 0, 0, isDown: true, isRepeat: false, sequence: 2);
    var leftShift = normalizer.Normalize(0x10, 0x2A, 0, isDown: true, isRepeat: false, sequence: 3);
    var rightShift = normalizer.Normalize(0x10, 0x36, 0, isDown: true, isRepeat: false, sequence: 4);
    var leftControl = normalizer.Normalize(0x11, 0x1D, 0, isDown: true, isRepeat: false, sequence: 5);
    var rightControl = normalizer.Normalize(0x11, 0x1D, 0x01, isDown: true, isRepeat: false, sequence: 6);
    var leftAlt = normalizer.Normalize(0x12, 0x38, 0, isDown: true, isRepeat: false, sequence: 7);
    var rightAlt = normalizer.Normalize(0x12, 0x38, 0x01, isDown: true, isRepeat: false, sequence: 8);
    var leftMeta = normalizer.Normalize(0x5B, 0x5B, 0, isDown: true, isRepeat: false, sequence: 9);
    var rightMeta = normalizer.Normalize(0x5C, 0x5C, 0x01, isDown: true, isRepeat: false, sequence: 10);

    Check.Equal("KeyA", letter.Code);
    Check.Equal("Digit1", digit.Code);
    Check.Equal("ShiftLeft", leftShift.Code);
    Check.True(leftShift.Modifiers.HasFlag(AutomationKeyModifiers.Shift));
    Check.Equal("ShiftRight", rightShift.Code);
    Check.Equal("ControlLeft", leftControl.Code);
    Check.Equal("ControlRight", rightControl.Code);
    Check.True(rightControl.Modifiers.HasFlag(AutomationKeyModifiers.Control));
    Check.True(rightControl.Modifiers.HasFlag(AutomationKeyModifiers.Shift));
    Check.Equal("AltLeft", leftAlt.Code);
    Check.Equal("AltRight", rightAlt.Code);
    Check.True(rightAlt.Modifiers.HasFlag(AutomationKeyModifiers.Alt));
    Check.Equal("MetaLeft", leftMeta.Code);
    Check.Equal("MetaRight", rightMeta.Code);
    Check.True(rightMeta.Modifiers.HasFlag(AutomationKeyModifiers.Meta));
}

static void NormalizerTracksTransitionsAndSystemKeys()
{
    var normalizer = new KeyboardInputNormalizer();
    var down = normalizer.Normalize(0x41, 0x1E, 0, isDown: true, isRepeat: false, sequence: 10);
    var repeat = normalizer.Normalize(0x41, 0x1E, 0, isDown: true, isRepeat: true, sequence: 11);
    var up = normalizer.Normalize(0x41, 0x1E, 0, isDown: false, isRepeat: false, sequence: 12);

    Check.True(down.IsDown);
    Check.False(down.IsRepeat);
    Check.True(repeat.IsDown);
    Check.True(repeat.IsRepeat);
    Check.False(up.IsDown);
    Check.Equal(12L, up.Sequence);
    Check.True(KeyboardInputNormalizer.TryGetTransition(0x0104, out var systemDown));
    Check.True(systemDown);
    Check.True(KeyboardInputNormalizer.TryGetTransition(0x0105, out var systemUp));
    Check.False(systemUp);
    Check.False(KeyboardInputNormalizer.TryGetTransition(0x0200, out _));
}

static void NormalizerResetClearsHeldModifiers()
{
    var normalizer = new KeyboardInputNormalizer();
    _ = normalizer.Normalize(0x11, 0x1D, 0, isDown: true, isRepeat: false, sequence: 1);
    normalizer.ClearHeldKeys();
    var letter = normalizer.Normalize(0x41, 0x1E, 0, isDown: true, isRepeat: false, sequence: 2);
    Check.Equal(AutomationKeyModifiers.None, letter.Modifiers);
}

static void RecordingDecisionStillRequiresTrustedContext()
{
    var recording = new LauncherNavigationState(true, 1, LauncherMode.RecordingHotkey);
    Check.False(HotkeyRecordingPolicy.ShouldCapture(recording, windowContextVisible: false, rendererFocused: true,
        nativeDialogActive: false, nativeForeground: true, contextModeMatches: true));
    Check.False(HotkeyRecordingPolicy.ShouldCapture(recording, windowContextVisible: true, rendererFocused: true,
        nativeDialogActive: true, nativeForeground: true, contextModeMatches: true));
    Check.True(HotkeyRecordingPolicy.ShouldCapture(recording, windowContextVisible: true, rendererFocused: true,
        nativeDialogActive: false, nativeForeground: true, contextModeMatches: true));
}

var failures = 0;
foreach (var (name, run) in checks)
{
    try
    {
        run();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.WriteLine($"FAIL {name}: {exception.Message}");
    }
}

Console.WriteLine($"{checks.Length - failures}/{checks.Length} checks passed");
return failures == 0 ? 0 : 1;

static void RecordingCanBeCancelled()
{
    using var hook = new KeyboardHookService(
        "RightControl",
        () => new LauncherNavigationState(false, 1, LauncherMode.Launcher),
        () => new LauncherWindowContext(IntPtr.Zero, false, false, false, "launcher"));
    hook.StartRecording();
    Check.True(hook.IsRecording);
    hook.CancelRecording();
    Check.False(hook.IsRecording);
}

static void InvalidIconPathIsIgnored()
{
    var icon = new WindowsIconCache().GetDataUrl("\0bad-path.exe");
    Check.Equal<string?>(null, icon);
}

static void LegacySettingsUpgradeIsAtomic()
{
    var dataDirectory = Path.Combine(Path.GetTempPath(), "automator-settings-spec-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(dataDirectory);
    try
    {
        var settingsPath = Path.Combine(dataDirectory, "settings.json");
        File.WriteAllText(settingsPath,
            "{\"SchemaVersion\":1,\"Hotkey\":\"RightAlt\",\"Theme\":\"Dark\",\"StartWithWindows\":false,\"Bindings\":[{\"Id\":\"codex\",\"Name\":\"Codex\",\"TargetPath\":\"C:\\\\Apps\\\\Codex.exe\",\"Alias\":\"cx\",\"Arguments\":\"--profile work\"}]}");
        var store = new WindowsSettingsStore(true, dataDirectory, NullApplicationLog.Instance);

        var loaded = store.Load();

        Check.True(loaded.IsValid);
        Check.Equal(2, loaded.Settings.SchemaVersion);
        Check.Equal("RightAlt", loaded.Settings.Hotkey);
        Check.Equal(AppTheme.Dark, loaded.Settings.Theme);
        Check.Equal(false, loaded.Settings.StartWithWindows);
        Check.Equal("--profile work", loaded.Settings.Bindings[0].Arguments);
        Check.Equal(2, SettingsSerializer.ReadSchemaVersion(File.ReadAllText(settingsPath)));
        Check.Equal(1, Directory.GetFiles(dataDirectory, "settings.json.bak-*").Length);
    }
    finally { Directory.Delete(dataDirectory, recursive: true); }
}

static void FailedModuleMigrationPreservesSource()
{
    var dataDirectory = Path.Combine(Path.GetTempPath(), "automator-settings-spec-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(dataDirectory);
    try
    {
        var settingsPath = Path.Combine(dataDirectory, "settings.json");
        const string original = "{\"SchemaVersion\":2,\"Hotkey\":\"RightControl\",\"Theme\":\"Light\",\"StartWithWindows\":true,\"Bindings\":[],\"ModuleSettings\":[{\"ModuleId\":\"failing-module\",\"SchemaVersion\":1,\"Value\":{\"old\":true}}]}";
        File.WriteAllText(settingsPath, original);
        var capabilities = new AutomationCapabilityRegistry();
        var registry = new AutomationModuleRegistry([new FailingSettingsModule()], capabilities);
        var store = new WindowsSettingsStore(true, dataDirectory, NullApplicationLog.Instance, registry);

        var loaded = store.Load();

        Check.False(loaded.IsValid);
        Check.True(loaded.Error?.Contains("could not be loaded", StringComparison.OrdinalIgnoreCase) == true);
        Check.Equal(original, File.ReadAllText(settingsPath));
        Check.Equal(0, Directory.GetFiles(dataDirectory, "settings.json.bak-*").Length);
    }
    finally { Directory.Delete(dataDirectory, recursive: true); }
}

static void WebsiteLauncherRedirectsDirectBrowserOutput()
{
    var starts = new List<System.Diagnostics.ProcessStartInfo>();
    var adapter = new WindowsAutomationWebsiteLauncher(starts.Add, "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe");

    adapter.LaunchAsync([[new Uri("https://example.test")]], CancellationToken.None).GetAwaiter().GetResult();

    Check.Equal(1, starts.Count);
    Check.False(starts[0].UseShellExecute);
    Check.True(starts[0].RedirectStandardOutput);
    Check.True(starts[0].RedirectStandardError);
}

static void WebsiteLauncherOpensValidatedGroups()
{
    var starts = new List<System.Diagnostics.ProcessStartInfo>();
    var adapter = new WindowsAutomationWebsiteLauncher(starts.Add, "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe");
    var registry = new AutomationCapabilityRegistry(websiteLauncherFactory: _ => adapter);
    var grants = new[] { new AutomationCapabilityRequirement(AutomationCapabilityIds.WebsiteLaunch, 1) };
    using var cancellation = new CancellationTokenSource();
    var context = registry.CreateContext(new AutomationModuleDescriptor("website-launcher", grants));
    Check.True(context.HasCapability(AutomationCapabilityIds.WebsiteLaunch));
    Check.True(context.WebsiteLauncher is not null);

    context.WebsiteLauncher!.LaunchAsync(
        [
            [new Uri("https://example.test/one"), new Uri("http://example.test/two")],
            [new Uri("https://example.test/three")],
        ], cancellation.Token).GetAwaiter().GetResult();

    Check.Equal(2, starts.Count);
    Check.Equal("C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe", starts[0].FileName);
    Check.Equal("--new-window", starts[0].ArgumentList[0]);
    Check.Equal("https://example.test/one", starts[0].ArgumentList[1]);
    Check.Equal("http://example.test/two", starts[0].ArgumentList[2]);
    Check.Equal("--new-window", starts[1].ArgumentList[0]);
    Check.Equal("https://example.test/three", starts[1].ArgumentList[1]);
    Check.True(starts.All(start => !start.UseShellExecute));
    Check.True(starts.All(start => start.ArgumentList.Count > 0));
    Check.Throws<ArgumentException>(() => adapter.LaunchAsync(
        [[new Uri("file:///C:/secret.txt")]], CancellationToken.None).GetAwaiter().GetResult());
    Check.Throws<ArgumentException>(() => adapter.LaunchAsync(
        [[new Uri("javascript:alert(1)")]], CancellationToken.None).GetAwaiter().GetResult());
    Check.Equal(2, starts.Count);
    Check.Throws<ArgumentException>(() => adapter.LaunchAsync(
        [[new Uri("https://example.test/valid"), new Uri("file:///C:/secret.txt")]], CancellationToken.None).GetAwaiter().GetResult());
    Check.Equal(2, starts.Count);

    starts.Clear();
    var firefox = new WindowsAutomationWebsiteLauncher(starts.Add, "C:\\Program Files\\Mozilla Firefox\\firefox.exe");
    firefox.LaunchAsync(
        [[new Uri("https://example.test/one"), new Uri("https://example.test/two")], [new Uri("https://example.test/three")]],
        CancellationToken.None).GetAwaiter().GetResult();
    Check.Equal(2, starts.Count);
    Check.Equal("-new-window", starts[0].ArgumentList[0]);
    Check.Equal("https://example.test/one", starts[0].ArgumentList[1]);
    Check.Equal("-new-tab", starts[0].ArgumentList[2]);
    Check.Equal("https://example.test/two", starts[0].ArgumentList[3]);
    Check.Equal("-new-window", starts[1].ArgumentList[0]);
    Check.Equal("https://example.test/three", starts[1].ArgumentList[1]);
    Check.True(starts.All(start => !start.UseShellExecute && start.RedirectStandardOutput && start.RedirectStandardError));

    starts.Clear();
    var unknown = new WindowsAutomationWebsiteLauncher(starts.Add, "C:\\Program Files\\UnknownBrowser\\browser.exe");
    unknown.LaunchAsync(
        [[new Uri("https://example.test/one"), new Uri("https://example.test/two")], [new Uri("https://example.test/three")]],
        CancellationToken.None).GetAwaiter().GetResult();
    Check.Equal(3, starts.Count);
    Check.Equal("https://example.test/one", starts[0].FileName);
    Check.Equal("https://example.test/two", starts[1].FileName);
    Check.Equal("https://example.test/three", starts[2].FileName);
    Check.True(starts.All(start => start.UseShellExecute));
    Check.True(starts.All(start => !start.RedirectStandardOutput && !start.RedirectStandardError));

    context.DisposeAsync().AsTask().GetAwaiter().GetResult();
    Check.Throws<ObjectDisposedException>(() => context.WebsiteLauncher!.LaunchAsync(
        [[new Uri("https://example.test")]], CancellationToken.None).GetAwaiter().GetResult());
}

static class Check
{
    public static void True(bool actual)
    {
        if (!actual) throw new InvalidOperationException("Expected true.");
    }

    public static void False(bool actual)
    {
        if (actual) throw new InvalidOperationException("Expected false.");
    }

    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
    }

    public static void Throws<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }
}

sealed class FailingSettingsModule : IAutomationModule
{
    public AutomationModuleDefinition Definition { get; } = new(
        2, "failing-module", "Failing", "grid", "reserved", false, 1, 2, [], []);
    public int ContractVersion => Definition.ContractVersion;
    public string Id => Definition.Id;
    public string Title => Definition.Title;
    public System.Text.Json.JsonElement CreateDefaultSettings() => System.Text.Json.JsonDocument.Parse("{}").RootElement.Clone();
    public System.Text.Json.JsonElement MigrateSettings(int fromVersion, System.Text.Json.JsonElement value) =>
        throw new InvalidOperationException("intentional migration failure");
    public ValueTask<AutomationResult> ExecuteAsync(string actionId, System.Text.Json.JsonElement input,
        System.Text.Json.JsonElement moduleSettings,
        AutomationServicesContext services, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
