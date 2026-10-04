using Automator.Core.Configuration;
using Automator.Services;
using Microsoft.UI.Xaml;
using System.Threading;

namespace Automator;

public partial class App : Application
{
    private static Mutex? _instanceMutex;
    private MainWindow? _mainWindow;
    private KeyboardHookService? _keyboardHook;
    private TrayIconService? _trayIcon;

    public App()
    {
        AppLogger.Initialize();
        AppLogger.Info("App.Starting", "Automator process is starting.", new Dictionary<string, object?>
        {
            ["processId"] = Environment.ProcessId,
            ["executable"] = Environment.ProcessPath,
            ["osVersion"] = Environment.OSVersion.VersionString
        });
        UnhandledException += (_, args) => AppLogger.Critical("App.XamlUnhandledException",
            "An unhandled WinUI exception occurred.", args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) => AppLogger.Critical(
            "App.DomainUnhandledException", "An unhandled .NET exception occurred.",
            args.ExceptionObject as Exception,
            new Dictionary<string, object?> { ["isTerminating"] = args.IsTerminating });
        TaskScheduler.UnobservedTaskException += (_, args) => AppLogger.Error(
            "App.UnobservedTaskException", "A task completed with an unobserved exception.", args.Exception);

        _instanceMutex = new Mutex(initiallyOwned: true, name: "Local\\Automator.DesktopLauncher", out var isFirstInstance);
        if (!isFirstInstance)
        {
            AppLogger.Info("Instance.DuplicateLaunch", "A second launch requested activation of the existing instance.");
            ActivateExistingInstance();
            Environment.Exit(0);
            return;
        }

        AppLogger.Info("Instance.Primary", "This process owns the single-instance mutex.");
        InitializeComponent();
        RequestedTheme = ApplicationTheme.Light;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        AppLogger.Info("App.Launched", "Initializing application services.");
        var settingsStore = new SettingsStore();
        var settings = settingsStore.Load();
        AppLogger.Info("Settings.Loaded", "Application settings loaded.", new Dictionary<string, object?>
        {
            ["hotkey"] = settings.Hotkey,
            ["theme"] = settings.Theme.ToString(),
            ["startWithWindows"] = settings.StartWithWindows,
            ["bindingCount"] = settings.Bindings.Count
        });
        try
        {
            _keyboardHook = new KeyboardHookService(settings.Hotkey);
        }
        catch (ArgumentException)
        {
            AppLogger.Warning("Keyboard.InvalidConfiguredHotkey", "The saved opener key was invalid; Right Control has been restored.");
            settings = settings with { Hotkey = "RightControl" };
            _keyboardHook = new KeyboardHookService(settings.Hotkey);
            settingsStore.Save(settings);
        }
        _mainWindow = new MainWindow(settings, settingsStore, _keyboardHook);
        _mainWindow.Activate();
        _mainWindow.HidePanel();
        _mainWindow.ExitRequested += (_, _) => ExitApplication();

        _trayIcon = new TrayIconService(
            _mainWindow.Handle,
            showLauncher: _mainWindow.TogglePanel,
            activateLauncher: _mainWindow.ActivateLauncher,
            showSettings: _mainWindow.ShowSettings,
            exit: ExitApplication);
        if (!_trayIcon.IsRegistered)
        {
            AppLogger.Warning("Tray.RegistrationUnavailable", "The notification-area icon could not be registered; showing fallback mode.");
            _mainWindow.ShowFallbackMode();
        }
        else
        {
            AppLogger.Info("Tray.Registered", "The notification-area icon was registered.");
        }

        _keyboardHook.Start();
        if (!_keyboardHook.WaitForInstallation(TimeSpan.FromSeconds(2)))
        {
            AppLogger.Error("Keyboard.HookInstallFailed", "The global keyboard hook did not install.", properties:
                new Dictionary<string, object?> { ["win32Error"] = _keyboardHook.HookInstallErrorCode });
            _mainWindow.ShowHookFailure(_keyboardHook.HookInstallErrorCode);
        }
        else
        {
            AppLogger.Info("Keyboard.HookInstalled", "The global keyboard hook is ready.", new Dictionary<string, object?>
            {
                ["openerKey"] = settings.Hotkey
            });
        }
        try { settingsStore.ApplyStartupSetting(settings.StartWithWindows); }
        catch (Exception exception) when (exception is System.Security.SecurityException or UnauthorizedAccessException)
        {
            AppLogger.Warning("Startup.SettingApplyFailed", "Could not apply the Windows startup setting.", exception,
                new Dictionary<string, object?> { ["enabled"] = settings.StartWithWindows });
        }
        AppLogger.Info("App.Ready", "Automator startup completed.");
    }

    private void ExitApplication()
    {
        AppLogger.Info("App.Exiting", "Automator is shutting down.");
        try { _trayIcon?.Dispose(); }
        catch (Exception exception) { AppLogger.Error("App.TrayDisposeFailed", "Could not clean up the tray icon.", exception); }
        try { _keyboardHook?.Dispose(); }
        catch (Exception exception) { AppLogger.Error("App.KeyboardDisposeFailed", "Could not stop the keyboard hook cleanly.", exception); }
        try { _mainWindow?.DisposeServices(); }
        catch (Exception exception) { AppLogger.Error("App.ServiceDisposeFailed", "Could not dispose application services cleanly.", exception); }
        if (_instanceMutex is not null)
        {
            try
            {
                _instanceMutex.ReleaseMutex();
                _instanceMutex.Dispose();
                _instanceMutex = null;
            }
            catch (ApplicationException exception)
            {
                AppLogger.Error("App.MutexReleaseFailed", "Could not release the single-instance mutex.", exception);
            }
        }
        Exit();
        AppLogger.Info("App.ExitRequested", "The WinUI application exit was requested.");
    }

    private static void ActivateExistingInstance()
    {
        // The owning process creates its window shortly after taking the mutex. Retry briefly
        // so a fast second launch during startup can still activate it.
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var window = NativeMethods.FindWindow(null, "Automator");
            if (window != IntPtr.Zero)
            {
                NativeMethods.GetWindowThreadProcessId(window, out var processId);
                var allowForeground = NativeMethods.AllowSetForegroundWindow(processId);
                // The window can exist a moment before its message handler is attached.
                // Activate repeatedly; this action is idempotent, so the last message wins.
                var successfulPosts = 0;
                for (var activationAttempt = 0; activationAttempt < 4; activationAttempt++)
                {
                    if (NativeMethods.PostMessage(window, TrayIconService.ActivateMessage, 0, 0)) successfulPosts++;
                    Thread.Sleep(100);
                }
                AppLogger.Info("Instance.ActivationPosted", "Activation was sent to the existing window.", new Dictionary<string, object?>
                {
                    ["windowHandle"] = $"0x{window.ToInt64():X}",
                    ["targetProcessId"] = processId,
                    ["allowForeground"] = allowForeground,
                    ["successfulPosts"] = successfulPosts
                });
                return;
            }

            Thread.Sleep(100);
        }

        AppLogger.Warning("Instance.WindowNotFound", "Could not find the existing Automator window to activate.");
    }
}
