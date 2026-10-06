using Automator.Application.Launcher;
using Automator.Application.Automation;
using Automator.Application.Logging;
using Automator.Core.Automation;
using Automator.Core.Configuration;
using Automator.Core.Launcher;
using Automator.Core.Plugins;
using System.Text.Json;

var checks = new (string Name, Func<Task> Run)[]
{
    ("opening resets launcher state and captures the previous foreground HWND", OpeningResetsStateAndCapturesContext),
    ("global tab keys require the visible foreground launcher context", GlobalTabKeysRequireLauncherContext),
    ("global navigation consumes only focused unmodified launcher keys", NavigationKeyPolicyRequiresFocusedUnmodifiedLauncher),
    ("hotkey recording ignores stale, unfocused and native-dialog input", RecordingRequiresVisibleFocusedContext),
    ("a view-mode transition cancels pending alias activation", ModeTransitionCancelsPendingActivation),
    ("stale catalog completion is ignored after the view changes", StaleCatalogCompletionIsIgnored),
    ("late activation results cannot affect a reopened panel", ReopenedPanelInvalidatesActivationResult),
    ("catalog pins the pointer-captured app and removes duplicate identities", CatalogPinsAndDeduplicates),
    ("alias edits update a binding without losing target arguments", AliasEditUpdatesBinding),
    ("alias matching is only active in launcher slot one", AliasMatchingIsContextual),
    ("state revisions increase monotonically", RevisionsIncrease),
    ("query changes cancel only the pending alias delay", PendingAliasDelayCanBeCancelled),
    ("a later query cannot cancel an activation already requested", QueryChangeDoesNotCancelActivation),
    ("concurrent state changes publish revisions in serialized order", ConcurrentCommandsKeepRevisionOrder),
    ("tab registry publishes nine stable slots and their actions", TabRegistryPublishesVersionedSlots),
    ("website shortcut actions validate settings and launch saved groups in order", WebsiteLauncherValidatesAndLaunchesGroups),
    ("bundled module registry validates and dispatches versioned actions", BundledModuleRegistryDispatches),
    ("bundled modules receive cancellation and module settings migrate", BundledModuleLifecycleAndSettings),
    ("module settings reads return defaults or saved values and reject stale or inactive requests", ModuleSettingsReadsAreScopedAndVersioned),
    ("module settings writes require the active registered version and preserve unknown entries", ModuleSettingsWritesAreScopedAndPreserveUnknownEntries),
    ("bundled module registry rejects duplicate and unsupported declarations", InvalidBundledModulesRejected),
    ("bundled module actions cannot use an unavailable declared capability", UndeclaredModuleServiceRejected),
    ("bundled module results enforce structured output limits", OversizedModuleResultRejected),
    ("production module view kinds match the cross-layer view contract", ProductionModuleViewsMatchContract),
    ("capability registry rejects unknown capabilities and versions", CapabilityRegistryRejectsUnknownCapabilitiesAndVersions),
    ("future module service capabilities are recognized but require host registration", NewModuleServiceCapabilitiesAreValidated),
    ("automation service authorization enforces active module grants and host context", AutomationServiceAuthorizationEnforcesAllBoundaries),
    ("module context exposes only the granted typed services", ModuleContextExposesOnlyGrantedServices),
    ("library capability binds records to the active module and revokes on disposal", ModuleLibraryCapabilityIsScoped),
    ("process capability is module-scoped and canceled when its context ends", ModuleProcessCapabilityIsScoped),
    ("script runner saves profiles and passes script paths as structured process arguments", ScriptRunnerUsesSavedProfiles),
    ("script runner launches PowerShell profiles noninteractively with structured script arguments", ScriptRunnerRunsPowerShell),
    ("script runner rejects invalid profile paths and timeout bounds", ScriptRunnerRejectsInvalidProfiles),
    ("script runner reports timeout and invalid JSON output", ScriptRunnerReportsProcessWarnings),
    ("script template catalog validates descriptors and legacy profile metadata", ScriptRunnerTemplateSpecs.RunAsync),
    ("API profiles save URL-only grants and profile default JSON", ApiProfilesSaveUrlOnlyAndDefaultInput),
    ("work-time log actions validate metadata and delegate to the host coordinator", FocusSessionModuleDispatchesCoordinatorActions),
    ("context disposal cancels active HTTP calls and rejects later calls", ContextDisposalScopesHttpCalls),
    ("disposed context waits for keyboard subscription cleanup", DisposedContextWaitsForKeyboardCleanup),
    ("keyboard unsubscribe disposes the context-scoped subscriptions", KeyboardUnsubscribeDisposesScopedSubscriptions),
    ("keyboard delivery requires the trusted active visible focused context", KeyboardDeliveryRequiresTrustedContext),
    ("keyboard subscriber revocation and overflow detach delivery", KeyboardSubscribersAreRevokedAndOverflowIsDetached),
    ("concurrent keyboard revocation and disposal stop a subscriber safely", ConcurrentKeyboardRevocationAndDisposalAreSafe),
    ("request executor cancels one request and every request in a module", RequestExecutorCancelsRequestsByScope),
    ("request executor registers before an immediate cancellation", RequestExecutorRegistersBeforeImmediateCancellation),
    ("saved profile execution uses only registered handlers and carries run metadata", SavedProfileExecutionUsesRegisteredHandlers),
    ("saved profile catalogs expose only validated name and id metadata", SavedProfileCatalogsExposeSafeMetadata),
    ("workflow engine maps JSON between saved profiles and persists summaries only", WorkflowEngineMapsProfilesAndPersistsSafeHistory),
    ("workflow engine stops after the first failed profile", WorkflowEngineStopsAfterFailure),
    ("canceled workflows persist only a safe cancellation summary", CanceledWorkflowPersistsSafeHistory),
    ("run activity retains bounded metadata and projects existing workflow/scheduler history", RunActivitySpecs.RunAsync),
    ("pending service work does not block serialized launcher commands", ServiceWorkDoesNotBlockLauncherCommands),
};

var failures = 0;
foreach (var (name, run) in checks)
{
    try
    {
        await run();
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

static Task OpeningResetsStateAndCapturesContext()
{
    var session = new LauncherSession(CreateSettings());
    session.SelectTab(4);
    session.SetQuery("old query");
    session.HidePanel();
    session.ShowPanel("0x000000000001ABCD");

    Check.True(session.Snapshot.Visible);
    Check.Equal(1, session.Snapshot.SelectedTab);
    Check.Equal(string.Empty, session.Snapshot.Query);
    Check.Equal("0x000000000001ABCD", session.Snapshot.PreviousForegroundHwnd);
    return Task.CompletedTask;
}

static Task GlobalTabKeysRequireLauncherContext()
{
    var session = new LauncherSession(CreateSettings());
    Check.False(session.SelectTabFromGlobalKey('2', launcherNativeForeground: true));

    session.ShowPanel("0x11");
    Check.False(session.SelectTabFromGlobalKey('2', launcherNativeForeground: false));
    Check.True(session.SelectTabFromGlobalKey('2', launcherNativeForeground: true));
    Check.Equal(2, session.Snapshot.SelectedTab);

    session.SelectTab(1);
    session.EnterMode(LauncherMode.Settings);
    Check.False(session.SelectTabFromGlobalKey('3', launcherNativeForeground: true));

    session.EnterMode(LauncherMode.RecordingHotkey);
    Check.False(session.SelectTabFromGlobalKey('4', launcherNativeForeground: true));
    return Task.CompletedTask;
}

static Task CatalogPinsAndDeduplicates()
{
    var session = new LauncherSession(CreateSettings());
    var captured = Binding("pointer", "Browser", @"C:\Apps\Browser.exe", "");
    var duplicate = Binding("catalog-copy", "Browser Copy", @"c:\apps\browser.exe", "");
    var alternateArguments = Binding("browser-profile", "Browser Profile", @"C:\Apps\Browser.exe", "", "--profile guest");
    var caseSensitiveArguments = Binding("browser-profile-case", "Browser Profile Case", @"C:\Apps\Browser.exe", "", "--profile Guest");
    var other = Binding("other", "Editor", @"C:\Apps\Editor.exe", "");
    session.ShowPanel("0x11", captured);
    Check.True(session.OpenCatalog([duplicate, alternateArguments, caseSensitiveArguments, other]));

    Check.Equal(LauncherMode.Catalog, session.Snapshot.Mode);
    Check.Equal(4, session.Snapshot.CatalogApps.Count);
    Check.Equal("pointer", session.Snapshot.CatalogApps[0].Id);
    Check.Equal("--profile guest", session.Snapshot.CatalogApps[1].Arguments);
    Check.Equal("--profile Guest", session.Snapshot.CatalogApps[2].Arguments);
    Check.Equal("other", session.Snapshot.CatalogApps[3].Id);
    return Task.CompletedTask;
}

static Task NavigationKeyPolicyRequiresFocusedUnmodifiedLauncher()
{
    var session = new LauncherSession(CreateSettings());
    session.ShowPanel("0x11");
    var state = session.NavigationState;
    Check.Equal(NavigationKeyAction.None, NavigationKeyPolicy.Decide(state, nativeForeground: false, rendererFocused: true, hasModifier: false, 0x32));
    Check.Equal(NavigationKeyAction.None, NavigationKeyPolicy.Decide(state, nativeForeground: true, rendererFocused: false, hasModifier: false, 0x32));
    Check.Equal(NavigationKeyAction.None, NavigationKeyPolicy.Decide(state, nativeForeground: true, rendererFocused: true, hasModifier: true, 0x32));
    Check.Equal(NavigationKeyAction.SelectTab, NavigationKeyPolicy.Decide(state, true, true, false, 0x32));
    Check.Equal(NavigationKeyAction.OpenCatalog, NavigationKeyPolicy.Decide(state, true, true, false, 0xBF));
    session.OpenCatalog([]);
    Check.Equal(NavigationKeyAction.SelectTab, NavigationKeyPolicy.Decide(session.NavigationState, true, true, false, 0x32));
    Check.True(session.SelectTabFromGlobalKey('2', launcherNativeForeground: true));
    Check.Equal(LauncherMode.Launcher, session.Snapshot.Mode);
    session.SelectTab(2);
    Check.Equal(NavigationKeyAction.SelectTab, NavigationKeyPolicy.Decide(session.NavigationState, true, true, false, 0x32));
    session.EnterMode(LauncherMode.Settings);
    Check.Equal(NavigationKeyAction.None, NavigationKeyPolicy.Decide(session.NavigationState, true, true, false, 0x1B));
    return Task.CompletedTask;
}

static Task RecordingRequiresVisibleFocusedContext()
{
    var session = new LauncherSession(CreateSettings());
    session.ShowPanel("0x11");
    session.EnterMode(LauncherMode.RecordingHotkey);
    var state = session.NavigationState;
    Check.False(HotkeyRecordingPolicy.ShouldCapture(state with { Visible = false }, true, true, false, true, true));
    Check.False(HotkeyRecordingPolicy.ShouldCapture(state with { Mode = LauncherMode.Settings }, true, true, false, true, true));
    Check.False(HotkeyRecordingPolicy.ShouldCapture(state, false, true, false, true, true));
    Check.False(HotkeyRecordingPolicy.ShouldCapture(state, true, false, false, true, true));
    Check.False(HotkeyRecordingPolicy.ShouldCapture(state, true, true, true, true, true));
    Check.False(HotkeyRecordingPolicy.ShouldCapture(state, true, true, false, false, true));
    Check.False(HotkeyRecordingPolicy.ShouldCapture(state, true, true, false, true, false));
    Check.True(HotkeyRecordingPolicy.ShouldCapture(state, true, true, false, true, true));
    return Task.CompletedTask;
}

static async Task ModeTransitionCancelsPendingActivation()
{
    var delay = new GatedDelay();
    var activator = new RecordingActivator();
    using var scheduler = new AliasLaunchScheduler(delay, activator);
    var session = new LauncherSession(CreateSettings());
    session.ShowPanel("0x11");
    session.SetQuery("a");
    var pending = scheduler.ScheduleAsync(Binding("app", "App", @"C:\Apps\App.exe", "a"), "0x11");
    var delayToken = await delay.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));

    var coordinator = new LauncherModeCoordinator(session, scheduler);
    Check.True(coordinator.SetMode(LauncherMode.Settings));
    await pending.WaitAsync(TimeSpan.FromSeconds(2));
    Check.True(delayToken.IsCancellationRequested);
    Check.Equal(0, activator.CallCount);
}

static Task StaleCatalogCompletionIsIgnored()
{
    var session = new LauncherSession(CreateSettings());
    session.ShowPanel("0x11");
    Check.True(session.BeginCatalogLoading());
    session.SelectTab(2);
    session.CompleteCatalogLoading([Binding("late", "Late result", @"C:\Apps\Late.exe", "")]);
    Check.Equal(LauncherMode.Launcher, session.Snapshot.Mode);
    Check.Equal(2, session.Snapshot.SelectedTab);
    Check.Equal(0, session.Snapshot.CatalogApps.Count);
    return Task.CompletedTask;
}

static Task ReopenedPanelInvalidatesActivationResult()
{
    var epoch = new PanelActivationEpoch();
    var activation = epoch.BeginActivation();
    Check.True(epoch.IsCurrent(activation));
    epoch.PanelChanged(); // A new user open invalidates the old activation's completion ticket.
    Check.False(epoch.IsCurrent(activation));
    return Task.CompletedTask;
}

static Task AliasEditUpdatesBinding()
{
    var existing = Binding("one", "Browser", @"C:\Apps\Browser.exe", "br", "--profile work");
    var candidate = Binding("two", "Editor", @"C:\Apps\Editor.exe", "", "--safe");
    var session = new LauncherSession(CreateSettings(existing));
    session.ShowPanel("0x11");
    Check.True(session.BeginAliasEdit(candidate));

    Check.Equal(AliasValidationError.Duplicate, session.SaveAlias("BR"));
    Check.Equal(string.Empty, session.Snapshot.Bindings.SingleOrDefault(item => item.Id == "two")?.Alias ?? string.Empty);

    Check.Equal(null, session.SaveAlias("ed"));
    var saved = session.Snapshot.Bindings.Single(item => item.Id == "two");
    Check.Equal("ed", saved.Alias);
    Check.Equal("--safe", saved.Arguments);
    Check.Equal(candidate.TargetPath, saved.TargetPath);
    Check.Equal(LauncherMode.Launcher, session.Snapshot.Mode);
    return Task.CompletedTask;
}

static Task AliasMatchingIsContextual()
{
    var session = new LauncherSession(CreateSettings(Binding("codex", "Codex", @"C:\Apps\Codex.exe", "cx")));
    session.ShowPanel("0x11");
    session.SetQuery("c");
    Check.Equal(AliasMatchKind.Partial, session.ResolveQuery().Kind);
    session.SetQuery("CX");
    Check.Equal(AliasMatchKind.Exact, session.ResolveQuery().Kind);

    session.SelectTab(2);
    Check.Equal(AliasMatchKind.None, session.ResolveQuery().Kind);
    session.SelectTab(1);
    session.EnterMode(LauncherMode.Catalog);
    Check.Equal(AliasMatchKind.None, session.ResolveQuery().Kind);
    return Task.CompletedTask;
}

static Task RevisionsIncrease()
{
    var session = new LauncherSession(CreateSettings());
    var initial = session.Snapshot.Revision;
    session.ShowPanel("0x11");
    var opened = session.Snapshot.Revision;
    session.SetQuery("hello");
    var searched = session.Snapshot.Revision;

    Check.True(opened > initial);
    Check.True(searched > opened);
    return Task.CompletedTask;
}

static async Task ConcurrentCommandsKeepRevisionOrder()
{
    var emitted = new List<long>();
    long revision = 0;
    await using var commands = new SerializedCommandQueue();
    var results = await Task.WhenAll(Enumerable.Range(0, 128).Select(_ => commands.EnqueueAsync<long>(async () =>
    {
        var next = checked(++revision);
        await Task.Yield();
        emitted.Add(next);
        return next;
    })));

    Check.Equal(128, results.Distinct().Count());
    Check.Equal(128, emitted.Count);
    for (var index = 0; index < emitted.Count; index++)
        Check.Equal((long)index + 1, emitted[index]);
}

static Task TabRegistryPublishesVersionedSlots()
{
    var tabs = LauncherTabRegistry.Tabs;
    Check.Equal(9, tabs.Count);
    Check.Equal(3, LauncherTabRegistry.Version);
    Check.Equal(1, tabs[0].Slot);
    Check.Equal("launcher", tabs[0].Id);
    Check.True(tabs[0].Actions.Any(action => action.Id == "openCatalog" && action.Command == "launcher/openCatalog" && action.Version == 1));
    Check.Equal("script-runner", tabs[1].Id);
    Check.Equal("script-runner", tabs[1].Kind);
    Check.True(tabs[1].Actions.Any(action => action.Id == "runProfile"));
    Check.True(tabs.Skip(2).Take(3).Select(tab => tab.Id).SequenceEqual(["api", "browser-automation", "workflows"]));
    Check.True(tabs.Skip(2).Take(3).Select(tab => tab.Kind).SequenceEqual(["api", "browser-automation", "workflows"]));
    Check.Equal("scheduler", tabs[5].Id);
    Check.Equal("scheduler", tabs[5].Kind);
    Check.True(tabs[5].Actions.Any(action => action.Id == "runNow"));
    Check.Equal("focus-sessions", tabs[6].Id);
    Check.Equal("focus-sessions", tabs[6].Kind);
    Check.Equal("website-launcher", tabs[7].Id);
    Check.Equal("website-launcher", tabs[7].Kind);
    Check.True(tabs[7].Actions.Any(action => action.Id == "launchRow"));
    Check.Equal("reserved-9", tabs[8].Id);
    Check.Equal("reserved", tabs[8].Kind);
    Check.Equal(0, tabs[8].Actions.Count);
    Check.True(tabs.Take(2).SelectMany(tab => tab.Capabilities).Any(capability => capability.Id == AutomationCapabilityIds.LibraryStorage));
    Check.Equal(9, LauncherTabRegistry.States.Count);
    Check.True(LauncherTabRegistry.States.All(state => state.Version == LauncherTabRegistry.Version));
    Check.Equal(9, tabs.Select(tab => tab.Slot).Distinct().Count());
    var runtimeRegistry = LauncherTabRegistry.CreateAutomationRegistry(new AutomationCapabilityRegistry());
    Check.Equal(9, runtimeRegistry.Modules.Count);
    foreach (var tab in tabs)
    {
        var definition = runtimeRegistry.Modules.Single(module => module.Id == tab.Id);
        Check.Equal(tab.Slot, definition.Slot);
        Check.Equal(tab.Title, definition.Title);
        Check.Equal(tab.IconKey, definition.IconKey);
        Check.Equal(tab.Kind, definition.ViewKind);
        Check.Equal(tab.ContractVersion, definition.ContractVersion);
        Check.Equal(tab.SettingsVersion, definition.SettingsVersion);
        Check.True(tab.Actions.Select(action => action.Id).SequenceEqual(definition.Actions.Select(action => action.Id)));
    }
    return Task.CompletedTask;
}

static async Task WebsiteLauncherValidatesAndLaunchesGroups()
{
    var launcher = new RecordingWebsiteLauncher();
    var capabilities = new AutomationCapabilityRegistry(websiteLauncherFactory: _ => launcher);
    var module = new WebsiteLauncherModule();
    await using var services = capabilities.CreateContext(new AutomationModuleDescriptor(
        module.Id, [new(AutomationCapabilityIds.WebsiteLaunch, 1)]));
    var settings = JsonSerializer.SerializeToElement(new WebsiteLauncherSettings([
        new("work", "Work", "docs", [
            new("window-one", "Main", [new("wiki", "Wiki", "https://wiki.example.test"), new("chat", "Chat", "https://chat.example.test")]),
            new("window-two", "Reports", [new("report", "Report", "https://report.example.test")]),
        ])
    ]), new JsonSerializerOptions(JsonSerializerDefaults.Web));
    using var input = JsonDocument.Parse("{\"rowId\":\"work\"}");
    var result = await module.ExecuteAsync("launchRow", input.RootElement, settings, services, CancellationToken.None);
    Check.Equal(AutomationStatus.Success, result.Status);
    Check.Equal(2, launcher.Groups!.Count);
    Check.Equal("https://wiki.example.test/", launcher.Groups[0][0].AbsoluteUri);
    Check.Equal("https://chat.example.test/", launcher.Groups[0][1].AbsoluteUri);
    Check.Equal("https://report.example.test/", launcher.Groups[1][0].AbsoluteUri);
    var quickActionsJson = WebsiteLauncherModule.SerializeQuickActions(settings);
    Check.Equal("[{\"id\":\"work\",\"name\":\"Work\",\"alias\":\"docs\"}]", quickActionsJson);
    Check.False(quickActionsJson.Contains("https://", StringComparison.Ordinal));

    var invalid = JsonSerializer.SerializeToElement(new WebsiteLauncherSettings([
        new("bad", "Bad", "bad", [new("group", "Group", [new("site", "Bad scheme", "javascript:alert(1)")])])
    ]), new JsonSerializerOptions(JsonSerializerDefaults.Web));
    var rejected = await module.ExecuteAsync("launchRow", input.RootElement, invalid, services, CancellationToken.None);
    Check.Equal(AutomationStatus.Error, rejected.Status);
    Check.Equal(2, launcher.Groups.Count);
}

static Task CapabilityRegistryRejectsUnknownCapabilitiesAndVersions()
{
    var registry = new AutomationCapabilityRegistry();
    Check.Throws<InvalidOperationException>(() => registry.CreateContext(new AutomationModuleDescriptor(
        "unknown", [new("filesystem.raw", 1)])));
    Check.Throws<InvalidOperationException>(() => registry.CreateContext(new AutomationModuleDescriptor(
        "old-version", [new(AutomationCapabilityIds.KeyboardInput, 99)])));
    return Task.CompletedTask;
}

static Task NewModuleServiceCapabilitiesAreValidated()
{
    var registry = new AutomationCapabilityRegistry();
    foreach (var capability in new[]
    {
        "secrets.manage", "api.profileHttp", "browser.session", "workflow.execute", "scheduler.manage", "focus.manage", "website.launch"
    })
    {
        try
        {
            registry.CreateContext(new AutomationModuleDescriptor("capability-check", [new(capability, 1)]));
            throw new InvalidOperationException($"Capability '{capability}' was granted without a host service.");
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("no host service registered", StringComparison.Ordinal))
        {
        }
    }
    return Task.CompletedTask;
}

static async Task BundledModuleRegistryDispatches()
{
    var module = new FixtureAutomationModule();
    var registry = new AutomationModuleRegistry([module], new AutomationCapabilityRegistry());
    await using var context = new AutomationCapabilityRegistry().CreateContext(new AutomationModuleDescriptor(module.Id, []));
    using var input = System.Text.Json.JsonDocument.Parse("{\"count\":4}");
    using var savedSettings = System.Text.Json.JsonDocument.Parse("{\"scope\":\"work\"}");

    var result = await registry.DispatchAsync(module.Id, "summarize", 1, 1, input.RootElement, module.Id, context,
        [new ModuleSettingsEntry(module.Id, 2, savedSettings.RootElement.Clone())], CancellationToken.None);
    Check.Equal(AutomationStatus.Success, result.Status);
    Check.Equal(4, result.Data.GetProperty("count").GetInt32());
    Check.Equal("work", result.Data.GetProperty("settings").GetProperty("scope").GetString());
    Check.Equal("open", result.Actions[0].Id);
    Check.Equal("report-4", result.Actions[0].Payload.GetProperty("reportId").GetString());
    await Check.ThrowsAsync<InvalidOperationException>(() => registry.DispatchAsync(module.Id, "summarize", 1, 1,
        input.RootElement, "reserved-2", context, [], CancellationToken.None));
    await Check.ThrowsAsync<InvalidOperationException>(() => registry.DispatchAsync(module.Id, "missing", 1, 1,
        input.RootElement, module.Id, context, [], CancellationToken.None));
    await Check.ThrowsAsync<InvalidOperationException>(() => registry.DispatchAsync(module.Id, "summarize", 1, 99,
        input.RootElement, module.Id, context, [], CancellationToken.None));
}

static async Task BundledModuleLifecycleAndSettings()
{
    var module = new FixtureAutomationModule();
    var registry = new AutomationModuleRegistry([module], new AutomationCapabilityRegistry());
    using var legacySettings = System.Text.Json.JsonDocument.Parse("{\"counter\":2}");
    var migrated = registry.MigrateModuleSettings([
        new ModuleSettingsEntry(module.Id, 1, legacySettings.RootElement.Clone()),
        new ModuleSettingsEntry("future-module", 4, legacySettings.RootElement.Clone())]);
    Check.Equal(2, migrated.Single(entry => entry.ModuleId == module.Id).SchemaVersion);
    Check.Equal(true, migrated.Single(entry => entry.ModuleId == module.Id).Value.GetProperty("migrated").GetBoolean());
    Check.Equal(4, migrated.Single(entry => entry.ModuleId == "future-module").SchemaVersion);

    await using var context = new AutomationCapabilityRegistry().CreateContext(new AutomationModuleDescriptor(module.Id, []));
    using var input = System.Text.Json.JsonDocument.Parse("{}");
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    await Check.ThrowsAsync<OperationCanceledException>(() => registry.DispatchAsync(module.Id, "wait", 1, 1,
        input.RootElement, module.Id, context, [], cancellation.Token));
}

static Task ModuleSettingsWritesAreScopedAndPreserveUnknownEntries()
{
    var module = new FixtureAutomationModule();
    var registry = new AutomationModuleRegistry([module], new AutomationCapabilityRegistry());
    using var prior = System.Text.Json.JsonDocument.Parse("{\"old\":true}");
    using var replacement = System.Text.Json.JsonDocument.Parse("{\"layout\":\"compact\"}");
    using var futureValue = System.Text.Json.JsonDocument.Parse("{\"device\":\"retained\"}");
    var existing = new List<ModuleSettingsEntry>
    {
        new(module.Id, 1, prior.RootElement.Clone()),
        new("future-device-tab", 3, futureValue.RootElement.Clone()),
    };

    var updated = registry.CreateUpdatedModuleSettings(module.Id, 1, 2, replacement.RootElement, module.Id, existing);
    Check.Equal("compact", updated.Single(entry => entry.ModuleId == module.Id).Value.GetProperty("layout").GetString());
    Check.Equal(2, updated.Single(entry => entry.ModuleId == module.Id).SchemaVersion);
    Check.Equal(3, updated.Single(entry => entry.ModuleId == "future-device-tab").SchemaVersion);
    Check.Equal("retained", updated.Single(entry => entry.ModuleId == "future-device-tab").Value.GetProperty("device").GetString());

    Check.Throws<InvalidOperationException>(() => registry.CreateUpdatedModuleSettings(module.Id, 1, 2,
        replacement.RootElement, "another-module", existing));
    Check.Throws<InvalidOperationException>(() => registry.CreateUpdatedModuleSettings(module.Id, 1, 1,
        replacement.RootElement, module.Id, existing));
    using var oversized = System.Text.Json.JsonDocument.Parse("{\"value\":\"" + new string('x', ModuleSettingsEntry.MaximumPayloadBytes) + "\"}");
    Check.Throws<InvalidOperationException>(() => registry.CreateUpdatedModuleSettings(module.Id, 1, 2,
        oversized.RootElement, module.Id, existing));
    return Task.CompletedTask;
}

static Task ModuleSettingsReadsAreScopedAndVersioned()
{
    var module = new FixtureAutomationModule();
    var registry = new AutomationModuleRegistry([module], new AutomationCapabilityRegistry());
    var defaults = registry.GetActiveSettings(module.Id, 1, 2, module.Id, []);
    Check.Equal("{}", defaults.Value.GetRawText());
    Check.Equal(module.Id, defaults.ModuleId);
    Check.Equal(1, defaults.ContractVersion);
    Check.Equal(2, defaults.SettingsVersion);

    using var savedValue = System.Text.Json.JsonDocument.Parse("{\"layout\":\"compact\"}");
    var persisted = registry.GetActiveSettings(module.Id, 1, 2, module.Id,
        [new ModuleSettingsEntry(module.Id, 2, savedValue.RootElement.Clone())]);
    Check.Equal("compact", persisted.Value.GetProperty("layout").GetString());

    Check.Throws<InvalidOperationException>(() => registry.GetActiveSettings(module.Id, 1, 1, module.Id, []));
    Check.Throws<InvalidOperationException>(() => registry.GetActiveSettings(module.Id, 2, 2, module.Id, []));
    Check.Throws<InvalidOperationException>(() => registry.GetActiveSettings(module.Id, 1, 2, "reserved-2", []));
    return Task.CompletedTask;
}

static Task InvalidBundledModulesRejected()
{
    var module = new FixtureAutomationModule();
    Check.Throws<InvalidOperationException>(() => new AutomationModuleRegistry([module, module], new AutomationCapabilityRegistry()));
    Check.Throws<InvalidOperationException>(() => new AutomationModuleRegistry([new FixtureAutomationModule(duplicateActions: true)], new AutomationCapabilityRegistry()));
    Check.Throws<InvalidOperationException>(() => new AutomationModuleRegistry([new FixtureAutomationModule(unknownCapability: true)], new AutomationCapabilityRegistry()));
    Check.Throws<InvalidOperationException>(() => new AutomationModuleRegistry([new FixtureAutomationModule(contractVersion: 99)], new AutomationCapabilityRegistry()));
    return Task.CompletedTask;
}

static async Task UndeclaredModuleServiceRejected()
{
    var module = new FixtureAutomationModule(requireKeyboard: true);
    var registry = new AutomationModuleRegistry([module], new AutomationCapabilityRegistry());
    await using var context = new AutomationCapabilityRegistry().CreateContext(new AutomationModuleDescriptor(module.Id, []));
    using var input = System.Text.Json.JsonDocument.Parse("{}");
    await Check.ThrowsAsync<InvalidOperationException>(() => registry.DispatchAsync(module.Id, "summarize", 1, 1,
        input.RootElement, module.Id, context, [], CancellationToken.None));
}

static async Task OversizedModuleResultRejected()
{
    var module = new FixtureAutomationModule(oversizedResult: true);
    var registry = new AutomationModuleRegistry([module], new AutomationCapabilityRegistry());
    await using var context = new AutomationCapabilityRegistry().CreateContext(new AutomationModuleDescriptor(module.Id, []));
    using var input = System.Text.Json.JsonDocument.Parse("{}");
    await Check.ThrowsAsync<InvalidOperationException>(() => registry.DispatchAsync(module.Id, "summarize", 1, 1,
        input.RootElement, module.Id, context, [], CancellationToken.None));
}

static Task ProductionModuleViewsMatchContract()
{
    using var document = System.Text.Json.JsonDocument.Parse(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "bundled-tab-view-kinds.json")));
    var supportedKinds = document.RootElement.GetProperty("kinds").EnumerateArray()
        .Select(item => item.GetString()!).ToHashSet(StringComparer.Ordinal);
    Check.True(LauncherTabRegistry.Tabs.All(tab => supportedKinds.Contains(tab.Kind)));
    Check.Equal(9, supportedKinds.Count);
    return Task.CompletedTask;
}

static Task AutomationServiceAuthorizationEnforcesAllBoundaries()
{
    var grants = new[] { new AutomationCapabilityRequirement(AutomationCapabilityIds.KeyboardInput, 1) };
    var keyboardAndHttpGrants = new[]
    {
        new AutomationCapabilityRequirement(AutomationCapabilityIds.KeyboardInput, 1),
        new AutomationCapabilityRequirement(AutomationCapabilityIds.HttpRequest, 1),
    };
    Check.Equal(AutomationServiceAuthorizationFailure.InactiveModule,
        AutomationServiceAuthorization.Evaluate("reserved-2", "launcher", grants, AutomationCapabilityIds.KeyboardInput, contextEligible: true));
    Check.Equal(AutomationServiceAuthorizationFailure.CapabilityNotGranted,
        AutomationServiceAuthorization.Evaluate("launcher", "launcher", [], AutomationCapabilityIds.HttpRequest, contextEligible: true));
    Check.Equal(AutomationServiceAuthorizationFailure.ContextNotEligible,
        AutomationServiceAuthorization.Evaluate("launcher", "launcher", grants, AutomationCapabilityIds.KeyboardInput, contextEligible: false));
    Check.Equal(AutomationServiceAuthorizationFailure.None,
        AutomationServiceAuthorization.Evaluate("launcher", "launcher", grants, AutomationCapabilityIds.KeyboardInput, contextEligible: true));
    Check.Equal(AutomationServiceAuthorizationFailure.None,
        AutomationServiceAuthorization.Evaluate("launcher", "launcher", keyboardAndHttpGrants,
            AutomationCapabilityIds.KeyboardInput, contextEligible: false, requireEligibleContext: false));
    Check.Equal(AutomationServiceAuthorizationFailure.None,
        AutomationServiceAuthorization.Evaluate("launcher", "launcher", keyboardAndHttpGrants,
            AutomationCapabilityIds.HttpRequest, contextEligible: false, requireEligibleContext: false));
    Check.Equal(AutomationServiceAuthorizationFailure.InactiveModule,
        AutomationServiceAuthorization.Evaluate("reserved-2", "launcher", keyboardAndHttpGrants,
            AutomationCapabilityIds.HttpRequest, contextEligible: false, requireEligibleContext: false));
    Check.Equal(AutomationServiceAuthorizationFailure.CapabilityNotGranted,
        AutomationServiceAuthorization.Evaluate("launcher", "launcher", grants,
            AutomationCapabilityIds.HttpRequest, contextEligible: false, requireEligibleContext: false));
    return Task.CompletedTask;
}

static async Task ModuleContextExposesOnlyGrantedServices()
{
    var keyboard = new RecordingKeyboardInput();
    var http = new RecordingHttpClient();
    var registry = new AutomationCapabilityRegistry(_ => keyboard, (_, _) => http);
    await using var keyboardContext = registry.CreateContext(new AutomationModuleDescriptor(
        "keyboard-only", [new(AutomationCapabilityIds.KeyboardInput, 1)]));
    await using var httpContext = registry.CreateContext(new AutomationModuleDescriptor(
        "http-only", [new(AutomationCapabilityIds.HttpRequest, 1)], new AutomationHttpPolicy(["example.test"])));
    await using var emptyContext = registry.CreateContext(new AutomationModuleDescriptor("none", []));

    Check.True(keyboardContext.KeyboardInput is not null);
    Check.Equal(null, keyboardContext.Http);
    Check.Equal(null, httpContext.KeyboardInput);
    Check.True(httpContext.Http is not null);
    Check.Equal(null, emptyContext.KeyboardInput);
    Check.Equal(null, emptyContext.Http);
}

static async Task ModuleLibraryCapabilityIsScoped()
{
    var store = new RecordingLibraryStore();
    var registry = new AutomationCapabilityRegistry(libraryStoreFactory: _ => store);
    await using var context = registry.CreateContext(new AutomationModuleDescriptor(
        "script-runner", [new(AutomationCapabilityIds.LibraryStorage, 1)]));
    using var record = System.Text.Json.JsonDocument.Parse("{\"name\":\"Daily report\"}");

    Check.True(context.Library is not null);
    Check.Equal(null, context.Http);
    await context.Library!.UpsertAsync("profiles", "daily-report", 1, record.RootElement, CancellationToken.None);
    Check.Equal("script-runner", store.LastWrite!.ModuleId);
    Check.Equal("Daily report", store.LastWrite.Data.GetProperty("name").GetString());

    await context.DisposeAsync();
    await Check.ThrowsAsync<ObjectDisposedException>(() => context.Library!.ListAsync("profiles", CancellationToken.None));
    await using var denied = registry.CreateContext(new AutomationModuleDescriptor("reserved-8", []));
    Check.Equal(null, denied.Library);
}

static async Task ModuleProcessCapabilityIsScoped()
{
    var process = new RecordingProcessService();
    var registry = new AutomationCapabilityRegistry(processServiceFactory: _ => process);
    var context = registry.CreateContext(new AutomationModuleDescriptor(
        "scripts", [new(AutomationCapabilityIds.ProcessExecution, 1)]));
    Check.True(context.Processes is not null);
    var running = context.Processes!.ExecuteAsync(new AutomationProcessRequest(
        "C:\\tools\\python.exe", [], "C:\\tools", TimeSpan.FromSeconds(1)), CancellationToken.None);
    var executionToken = await process.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
    await context.DisposeAsync();
    Check.True(executionToken.IsCancellationRequested);
    await Check.ThrowsAsync<OperationCanceledException>(() => running);

    await using var denied = registry.CreateContext(new AutomationModuleDescriptor("reserved-8", []));
    Check.Equal(null, denied.Processes);
}

static async Task FocusSessionModuleDispatchesCoordinatorActions()
{
    var moduleType = typeof(ILauncherTabModuleProvider).Assembly.GetType("Automator.Application.Automation.FocusSessionsModule");
    Check.True(moduleType is not null);
    var module = (ILauncherTabModuleProvider)Activator.CreateInstance(moduleType!)!;
    var coordinator = new RecordingWorkTimeCoordinator();
    var capabilities = new AutomationCapabilityRegistry(workTimeCoordinatorFactory: _ => coordinator);
    var registry = new AutomationModuleRegistry([module], capabilities);
    await using var services = capabilities.CreateContext(new AutomationModuleDescriptor(module.Id, module.Definition.Capabilities));
    var settings = Array.Empty<ModuleSettingsEntry>();

    Check.Equal(7, module.Definition.Slot);
    Check.Equal("focus-sessions", module.Definition.ViewKind);
    Check.True(module.Definition.Actions.Select(action => action.Id)
        .SequenceEqual(["getSnapshot", "start", "stop", "saveEntry", "updateEntry", "deleteEntry", "resumePending", "discardPending"]));
    Check.True(module.Definition.Actions.All(action => action.RequiredCapabilities.Contains(
        new AutomationCapabilityRequirement(AutomationCapabilityIds.WorkTimeManagement, 1))));

    foreach (var action in new[] { "getSnapshot", "start", "stop", "resumePending", "discardPending" })
    {
        var result = await registry.DispatchAsync(module.Id, action, 1, 1, JsonDocument.Parse("{}").RootElement,
            module.Id, services, settings, CancellationToken.None);
        Check.Equal(AutomationStatus.Success, result.Status);
        Check.True(result.Data.TryGetProperty("active", out _));
        Check.True(result.Data.TryGetProperty("history", out _));
    }

    var blankDescription = await registry.DispatchAsync(module.Id, "saveEntry", 1, 1,
        JsonDocument.Parse("""{"description":"  ","tags":[]}""").RootElement,
        module.Id, services, settings, CancellationToken.None);
    Check.Equal(AutomationStatus.Error, blankDescription.Status);
    Check.Equal(0, coordinator.SaveCount);

    var saved = await registry.DispatchAsync(module.Id, "saveEntry", 1, 1,
        JsonDocument.Parse("""{"description":"Review release","tags":["work","review"]}""").RootElement,
        module.Id, services, settings, CancellationToken.None);
    Check.Equal(AutomationStatus.Success, saved.Status);
    Check.Equal("Review release", coordinator.LastDescription);
    Check.True(coordinator.LastTags.SequenceEqual(["work", "review"]));

    var invalidUpdate = await registry.DispatchAsync(module.Id, "updateEntry", 1, 1,
        JsonDocument.Parse("""{"id":"entry-1","description":"  ","tags":[]}""").RootElement,
        module.Id, services, settings, CancellationToken.None);
    Check.Equal(AutomationStatus.Error, invalidUpdate.Status);
    Check.Equal(0, coordinator.UpdateCount);

    var updated = await registry.DispatchAsync(module.Id, "updateEntry", 1, 1,
        JsonDocument.Parse("""{"id":"entry-1","description":"Corrected work","tags":["planning"]}""").RootElement,
        module.Id, services, settings, CancellationToken.None);
    Check.Equal(AutomationStatus.Success, updated.Status);
    Check.Equal("entry-1", coordinator.LastUpdatedId);
    Check.Equal("Corrected work", coordinator.LastDescription);
    Check.True(coordinator.LastTags.SequenceEqual(["planning"]));

    var invalidDelete = await registry.DispatchAsync(module.Id, "deleteEntry", 1, 1,
        JsonDocument.Parse("{}").RootElement, module.Id, services, settings, CancellationToken.None);
    Check.Equal(AutomationStatus.Error, invalidDelete.Status);
    Check.Equal(0, coordinator.DeleteCount);

    var deleted = await registry.DispatchAsync(module.Id, "deleteEntry", 1, 1,
        JsonDocument.Parse("""{"id":"entry-1"}""").RootElement, module.Id, services, settings, CancellationToken.None);
    Check.Equal(AutomationStatus.Success, deleted.Status);
    Check.Equal("entry-1", coordinator.LastDeletedId);
    Check.True(coordinator.Calls.SequenceEqual(["getSnapshot", "start", "stop", "resumePending", "discardPending", "saveEntry", "updateEntry", "deleteEntry"]));
}

static async Task ScriptRunnerUsesSavedProfiles()
{
    var module = new ScriptRunnerModule();
    var library = new RecordingLibraryStore();
    var process = new CapturingProcessService();
    var capabilities = new AutomationCapabilityRegistry(
        libraryStoreFactory: _ => library,
        processServiceFactory: _ => process);
    var registry = new AutomationModuleRegistry([module], capabilities);
    await using var context = capabilities.CreateContext(new AutomationModuleDescriptor(module.Id, module.Definition.Capabilities));
    using var profile = System.Text.Json.JsonDocument.Parse("""
        {"id":"daily-report","name":"Daily report","interpreter":"python","interpreterPath":"C:\\Python\\python.exe","scriptPath":"C:\\scripts\\report.py","arguments":["--daily","value with spaces"],"workingDirectory":"C:\\scripts","outputMode":"json","timeoutSeconds":30}
        """);
    var saved = await registry.DispatchAsync(module.Id, "saveProfile", 1, 1, profile.RootElement, module.Id, context, [], CancellationToken.None);
    Check.Equal(AutomationStatus.Success, saved.Status);
    Check.Equal("script-runner", library.LastWrite!.ModuleId);
    Check.Equal("daily-report", library.LastWrite.Id);

    using var runInput = System.Text.Json.JsonDocument.Parse("{\"id\":\"daily-report\"}");
    var result = await registry.DispatchAsync(module.Id, "runProfile", 1, 1, runInput.RootElement, module.Id, context, [], CancellationToken.None);
    Check.Equal(AutomationStatus.Success, result.Status);
    Check.Equal("C:\\Python\\python.exe", process.LastRequest!.ExecutablePath);
    Check.Equal("C:\\scripts\\report.py", process.LastRequest.Arguments[0]);
    Check.Equal("--daily", process.LastRequest.Arguments[1]);
    Check.Equal("value with spaces", process.LastRequest.Arguments[2]);
    Check.True(result.Data.GetProperty("structuredOutput").GetProperty("generated").GetBoolean());

    var listed = await registry.DispatchAsync(module.Id, "listProfiles", 1, 1, runInput.RootElement, module.Id, context, [], CancellationToken.None);
    Check.Equal(1, listed.Data.GetProperty("profiles").GetArrayLength());
    var deleted = await registry.DispatchAsync(module.Id, "deleteProfile", 1, 1, runInput.RootElement, module.Id, context, [], CancellationToken.None);
    Check.Equal(AutomationStatus.Success, deleted.Status);
}

static async Task ApiProfilesSaveUrlOnlyAndDefaultInput()
{
    var module = new ApiModule();
    var library = new RecordingLibraryStore();
    var secrets = new CapturingSecretManager();
    var runner = new CapturingApiProfileRunner();
    var capabilities = new AutomationCapabilityRegistry(
        libraryStoreFactory: _ => library,
        secretManagerFactory: _ => secrets,
        apiProfileRunnerFactory: _ => runner);
    await using var context = capabilities.CreateContext(new AutomationModuleDescriptor(module.Id, module.Definition.Capabilities));
    using var profile = JsonDocument.Parse("""
        {"id":"status-check","name":"Status check","method":"POST","url":"https://api.example.test/v1/status","allowLocalNetwork":false,"headers":{"Accept":"application/json"},"secretHeaders":{},"bodyTemplate":null,"defaultInput":{"query":"queued","limit":4},"responseMode":"json","timeoutSeconds":30}
        """);
    var saved = await module.ExecuteAsync("saveProfile", profile.RootElement, module.CreateDefaultSettings(), context, CancellationToken.None);
    Check.Equal(AutomationStatus.Success, saved.Status);
    Check.False(library.LastWrite!.Data.TryGetProperty("allowedHosts", out _));
    Check.Equal("queued", library.LastWrite.Data.GetProperty("defaultInput").GetProperty("query").GetString());

    using var run = JsonDocument.Parse("""{"id":"status-check","input":null,"useDefaultInput":true}""");
    var result = await module.ExecuteAsync("runProfile", run.RootElement, module.CreateDefaultSettings(), context, CancellationToken.None);
    Check.Equal(AutomationStatus.Success, result.Status);
    Check.Equal("queued", runner.LastInput!.Value.GetProperty("query").GetString());
}

static async Task ScriptRunnerRunsPowerShell()
{
    var module = new ScriptRunnerModule();
    var library = new RecordingLibraryStore();
    var process = new CapturingProcessService();
    var capabilities = new AutomationCapabilityRegistry(
        libraryStoreFactory: _ => library,
        processServiceFactory: _ => process);
    var registry = new AutomationModuleRegistry([module], capabilities);
    await using var context = capabilities.CreateContext(new AutomationModuleDescriptor(module.Id, module.Definition.Capabilities));
    using var profile = System.Text.Json.JsonDocument.Parse("""
        {"id":"daily-powershell","name":"Daily PowerShell","interpreter":"powershell","interpreterPath":"C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe","scriptPath":"C:\\scripts\\daily report.ps1","arguments":["-Mode","value with spaces"],"workingDirectory":"C:\\scripts","outputMode":"text","timeoutSeconds":30}
        """);
    var saved = await registry.DispatchAsync(module.Id, "saveProfile", 1, 1, profile.RootElement, module.Id, context, [], CancellationToken.None);
    Check.Equal(AutomationStatus.Success, saved.Status);

    using var runInput = System.Text.Json.JsonDocument.Parse("{\"id\":\"daily-powershell\"}");
    var result = await registry.DispatchAsync(module.Id, "runProfile", 1, 1, runInput.RootElement, module.Id, context, [], CancellationToken.None);
    Check.Equal(AutomationStatus.Success, result.Status);
    Check.Equal("C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe", process.LastRequest!.ExecutablePath);
    Check.Equal(7, process.LastRequest.Arguments.Count);
    Check.Equal("-NoLogo", process.LastRequest.Arguments[0]);
    Check.Equal("-NoProfile", process.LastRequest.Arguments[1]);
    Check.Equal("-NonInteractive", process.LastRequest.Arguments[2]);
    Check.Equal("-File", process.LastRequest.Arguments[3]);
    Check.Equal("C:\\scripts\\daily report.ps1", process.LastRequest.Arguments[4]);
    Check.Equal("-Mode", process.LastRequest.Arguments[5]);
    Check.Equal("value with spaces", process.LastRequest.Arguments[6]);
}

static async Task ScriptRunnerRejectsInvalidProfiles()
{
    var module = new ScriptRunnerModule();
    var library = new RecordingLibraryStore();
    var capabilities = new AutomationCapabilityRegistry(libraryStoreFactory: _ => library);
    var registry = new AutomationModuleRegistry([module], capabilities);
    await using var context = capabilities.CreateContext(new AutomationModuleDescriptor(module.Id, [new(AutomationCapabilityIds.LibraryStorage, 1)]));
    using var badExtension = System.Text.Json.JsonDocument.Parse("""
        {"id":"bad-script","name":"Bad script","interpreter":"python","interpreterPath":"C:\\Python\\python.exe","scriptPath":"C:\\scripts\\report.txt","arguments":[],"workingDirectory":"C:\\scripts","outputMode":"text","timeoutSeconds":30}
        """);
    var result = await registry.DispatchAsync(module.Id, "saveProfile", 1, 1, badExtension.RootElement,
        module.Id, context, [], CancellationToken.None);
    Check.Equal(AutomationStatus.Error, result.Status);
    Check.True(result.Message.Contains(".py", StringComparison.Ordinal));

    using var badPowerShellExtension = System.Text.Json.JsonDocument.Parse("""
        {"id":"bad-powershell","name":"Bad PowerShell","interpreter":"powershell","interpreterPath":"C:\\PowerShell\\pwsh.exe","scriptPath":"C:\\scripts\\task.psm1","arguments":[],"workingDirectory":"C:\\scripts","outputMode":"text","timeoutSeconds":30}
        """);
    result = await registry.DispatchAsync(module.Id, "saveProfile", 1, 1, badPowerShellExtension.RootElement,
        module.Id, context, [], CancellationToken.None);
    Check.Equal(AutomationStatus.Error, result.Status);
    Check.True(result.Message.Contains(".ps1", StringComparison.Ordinal));

    using var badTimeout = System.Text.Json.JsonDocument.Parse("""
        {"id":"too-long","name":"Long timeout","interpreter":"bash","interpreterPath":"C:\\Git\\bin\\bash.exe","scriptPath":"C:\\scripts\\task.sh","arguments":[],"workingDirectory":"C:\\scripts","outputMode":"text","timeoutSeconds":3601}
        """);
    result = await registry.DispatchAsync(module.Id, "saveProfile", 1, 1, badTimeout.RootElement,
        module.Id, context, [], CancellationToken.None);
    Check.Equal(AutomationStatus.Error, result.Status);
    Check.True(result.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase));
}

static async Task ScriptRunnerReportsProcessWarnings()
{
    var module = new ScriptRunnerModule();
    var library = new RecordingLibraryStore();
    var process = new CapturingProcessService();
    var capabilities = new AutomationCapabilityRegistry(libraryStoreFactory: _ => library, processServiceFactory: _ => process);
    var registry = new AutomationModuleRegistry([module], capabilities);
    await using var context = capabilities.CreateContext(new AutomationModuleDescriptor(module.Id, module.Definition.Capabilities));
    using var profile = System.Text.Json.JsonDocument.Parse("""
        {"id":"warnings","name":"Warnings","interpreter":"python","interpreterPath":"C:\\Python\\python.exe","scriptPath":"C:\\scripts\\warnings.py","arguments":[],"workingDirectory":"C:\\scripts","outputMode":"json","timeoutSeconds":30}
        """);
    await registry.DispatchAsync(module.Id, "saveProfile", 1, 1, profile.RootElement,
        module.Id, context, [], CancellationToken.None);
    using var input = System.Text.Json.JsonDocument.Parse("{\"id\":\"warnings\"}");

    process.Result = new AutomationProcessResult(null, true, "partial", "", false, false, 30_000);
    var result = await registry.DispatchAsync(module.Id, "runProfile", 1, 1, input.RootElement,
        module.Id, context, [], CancellationToken.None);
    Check.Equal(AutomationStatus.Warning, result.Status);
    Check.True(result.Data.GetProperty("timedOut").GetBoolean());

    process.Result = new AutomationProcessResult(0, false, "not json", "warning output", false, false, 14);
    result = await registry.DispatchAsync(module.Id, "runProfile", 1, 1, input.RootElement,
        module.Id, context, [], CancellationToken.None);
    Check.Equal(AutomationStatus.Warning, result.Status);
    Check.True(result.Data.GetProperty("parseFailure").GetBoolean());
    Check.Equal("warning output", result.Data.GetProperty("stderr").GetString());
}

static async Task ContextDisposalScopesHttpCalls()
{
    var http = new BlockingHttpClient();
    var registry = new AutomationCapabilityRegistry(httpClientFactory: (_, _) => http);
    var context = registry.CreateContext(new AutomationModuleDescriptor(
        "http-module", [new(AutomationCapabilityIds.HttpRequest, 1)], new AutomationHttpPolicy(["example.test"])));
    var request = context.Http!.SendAsync(new AutomationHttpRequest(
        "https://example.test/pending", "GET", new Dictionary<string, string>(), string.Empty), CancellationToken.None);
    var observedToken = await http.FirstCallStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

    await context.DisposeAsync();
    Check.True(observedToken.IsCancellationRequested);
    await Check.ThrowsAsync<ObjectDisposedException>(() => context.Http.SendAsync(new AutomationHttpRequest(
        "https://example.test/after-dispose", "GET", new Dictionary<string, string>(), string.Empty), CancellationToken.None));
    await Check.ThrowsAsync<OperationCanceledException>(() => request);
    Check.Equal(1, http.CallCount);
}

static async Task DisposedContextWaitsForKeyboardCleanup()
{
    var keyboard = new GatedKeyboardInput();
    var registry = new AutomationCapabilityRegistry(_ => keyboard);
    var context = registry.CreateContext(new AutomationModuleDescriptor(
        "keyboard-module", [new(AutomationCapabilityIds.KeyboardInput, 1)]));
    var subscribing = context.KeyboardInput!.SubscribeAsync(_ => ValueTask.CompletedTask, CancellationToken.None).AsTask();
    await keyboard.SubscribeStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

    await context.DisposeAsync();
    keyboard.CompleteSubscribe.TrySetResult();
    await keyboard.CleanupStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
    Exception? completedEarly = null;
    try { await subscribing.WaitAsync(TimeSpan.FromMilliseconds(50)); }
    catch (TimeoutException) { }
    catch (Exception exception) { completedEarly = exception; }
    keyboard.CompleteCleanup.TrySetResult();
    if (completedEarly is not null)
        throw new InvalidOperationException("SubscribeAsync returned before rejected subscription cleanup completed.", completedEarly);
    await Check.ThrowsAsync<ObjectDisposedException>(async () => await subscribing);
    Check.Equal(1, keyboard.DisposeCount);
}

static async Task KeyboardUnsubscribeDisposesScopedSubscriptions()
{
    var keyboard = new RecordingKeyboardInput();
    var registry = new AutomationCapabilityRegistry(_ => keyboard);
    await using var context = registry.CreateContext(new AutomationModuleDescriptor(
        "keyboard-module", [new(AutomationCapabilityIds.KeyboardInput, 1)]));
    _ = await context.KeyboardInput!.SubscribeAsync(_ => ValueTask.CompletedTask, CancellationToken.None);
    await context.UnsubscribeKeyboardAsync();
    Check.Equal(1, keyboard.DisposeCount);
}

static async Task KeyboardDeliveryRequiresTrustedContext()
{
    var blockedStates = new[]
    {
        new KeyboardDeliveryState("other", true, true, true, false, false),
        new KeyboardDeliveryState("launcher", false, true, true, false, false),
        new KeyboardDeliveryState("launcher", true, false, true, false, false),
        new KeyboardDeliveryState("launcher", true, true, false, false, false),
        new KeyboardDeliveryState("launcher", true, true, true, true, false),
        new KeyboardDeliveryState("launcher", true, true, true, false, true),
    };

    foreach (var blockedState in blockedStates)
    {
        var hub = new AutomationKeyboardEventHub();
        var delivered = 0;
        hub.SetDeliveryContext(new KeyboardDeliveryState("launcher", true, true, true, false, false));
        await using var subscription = await hub.SubscribeAsync("launcher", _ =>
        {
            delivered++;
            return ValueTask.CompletedTask;
        }, CancellationToken.None);

        hub.SetDeliveryContext(blockedState);
        hub.Publish(Key(1));
        Check.Equal(0, hub.ActiveSubscriptionCount);
        Check.Equal(0, delivered);
        hub.SetDeliveryContext(new KeyboardDeliveryState("launcher", true, true, true, false, false));
        hub.Publish(Key(2));
        Check.Equal(0, delivered);
    }

    var activeHub = new AutomationKeyboardEventHub();
    activeHub.SetDeliveryContext(new KeyboardDeliveryState("launcher", true, true, true, false, false));
    var received = new List<KeyInputEvent>();
    await using var activeSubscription = await activeHub.SubscribeAsync("launcher", item =>
    {
        received.Add(item);
        return ValueTask.CompletedTask;
    }, CancellationToken.None);
    activeHub.Publish(Key(7));
    await Check.EventuallyAsync(() => received.Count == 1);
    Check.Equal(7L, received[0].Sequence);
}

static async Task KeyboardSubscribersAreRevokedAndOverflowIsDetached()
{
    var hub = new AutomationKeyboardEventHub();
    hub.SetDeliveryContext(new KeyboardDeliveryState("slow", true, true, true, false, false));
    var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var delivered = 0;
    var subscription = await hub.SubscribeAsync("slow", async _ =>
    {
        Interlocked.Increment(ref delivered);
        firstEntered.TrySetResult();
        await release.Task;
    }, CancellationToken.None);
    hub.Publish(Key(1));
    await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
    for (var sequence = 2; sequence <= 260; sequence++) hub.Publish(Key(sequence));
    release.TrySetResult();
    await subscription.DisposeAsync();

    Check.True(hub.ActiveSubscriptionCount == 0);
    Check.True(delivered < 260);

    var revoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var oldDeliveryCount = 0;
    var revokedSubscription = await hub.SubscribeAsync("slow", _ =>
    {
        Interlocked.Increment(ref oldDeliveryCount);
        revoked.TrySetResult();
        return ValueTask.CompletedTask;
    }, CancellationToken.None);
    hub.Publish(Key(261));
    await revoked.Task.WaitAsync(TimeSpan.FromSeconds(2));
    hub.SetDeliveryContext(new KeyboardDeliveryState("other", true, true, true, false, false));
    await Check.EventuallyAsync(() => hub.ActiveSubscriptionCount == 0);
    hub.SetDeliveryContext(new KeyboardDeliveryState("slow", true, true, true, false, false));
    var freshDelivery = new TaskCompletionSource<KeyInputEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
    var freshSubscription = await hub.SubscribeAsync("slow", item =>
    {
        freshDelivery.TrySetResult(item);
        return ValueTask.CompletedTask;
    }, CancellationToken.None);
    hub.Publish(Key(262));
    Check.Equal(262L, (await freshDelivery.Task.WaitAsync(TimeSpan.FromSeconds(2))).Sequence);
    Check.Equal(1, oldDeliveryCount);
    await freshSubscription.DisposeAsync();
    await revokedSubscription.DisposeAsync();
}

static async Task ConcurrentKeyboardRevocationAndDisposalAreSafe()
{
    for (var iteration = 0; iteration < 128; iteration++)
    {
        var hub = new AutomationKeyboardEventHub();
        hub.SetDeliveryContext(new KeyboardDeliveryState("module", true, true, true, false, false));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var subscription = await hub.SubscribeAsync("module", async _ =>
        {
            entered.TrySetResult();
            await release.Task;
        }, CancellationToken.None);
        hub.Publish(Key(1000 + iteration));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        for (var sequence = 0; sequence < AutomationKeyboardEventHub.SubscriberCapacity; sequence++)
            hub.Publish(Key(2000 + sequence));

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var revoke = Task.Run(async () =>
        {
            await start.Task;
            hub.SetDeliveryContext(new KeyboardDeliveryState("other", true, true, true, false, false));
        });
        var dispose = Task.Run(async () =>
        {
            await start.Task;
            await subscription.DisposeAsync();
        });
        start.TrySetResult();
        release.TrySetResult();
        await Task.WhenAll(revoke, dispose).WaitAsync(TimeSpan.FromSeconds(2));
        Check.Equal(0, hub.ActiveSubscriptionCount);
        await subscription.DisposeAsync();
    }
}

static async Task RequestExecutorCancelsRequestsByScope()
{
    var executor = new AutomationRequestExecutor();
    using var requestStarted = new CancellationTokenSource();
    var requestObserved = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
    var one = executor.ExecuteAsync("module-a", "one", CancellationToken.None, token =>
    {
        requestObserved.TrySetResult(token);
        requestStarted.Token.Register(() => { });
        return WaitForCancellation(token);
    });
    var oneToken = await requestObserved.Task.WaitAsync(TimeSpan.FromSeconds(2));
    Check.True(executor.Cancel("module-a", "one"));
    await Check.ThrowsAsync<OperationCanceledException>(() => one);
    Check.True(oneToken.IsCancellationRequested);

    var twoObserved = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
    var two = executor.ExecuteAsync("module-a", "two", CancellationToken.None, token =>
    {
        twoObserved.TrySetResult(token);
        return WaitForCancellation(token);
    });
    var threeObserved = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
    var three = executor.ExecuteAsync("module-b", "three", CancellationToken.None, token =>
    {
        threeObserved.TrySetResult(token);
        return WaitForCancellation(token);
    });
    var twoToken = await twoObserved.Task.WaitAsync(TimeSpan.FromSeconds(2));
    var threeToken = await threeObserved.Task.WaitAsync(TimeSpan.FromSeconds(2));
    Check.True(executor.CancelModule("module-a") == 1);
    await Check.ThrowsAsync<OperationCanceledException>(() => two);
    Check.True(twoToken.IsCancellationRequested);
    Check.False(threeToken.IsCancellationRequested);
    Check.True(executor.CancelAll() == 1);
    await Check.ThrowsAsync<OperationCanceledException>(() => three);
}

static async Task RequestExecutorRegistersBeforeImmediateCancellation()
{
    var executor = new AutomationRequestExecutor();
    var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
    var request = executor.ExecuteAsync("module", "immediate", CancellationToken.None, token =>
    {
        started.TrySetResult(token);
        return WaitForCancellation(token);
    });

    Check.True(executor.Cancel("module", "immediate"));
    await Check.ThrowsAsync<OperationCanceledException>(() => request);
    Check.True((await started.Task.WaitAsync(TimeSpan.FromSeconds(2))).IsCancellationRequested);
}

static async Task SavedProfileExecutionUsesRegisteredHandlers()
{
    var handler = new FixtureSavedProfileHandler();
    var executor = new AutomationSavedProfileExecutor([handler]);
    using var input = JsonDocument.Parse("{\"payload\":{\"answer\":42}} ");
    var metadata = new AutomationExecutionMetadata(AutomationExecutionOrigin.Workflow, "workflow-run-42");
    var result = await executor.RunAsync("script-runner", "profile-1", input.RootElement, metadata, CancellationToken.None);

    Check.Equal("profile-1", handler.ProfileId);
    Check.Equal(AutomationExecutionOrigin.Workflow, handler.Metadata!.Origin);
    Check.Equal("workflow-run-42", handler.Metadata.CorrelationId);
    Check.Equal(42, handler.Input!.Value.GetProperty("payload").GetProperty("answer").GetInt32());
    Check.Equal("completed", result.Summary.Category);
    Check.Equal("not-for-history", result.Output.GetProperty("rawValue").GetString());

    await Check.ThrowsAsync<InvalidOperationException>(() => executor.RunAsync("unregistered", "profile-1",
        input.RootElement, metadata, CancellationToken.None));
}

static async Task SavedProfileCatalogsExposeSafeMetadata()
{
    var executor = new AutomationSavedProfileExecutor([new FixtureSavedProfileHandler()]);
    var profiles = await executor.ListProfilesAsync("script-runner", CancellationToken.None);
    Check.Equal(1, profiles.Count);
    Check.Equal("profile-1", profiles[0].ProfileId);
    Check.Equal("Example Script", profiles[0].Name);
    await Check.ThrowsAsync<InvalidOperationException>(() => executor.ListProfilesAsync("api", CancellationToken.None));
}

static async Task WorkflowEngineMapsProfilesAndPersistsSafeHistory()
{
    var store = new FixtureWorkflowLibraryStore();
    var literal = JsonDocument.Parse("\"literal\"");
    var sourceHandler = new FixtureWorkflowProfileHandler("script-runner", "source", "Source script",
        JsonDocument.Parse("{\"value\":42,\"sensitiveOutput\":\"never-persist\"}").RootElement.Clone());
    var destinationHandler = new FixtureWorkflowProfileHandler("api", "destination", "Destination API",
        JsonDocument.Parse("{\"accepted\":true}").RootElement.Clone());
    destinationHandler.ExpectedInputProperty = "answer";
    var workflow = new AutomationWorkflowProfile("workflow-1", "Map data", [
        new AutomationWorkflowStep("source-step", "script-runner", "source", []),
        new AutomationWorkflowStep("destination-step", "api", "destination", [
            new AutomationWorkflowInputBinding("/answer", null, "source-step", "/value"),
            new AutomationWorkflowInputBinding("/constant", literal.RootElement.Clone(), null, null, LiteralPresent: true)
        ])
    ]);
    await store.UpsertAsync(new AutomationLibraryRecord(AutomationWorkflowEngine.ModuleId,
        AutomationWorkflowEngine.ProfileCollection, workflow.Id, 1, JsonSerializer.SerializeToElement(workflow), DateTimeOffset.UtcNow),
        CancellationToken.None);

    var executor = new AutomationSavedProfileExecutor([sourceHandler, destinationHandler]);
    var engine = new AutomationWorkflowEngine(store, executor);
    using var initialInput = JsonDocument.Parse("{\"start\":true}");
    var run = await engine.RunAsync(workflow.Id, initialInput.RootElement, CancellationToken.None);

    Check.Equal(AutomationStatus.Success, run.Summary.Status);
    Check.Equal(2, run.Steps.Count);
    Check.Equal(42, destinationHandler.Input!.Value.GetProperty("answer").GetInt32());
    Check.Equal("literal", destinationHandler.Input.Value.GetProperty("constant").GetString());
    Check.Equal(AutomationExecutionOrigin.Workflow, destinationHandler.Metadata!.Origin);

    var history = await store.ListAsync(AutomationWorkflowEngine.ModuleId,
        AutomationWorkflowEngine.RunHistoryCollection, CancellationToken.None);
    Check.Equal(1, history.Count);
    var savedHistory = history[0].Data.GetRawText();
    Check.False(savedHistory.Contains("sensitiveOutput", StringComparison.Ordinal));
    Check.False(savedHistory.Contains("never-persist", StringComparison.Ordinal));
    Check.False(savedHistory.Contains("\"value\":42", StringComparison.Ordinal));
}

static async Task WorkflowEngineStopsAfterFailure()
{
    var store = new FixtureWorkflowLibraryStore();
    var first = new FixtureWorkflowProfileHandler("script-runner", "fails", "Failing script",
        JsonDocument.Parse("{\"value\":1}").RootElement.Clone(), AutomationStatus.Error);
    var second = new FixtureWorkflowProfileHandler("api", "never", "Never called",
        JsonDocument.Parse("{}").RootElement.Clone());
    var workflow = new AutomationWorkflowProfile("workflow-stop", "Stop on failure", [
        new AutomationWorkflowStep("first", "script-runner", "fails", []),
        new AutomationWorkflowStep("second", "api", "never", [])
    ]);
    await store.UpsertAsync(new AutomationLibraryRecord(AutomationWorkflowEngine.ModuleId,
        AutomationWorkflowEngine.ProfileCollection, workflow.Id, 1, JsonSerializer.SerializeToElement(workflow), DateTimeOffset.UtcNow),
        CancellationToken.None);
    var engine = new AutomationWorkflowEngine(store, new AutomationSavedProfileExecutor([first, second]));

    var result = await engine.RunAsync(workflow.Id, null, CancellationToken.None);
    Check.Equal(AutomationStatus.Error, result.Summary.Status);
    Check.Equal(1, result.Steps.Count);
    Check.Equal(0, second.ExecutionCount);
}

static async Task CanceledWorkflowPersistsSafeHistory()
{
    var store = new FixtureWorkflowLibraryStore();
    var waiting = new FixtureWorkflowProfileHandler("script-runner", "slow", "Slow script",
        JsonDocument.Parse("{\"sensitive\":\"not-persisted\"}").RootElement.Clone()) { WaitForCancellation = true };
    var workflow = new AutomationWorkflowProfile("workflow-cancel", "Cancel me", [
        new AutomationWorkflowStep("slow-step", "script-runner", "slow", [])
    ]);
    await store.UpsertAsync(new AutomationLibraryRecord(AutomationWorkflowEngine.ModuleId,
        AutomationWorkflowEngine.ProfileCollection, workflow.Id, 1, JsonSerializer.SerializeToElement(workflow), DateTimeOffset.UtcNow),
        CancellationToken.None);
    var engine = new AutomationWorkflowEngine(store, new AutomationSavedProfileExecutor([waiting]));
    using var cancellation = new CancellationTokenSource();

    var run = engine.RunAsync(workflow.Id, null, cancellation.Token);
    await waiting.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
    cancellation.Cancel();
    await Check.ThrowsAsync<OperationCanceledException>(() => run);
    var history = await store.ListAsync(AutomationWorkflowEngine.ModuleId,
        AutomationWorkflowEngine.RunHistoryCollection, CancellationToken.None);
    Check.Equal(1, history.Count);
    Check.True(history[0].Data.GetRawText().Contains("canceled", StringComparison.Ordinal));
    Check.False(history[0].Data.GetRawText().Contains("not-persisted", StringComparison.Ordinal));
}

static async Task ServiceWorkDoesNotBlockLauncherCommands()
{
    var executor = new AutomationRequestExecutor();
    var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var service = executor.ExecuteAsync("module", "request", CancellationToken.None, async token =>
    {
        started.TrySetResult();
        await release.Task.WaitAsync(token);
        return true;
    });
    await started.Task.WaitAsync(TimeSpan.FromSeconds(2));

    await using var commands = new SerializedCommandQueue();
    var command = await commands.EnqueueAsync(() => ValueTask.FromResult("launcher-responsive"))
        .WaitAsync(TimeSpan.FromSeconds(2));
    Check.Equal("launcher-responsive", command);
    release.TrySetResult();
    Check.True(await service.WaitAsync(TimeSpan.FromSeconds(2)));
}

static KeyInputEvent Key(long sequence) => new(sequence, "KeyA", 0x41, true, false, AutomationKeyModifiers.None);

static async Task<bool> WaitForCancellation(CancellationToken token)
{
    await Task.Delay(Timeout.InfiniteTimeSpan, token);
    return true;
}

static async Task PendingAliasDelayCanBeCancelled()
{
    var delay = new GatedDelay();
    var activator = new RecordingActivator();
    using var scheduler = new AliasLaunchScheduler(delay, activator);

    var scheduled = scheduler.ScheduleAsync(Binding("app", "App", @"C:\Apps\App.exe", "a"), "0x1234");
    var delayToken = await delay.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
    scheduler.CancelPending();
    await scheduled.WaitAsync(TimeSpan.FromSeconds(2));

    Check.True(delayToken.IsCancellationRequested);
    Check.Equal(0, activator.CallCount);
}

static async Task QueryChangeDoesNotCancelActivation()
{
    var delay = new GatedDelay();
    var activator = new RecordingActivator();
    using var scheduler = new AliasLaunchScheduler(delay, activator);

    var scheduled = scheduler.ScheduleAsync(Binding("app", "App", @"C:\Apps\App.exe", "a"), "0x123456789ABCDEF0");
    await delay.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
    delay.Release.TrySetResult(true);
    var activationToken = await activator.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));

    scheduler.CancelPending();
    Check.False(activationToken.IsCancellationRequested);
    Check.Equal("0x123456789ABCDEF0", activator.PreviousForegroundHwnd);
    Check.Equal(TimeSpan.FromMilliseconds(420), delay.RequestedDelay);

    activator.Release.TrySetResult(true);
    await scheduled.WaitAsync(TimeSpan.FromSeconds(2));
    Check.Equal(1, activator.CallCount);
}

static LauncherSettings CreateSettings(params AppBinding[] bindings) => new()
{
    Hotkey = "RightControl",
    Theme = AppTheme.Light,
    StartWithWindows = true,
    Bindings = bindings.ToList(),
};

static AppBinding Binding(string id, string name, string path, string alias, string arguments = "") =>
    new(id, name, path, alias, arguments);

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
        {
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
        }
    }

    public static void Throws<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    public static async Task ThrowsAsync<TException>(Func<Task> action) where TException : Exception
    {
        try { await action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    public static async Task EventuallyAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(5);
        if (!condition()) throw new InvalidOperationException("Condition was not reached before timeout.");
    }
}

sealed class RecordingKeyboardInput : IAutomationKeyboardInput
{
    private int _disposeCount;
    public int DisposeCount => Volatile.Read(ref _disposeCount);
    public ValueTask<IAsyncDisposable> SubscribeAsync(Func<KeyInputEvent, ValueTask> receiver, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IAsyncDisposable>(new AsyncDisposableAction(() =>
        {
            Interlocked.Increment(ref _disposeCount);
            return ValueTask.CompletedTask;
        }));
}

sealed class RecordingHttpClient : IAutomationHttpClient
{
    public Task<AutomationHttpResult> SendAsync(AutomationHttpRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new AutomationHttpResult(200, new Dictionary<string, string>(), "text/plain", "ok"));
}

sealed class RecordingLibraryStore : IAutomationLibraryStore
{
    public AutomationLibraryRecord? LastWrite { get; private set; }
    public Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(string moduleId, string collection, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AutomationLibraryRecord>>(LastWrite is { } item && item.ModuleId == moduleId && item.Collection == collection ? [item] : []);
    public Task<AutomationLibraryRecord?> GetAsync(string moduleId, string collection, string id, CancellationToken cancellationToken) =>
        Task.FromResult(LastWrite is { } item && item.ModuleId == moduleId && item.Collection == collection && item.Id == id ? item : null);
    public Task UpsertAsync(AutomationLibraryRecord record, CancellationToken cancellationToken)
    {
        LastWrite = record;
        return Task.CompletedTask;
    }
    public Task<bool> DeleteAsync(string moduleId, string collection, string id, CancellationToken cancellationToken)
    {
        if (LastWrite is not { } item || item.ModuleId != moduleId || item.Collection != collection || item.Id != id)
            return Task.FromResult(false);
        LastWrite = null;
        return Task.FromResult(true);
    }
}

sealed class RecordingWebsiteLauncher : IAutomationWebsiteLauncher
{
    public IReadOnlyList<IReadOnlyList<Uri>>? Groups { get; private set; }
    public Task LaunchAsync(IReadOnlyList<IReadOnlyList<Uri>> groups, CancellationToken cancellationToken)
    {
        Groups = groups.Select(group => (IReadOnlyList<Uri>)group.ToArray()).ToArray();
        return Task.CompletedTask;
    }
}

sealed class RecordingProcessService : IAutomationProcessService
{
    public TaskCompletionSource<CancellationToken> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task<AutomationProcessResult> ExecuteAsync(AutomationProcessRequest request, CancellationToken cancellationToken)
    {
        Started.TrySetResult(cancellationToken);
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return new AutomationProcessResult(0, false, string.Empty, string.Empty, false, false, 0);
    }
}

sealed class CapturingSecretManager : IAutomationSecretManager
{
    public Task SetAsync(string profileId, string secretId, string value, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<bool> ExistsAsync(string profileId, string secretId, CancellationToken cancellationToken) => Task.FromResult(false);
    public Task DeleteAsync(string profileId, string secretId, CancellationToken cancellationToken) => Task.CompletedTask;
}

sealed class CapturingApiProfileRunner : IAutomationApiProfileRunner
{
    public JsonElement? LastInput { get; private set; }

    public Task<AutomationApiProfileResponse> RunProfileAsync(string profileId, JsonElement? input, CancellationToken cancellationToken)
    {
        LastInput = input?.Clone();
        return Task.FromResult(new AutomationApiProfileResponse(200, "application/json",
            new Dictionary<string, string>(), "{}", 1));
    }
}

sealed class CapturingProcessService : IAutomationProcessService
{
    public AutomationProcessRequest? LastRequest { get; private set; }
    public AutomationProcessResult Result { get; set; } = new(0, false, "{\"generated\":true}", string.Empty, false, false, 22);

    public Task<AutomationProcessResult> ExecuteAsync(AutomationProcessRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(Result);
    }
}

sealed class RecordingFocusCoordinator : IAutomationFocusSessionCoordinator
{
    private AutomationFocusSessionState _state = AutomationFocusSessionState.Idle;
    private AutomationFocusPhase? _phase;
    private string? _sessionId;
    private AutomationFocusSettings? _activeSettings;

    public AutomationFocusSettings Settings { get; private set; } = new(25, 5);
    public int SettingsSaveCount { get; private set; }
    public List<string> Calls { get; } = [];

    public Task<AutomationFocusSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add("getSnapshot");
        return Task.FromResult(Snapshot());
    }

    public Task<AutomationFocusSessionSnapshot> SaveSettingsAsync(AutomationFocusSettings settings, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add("saveSettings");
        Settings = settings;
        SettingsSaveCount++;
        return Task.FromResult(Session());
    }

    public Task<AutomationFocusSessionSnapshot> StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add("start");
        _state = AutomationFocusSessionState.Running;
        _phase = AutomationFocusPhase.Focus;
        _sessionId = "focus-session-1";
        _activeSettings = Settings;
        return Task.FromResult(Session());
    }

    public Task<AutomationFocusSessionSnapshot> PauseAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add("pause");
        _state = AutomationFocusSessionState.Paused;
        return Task.FromResult(Session());
    }

    public Task<AutomationFocusSessionSnapshot> ResumeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add("resume");
        _state = AutomationFocusSessionState.Running;
        return Task.FromResult(Session());
    }

    public Task<AutomationFocusSessionSnapshot> SkipAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add("skip");
        _phase = _phase == AutomationFocusPhase.Focus ? AutomationFocusPhase.Break : AutomationFocusPhase.Focus;
        return Task.FromResult(Session());
    }

    public Task<AutomationFocusSessionSnapshot> EndAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add("end");
        _state = AutomationFocusSessionState.Idle;
        _phase = null;
        _sessionId = null;
        _activeSettings = null;
        return Task.FromResult(Session());
    }

    private AutomationFocusSnapshot Snapshot() => new(Session(), [], Settings);

    private AutomationFocusSessionSnapshot Session() => new(_state, _sessionId, _phase,
        _state == AutomationFocusSessionState.Running ? DateTimeOffset.UtcNow.AddMinutes(25) : null,
        _state == AutomationFocusSessionState.Paused ? 300_000 : null,
        _phase == AutomationFocusPhase.Break ? 1 : 0, _activeSettings ?? Settings);
}

sealed class RecordingWorkTimeCoordinator : IAutomationWorkTimeCoordinator
{
    public List<string> Calls { get; } = [];
    public int SaveCount { get; private set; }
    public int UpdateCount { get; private set; }
    public int DeleteCount { get; private set; }
    public string? LastDescription { get; private set; }
    public string? LastUpdatedId { get; private set; }
    public string? LastDeletedId { get; private set; }
    public IReadOnlyList<string> LastTags { get; private set; } = [];

    public Task<AutomationWorkTimeSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add("getSnapshot");
        return Task.FromResult(Snapshot());
    }

    public Task<AutomationWorkTimeSnapshot> StartAsync(CancellationToken cancellationToken) => RecordAsync("start", cancellationToken);
    public Task<AutomationWorkTimeSnapshot> StopAsync(CancellationToken cancellationToken) => RecordAsync("stop", cancellationToken);
    public Task<AutomationWorkTimeSnapshot> ResumePendingAsync(CancellationToken cancellationToken) => RecordAsync("resumePending", cancellationToken);
    public Task<AutomationWorkTimeSnapshot> DiscardPendingAsync(CancellationToken cancellationToken) => RecordAsync("discardPending", cancellationToken);

    public Task<AutomationWorkTimeSnapshot> SaveEntryAsync(string description, IReadOnlyList<string> tags, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add("saveEntry");
        SaveCount++;
        LastDescription = description;
        LastTags = tags.ToArray();
        return Task.FromResult(Snapshot());
    }

    public Task<AutomationWorkTimeSnapshot> UpdateEntryAsync(string id, string description, IReadOnlyList<string> tags, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add("updateEntry");
        UpdateCount++;
        LastUpdatedId = id;
        LastDescription = description;
        LastTags = tags.ToArray();
        return Task.FromResult(Snapshot());
    }

    public Task<AutomationWorkTimeSnapshot> DeleteEntryAsync(string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add("deleteEntry");
        DeleteCount++;
        LastDeletedId = id;
        return Task.FromResult(Snapshot());
    }

    private Task<AutomationWorkTimeSnapshot> RecordAsync(string action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add(action);
        return Task.FromResult(Snapshot());
    }

    private static AutomationWorkTimeSnapshot Snapshot() => new(null, null, []);
}

sealed class BlockingHttpClient : IAutomationHttpClient
{
    private int _callCount;
    public int CallCount => Volatile.Read(ref _callCount);
    public TaskCompletionSource<CancellationToken> FirstCallStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task<AutomationHttpResult> SendAsync(AutomationHttpRequest request, CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref _callCount) == 1)
        {
            FirstCallStarted.TrySetResult(cancellationToken);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
        return new AutomationHttpResult(200, new Dictionary<string, string>(), "text/plain", "ok");
    }
}

sealed class GatedKeyboardInput : IAutomationKeyboardInput
{
    private int _disposeCount;
    public int DisposeCount => Volatile.Read(ref _disposeCount);
    public TaskCompletionSource SubscribeStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource CompleteSubscribe { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource CleanupStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource CompleteCleanup { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async ValueTask<IAsyncDisposable> SubscribeAsync(Func<KeyInputEvent, ValueTask> receiver, CancellationToken cancellationToken)
    {
        SubscribeStarted.TrySetResult();
        await CompleteSubscribe.Task;
        return new AsyncDisposableAction(async () =>
        {
            Interlocked.Increment(ref _disposeCount);
            CleanupStarted.TrySetResult();
            await CompleteCleanup.Task;
        });
    }
}

sealed class AsyncDisposableAction(Func<ValueTask> dispose) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => dispose();
}

sealed class GatedDelay : ILaunchDelay
{
    public TaskCompletionSource<CancellationToken> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TimeSpan RequestedDelay { get; private set; }

    public async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        RequestedDelay = delay;
        Started.TrySetResult(cancellationToken);
        await Release.Task.WaitAsync(cancellationToken);
    }
}

sealed class RecordingActivator : IApplicationActivator
{
    public TaskCompletionSource<CancellationToken> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public string? PreviousForegroundHwnd { get; private set; }
    public int CallCount { get; private set; }

    public async Task ActivateAsync(AppBinding binding, string? previousForegroundHwnd, CancellationToken cancellationToken)
    {
        CallCount++;
        PreviousForegroundHwnd = previousForegroundHwnd;
        Started.TrySetResult(cancellationToken);
        await Release.Task.WaitAsync(cancellationToken);
    }
}

sealed class FixtureSavedProfileHandler : IAutomationSavedProfileHandler
{
    public string ModuleId => "script-runner";
    public string? ProfileId { get; private set; }
    public JsonElement? Input { get; private set; }
    public AutomationExecutionMetadata? Metadata { get; private set; }

    public Task<IReadOnlyList<AutomationSavedProfileSummary>> ListProfilesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<AutomationSavedProfileSummary> profiles = [new("profile-1", "Example Script")];
        return Task.FromResult(profiles);
    }

    public Task<AutomationProfileExecutionOutput> ExecuteAsync(string profileId, JsonElement? input,
        AutomationExecutionMetadata metadata, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ProfileId = profileId;
        Input = input?.Clone();
        Metadata = metadata;
        using var output = JsonDocument.Parse("{\"rawValue\":\"not-for-history\"}");
        return Task.FromResult(new AutomationProfileExecutionOutput(output.RootElement.Clone(),
            new AutomationExecutionSummary(AutomationStatus.Success, "completed", 8)));
    }
}

sealed class FixtureWorkflowProfileHandler(
    string moduleId,
    string profileId,
    string name,
    JsonElement output,
    AutomationStatus status = AutomationStatus.Success) : IAutomationSavedProfileHandler
{
    public string ModuleId { get; } = moduleId;
    public JsonElement? Input { get; private set; }
    public AutomationExecutionMetadata? Metadata { get; private set; }
    public string? ExpectedInputProperty { get; set; }
    public int ExecutionCount { get; private set; }
    public bool WaitForCancellation { get; set; }
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<IReadOnlyList<AutomationSavedProfileSummary>> ListProfilesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<AutomationSavedProfileSummary> result = [new(profileId, name)];
        return Task.FromResult(result);
    }

    public Task<AutomationProfileExecutionOutput> ExecuteAsync(string id, JsonElement? input,
        AutomationExecutionMetadata metadata, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Check.Equal(profileId, id);
        ExecutionCount++;
        Input = input?.Clone();
        Metadata = metadata;
        if (ExpectedInputProperty is { } property) Check.True(input?.TryGetProperty(property, out _) == true);
        if (WaitForCancellation)
        {
            Started.TrySetResult();
            return WaitAndCancelAsync(cancellationToken);
        }
        return Task.FromResult(new AutomationProfileExecutionOutput(output.Clone(),
            new AutomationExecutionSummary(status, status == AutomationStatus.Success ? "completed" : "nonzero-exit", 5)));
    }

    private async Task<AutomationProfileExecutionOutput> WaitAndCancelAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        throw new InvalidOperationException("The cancellation wait unexpectedly completed.");
    }
}

sealed class FixtureWorkflowLibraryStore : IAutomationLibraryStore
{
    private readonly Dictionary<(string ModuleId, string Collection, string Id), AutomationLibraryRecord> _records = [];

    public Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(string moduleId, string collection, CancellationToken cancellationToken)
    {
        IReadOnlyList<AutomationLibraryRecord> records = _records.Values
            .Where(record => record.ModuleId == moduleId && record.Collection == collection)
            .OrderByDescending(record => record.UpdatedUtc).ToArray();
        return Task.FromResult(records);
    }

    public Task<AutomationLibraryRecord?> GetAsync(string moduleId, string collection, string id, CancellationToken cancellationToken)
    {
        _records.TryGetValue((moduleId, collection, id), out var record);
        return Task.FromResult(record);
    }

    public Task UpsertAsync(AutomationLibraryRecord record, CancellationToken cancellationToken)
    {
        _records[(record.ModuleId, record.Collection, record.Id)] = record with { Data = record.Data.Clone() };
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string moduleId, string collection, string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_records.Remove((moduleId, collection, id)));
    }
}

sealed class FixtureAutomationModule : IAutomationModule
{
    private readonly bool _wait;
    private readonly bool _oversizedResult;

    public FixtureAutomationModule(bool duplicateActions = false, bool unknownCapability = false, int contractVersion = 1, bool wait = false, bool oversizedResult = false, bool requireKeyboard = false)
    {
        _wait = wait;
        _oversizedResult = oversizedResult;
        var keyboardGrant = new AutomationCapabilityRequirement(AutomationCapabilityIds.KeyboardInput, 1);
        var actions = new List<AutomationModuleActionDescriptor>
        {
            new("summarize", 1, requireKeyboard ? [keyboardGrant] : []),
            new("wait", 1, [])
        };
        if (duplicateActions) actions.Add(new("summarize", 1, []));
        Definition = new AutomationModuleDefinition(
            2,
            "fixture-module",
            "Fixture module",
            "grid",
            "fixture",
            false,
            contractVersion,
            2,
            unknownCapability ? [new("filesystem.raw", 1)] : requireKeyboard ? [keyboardGrant] : [],
            actions);
    }

    public AutomationModuleDefinition Definition { get; }
    public int ContractVersion => Definition.ContractVersion;
    public string Id => Definition.Id;
    public string Title => Definition.Title;

    public System.Text.Json.JsonElement CreateDefaultSettings()
    {
        using var document = System.Text.Json.JsonDocument.Parse("{}");
        return document.RootElement.Clone();
    }

    public System.Text.Json.JsonElement MigrateSettings(int fromVersion, System.Text.Json.JsonElement value)
    {
        if (fromVersion != 1 || Definition.SettingsVersion != 2)
            throw new InvalidOperationException("No migration path.");
        using var document = System.Text.Json.JsonDocument.Parse("{\"migrated\":true}");
        return document.RootElement.Clone();
    }

    public async ValueTask<Automator.Core.Plugins.AutomationResult> ExecuteAsync(
        string actionId,
        System.Text.Json.JsonElement input,
        System.Text.Json.JsonElement moduleSettings,
        AutomationServicesContext services,
        CancellationToken cancellationToken)
    {
        if (actionId == "wait" || _wait)
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        var count = input.TryGetProperty("count", out var countValue) ? countValue.GetInt32() : 0;
        using var data = System.Text.Json.JsonDocument.Parse(_oversizedResult
            ? "{\"value\":\"" + new string('x', 512 * 1024 + 1) + "\"}"
            : $"{{\"count\":{count},\"settings\":{moduleSettings.GetRawText()}}}");
        using var payload = System.Text.Json.JsonDocument.Parse($"{{\"reportId\":\"report-{count}\"}}");
        return new Automator.Core.Plugins.AutomationResult(
            1,
            Automator.Core.Plugins.AutomationStatus.Success,
            "Done",
            data.RootElement.Clone(),
            [new Automator.Core.Plugins.AutomationAction("open", "Open report", 1, payload.RootElement.Clone())]);
    }
}
