using Automator.Core.Configuration;
using Automator.Core.Launcher;
using Automator.Core.Plugins;

var checks = new (string Name, Action Run)[]
{
    ("tab keys select slots 1 through 9", TabKeysSelectSlots),
    ("the opener reacts to a solo key tap but ignores chords", OpenerRecognizesSoloTaps),
    ("launcher aliases resolve exact and partial matches", AliasesResolve),
    ("aliases accept letters and reject duplicates", AliasesValidate),
    ("launcher toggles the active app and restores others", LauncherToggles),
    ("configuration round-trips and flags missing app paths", ConfigurationRoundTrips),
    ("configuration rejects undefined themes and unsupported opener keys", InvalidThemeAndHotkeyRejected),
    ("configured opener names map consistently to Windows virtual keys", OpenerNamesMapToVirtualKeys),
    ("configuration rejects null bindings and duplicate identifiers", InvalidBindingsRejected),
    ("configuration wraps malformed imported JSON as structured data error", MalformedSettingsAreStructured),
    ("automation results preserve structured output and actions", AutomationResultsKeepOutput),
    ("schema 1 settings migrate without dropping launcher preferences", SchemaOneSettingsMigrate),
    ("unknown module settings survive settings round trips", UnknownModuleSettingsRoundTrip),
    ("module settings reject duplicate ids and oversized payloads", InvalidModuleSettingsRejected)
};

var failed = 0;
foreach (var (name, run) in checks)
{
    try
    {
        run();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {name}: {exception.Message}");
    }
}

Console.WriteLine($"{checks.Length - failed}/{checks.Length} checks passed");
return failed == 0 ? 0 : 1;

static void TabKeysSelectSlots()
{
    Check.Equal(0, TabKeyParser.Parse('1'));
    Check.Equal(8, TabKeyParser.Parse('9'));
    Check.Equal<int?>(null, TabKeyParser.Parse('0'));
    Check.Equal<int?>(null, TabKeyParser.Parse('a'));
}

static void OpenerRecognizesSoloTaps()
{
    var recognizer = new HotkeyTapRecognizer("RightControl");
    Check.Equal(false, recognizer.KeyDown("RightControl"));
    Check.Equal(true, recognizer.KeyUp("RightControl"));

    Check.Equal(false, recognizer.KeyDown("RightControl"));
    Check.Equal(false, recognizer.KeyDown("C"));
    Check.Equal(false, recognizer.KeyUp("RightControl"));
}

static void AliasesResolve()
{
    var codex = new AppBinding("codex", "Codex", "C:\\Apps\\Codex.exe", "cx");
    var chrome = new AppBinding("chrome", "Chrome", "C:\\Apps\\Chrome.exe", "ch");
    var bindings = new[] { codex, chrome };

    Check.Equal(AliasMatchKind.Partial, AliasMatcher.Resolve("c", bindings).Kind);
    Check.Equal(AliasMatchKind.Exact, AliasMatcher.Resolve("CX", bindings).Kind);
    Check.Equal(codex.Id, AliasMatcher.Resolve("cx", bindings).Binding?.Id);
    Check.Equal(AliasMatchKind.None, AliasMatcher.Resolve("z", bindings).Kind);

    var overlapping = new[] { codex, new AppBinding("calendar", "Calendar", "C:\\Apps\\Calendar.exe", "cxy") };
    Check.Equal(AliasMatchKind.AmbiguousExact, AliasMatcher.Resolve("cx", overlapping).Kind);
}

static void AliasesValidate()
{
    Check.Equal<AliasValidationError?>(null, AliasValidator.Validate("cx", []));
    Check.Equal(AliasValidationError.Empty, AliasValidator.Validate("  ", []));
    Check.Equal(AliasValidationError.LettersOnly, AliasValidator.Validate("vscode2", []));
    Check.Equal(AliasValidationError.Duplicate, AliasValidator.Validate("CX", ["cx"]));
}

static void LauncherToggles()
{
    Check.Equal(LaunchAction.Minimize, LaunchActionResolver.Resolve(isRunning: true, isForeground: true, isMinimized: false));
    Check.Equal(LaunchAction.RestoreAndMaximize, LaunchActionResolver.Resolve(isRunning: true, isForeground: false, isMinimized: true));
    Check.Equal(LaunchAction.RestoreAndMaximize, LaunchActionResolver.Resolve(isRunning: true, isForeground: false, isMinimized: false));
    Check.Equal(LaunchAction.StartAndMaximize, LaunchActionResolver.Resolve(isRunning: false, isForeground: false, isMinimized: false));
}

static void ConfigurationRoundTrips()
{
    var settings = new LauncherSettings
    {
        Hotkey = "RightControl",
        Theme = AppTheme.Light,
        StartWithWindows = true,
        Bindings = [new AppBinding("codex", "Codex", "C:\\Apps\\Codex.exe", "cx", "--profile work")]
    };

    var json = SettingsSerializer.Serialize(settings);
    var imported = SettingsSerializer.Deserialize(json);
    Check.Equal(settings.Hotkey, imported.Hotkey);
    Check.Equal(settings.Bindings[0].Alias, imported.Bindings[0].Alias);
    Check.Equal("--profile work", imported.Bindings[0].Arguments);
    Check.SequenceEqual(new[] { "C:\\Apps\\Codex.exe" },
        SettingsSerializer.FindMissingPaths(imported, _ => false));
    Check.Equal(0, SettingsSerializer.FindMissingPaths(imported, _ => true).Count);
}

static void InvalidThemeAndHotkeyRejected()
{
    Check.Throws<InvalidDataException>(() => SettingsSerializer.Deserialize(
        "{\"SchemaVersion\":1,\"Hotkey\":\"RightControl\",\"Theme\":99,\"StartWithWindows\":true,\"Bindings\":[]}"));

    var invalidHotkey = new LauncherSettings { Hotkey = "F20" };
    Check.Throws<InvalidDataException>(() => SettingsSerializer.Serialize(invalidHotkey));
    Check.Throws<InvalidDataException>(() => SettingsSerializer.Serialize(new LauncherSettings { Hotkey = "F01" }));
    Check.Throws<InvalidDataException>(() => SettingsSerializer.Serialize(new LauncherSettings { Hotkey = "Key0" }));
}

static void OpenerNamesMapToVirtualKeys()
{
    Check.Equal(0xA3u, HotkeyVirtualKeyMapper.ToVirtualKey("RightControl"));
    Check.Equal(0xA1u, HotkeyVirtualKeyMapper.ToVirtualKey("RightShift"));
    Check.Equal(0x5Bu, HotkeyVirtualKeyMapper.ToVirtualKey("LeftWindows"));
    Check.Equal(0xFFu, HotkeyVirtualKeyMapper.ToVirtualKey("Key255"));
    Check.Equal(0x7Bu, HotkeyVirtualKeyMapper.ToVirtualKey("F12"));
    Check.Throws<ArgumentException>(() => HotkeyVirtualKeyMapper.ToVirtualKey("F01"));
    Check.Throws<ArgumentException>(() => HotkeyVirtualKeyMapper.ToVirtualKey("Key0"));
}

static void InvalidBindingsRejected()
{
    Check.Throws<InvalidDataException>(() => SettingsSerializer.Deserialize(
        "{\"SchemaVersion\":1,\"Hotkey\":\"RightControl\",\"Theme\":\"Light\",\"StartWithWindows\":true,\"Bindings\":[null]}"));

    var duplicateIds = new LauncherSettings
    {
        Bindings =
        [
            new AppBinding("same", "Browser", "C:\\Apps\\Browser.exe", "br"),
            new AppBinding("SAME", "Editor", "C:\\Apps\\Editor.exe", "ed")
        ]
    };
    Check.Throws<InvalidDataException>(() => SettingsSerializer.Serialize(duplicateIds));

    var nullArguments = new LauncherSettings
    {
        Bindings = [new AppBinding("null-args", "Browser", "C:\\Apps\\Browser.exe", "br") { Arguments = null! }]
    };
    Check.Throws<InvalidDataException>(() => SettingsSerializer.Serialize(nullArguments));
}

static void MalformedSettingsAreStructured()
{
    Check.Throws<InvalidDataException>(() => SettingsSerializer.Deserialize("{ not json"));
    Check.Throws<InvalidDataException>(() => SettingsSerializer.Deserialize(
        "{\"SchemaVersion\":1,\"Hotkey\":\"RightControl\",\"Theme\":\"Ultraviolet\",\"StartWithWindows\":true,\"Bindings\":[]}"));
}

static void AutomationResultsKeepOutput()
{
    using var data = System.Text.Json.JsonDocument.Parse("{\"count\":3,\"ok\":true}");
    using var input = System.Text.Json.JsonDocument.Parse("{\"reportId\":\"r1\"}");
    var result = new AutomationResult(
        AutomationTabContract.CurrentVersion,
        AutomationStatus.Success,
        "Script finished",
        data.RootElement.Clone(),
        [new AutomationAction("open-report", "Open report", 1, input.RootElement.Clone())]);

    var serialized = System.Text.Json.JsonSerializer.Serialize(result);
    var roundTrip = System.Text.Json.JsonSerializer.Deserialize<AutomationResult>(serialized)!;
    Check.Equal(3, roundTrip.Data.GetProperty("count").GetInt32());
    Check.Equal(true, roundTrip.Data.GetProperty("ok").GetBoolean());
    Check.Equal("open-report", result.Actions[0].Id);
    Check.Equal("r1", roundTrip.Actions[0].Payload.GetProperty("reportId").GetString());
    Check.Equal(1, AutomationTabContract.CurrentVersion);
}

static void SchemaOneSettingsMigrate()
{
    var settings = SettingsSerializer.Deserialize(
        "{\"SchemaVersion\":1,\"Hotkey\":\"RightAlt\",\"Theme\":\"Dark\",\"StartWithWindows\":false,\"Bindings\":[{\"Id\":\"codex\",\"Name\":\"Codex\",\"TargetPath\":\"C:\\\\Apps\\\\Codex.exe\",\"Alias\":\"cx\",\"Arguments\":\"--profile work\"}]}");

    Check.Equal(2, settings.SchemaVersion);
    Check.Equal("RightAlt", settings.Hotkey);
    Check.Equal(AppTheme.Dark, settings.Theme);
    Check.Equal(false, settings.StartWithWindows);
    Check.Equal("cx", settings.Bindings[0].Alias);
    Check.Equal("--profile work", settings.Bindings[0].Arguments);
    Check.Equal(0, settings.ModuleSettings.Count);
}

static void UnknownModuleSettingsRoundTrip()
{
    using var value = System.Text.Json.JsonDocument.Parse("{\"future\":[1,{\"enabled\":true}]}");
    var settings = new LauncherSettings
    {
        ModuleSettings = [new ModuleSettingsEntry("future-device-tab", 3, value.RootElement.Clone())]
    };

    var imported = SettingsSerializer.Deserialize(SettingsSerializer.Serialize(settings));
    Check.Equal("future-device-tab", imported.ModuleSettings[0].ModuleId);
    Check.Equal(3, imported.ModuleSettings[0].SchemaVersion);
    Check.Equal(true, imported.ModuleSettings[0].Value.GetProperty("future")[1].GetProperty("enabled").GetBoolean());
}

static void InvalidModuleSettingsRejected()
{
    using var value = System.Text.Json.JsonDocument.Parse("{}");
    Check.Throws<InvalidDataException>(() => SettingsSerializer.Serialize(new LauncherSettings
    {
        ModuleSettings = [
            new ModuleSettingsEntry("future-tab", 1, value.RootElement.Clone()),
            new ModuleSettingsEntry("future-tab", 2, value.RootElement.Clone())]
    }));

    using var oversized = System.Text.Json.JsonDocument.Parse("{\"value\":\"" + new string('x', ModuleSettingsEntry.MaximumPayloadBytes) + "\"}");
    Check.Throws<InvalidDataException>(() => SettingsSerializer.Serialize(new LauncherSettings
    {
        ModuleSettings = [new ModuleSettingsEntry("future-tab", 1, oversized.RootElement.Clone())]
    }));
}

static class Check
{
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
        }
    }

    public static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException("Sequences did not match.");
        }
    }

    public static void Throws<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }
}
