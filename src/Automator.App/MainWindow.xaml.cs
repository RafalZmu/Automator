using System.Collections.ObjectModel;
using System.Diagnostics;
using Automator.Plugins;
using Automator.Services;
using Automator.Core.Configuration;
using Automator.Core.Launcher;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using WinRT;
using WinRT.Interop;

namespace Automator;

public sealed partial class MainWindow : Window
{
    private const int PanelWidth = 520;
    private const int PanelHeight = 550;
    private readonly SettingsStore _settingsStore;
    private readonly KeyboardHookService _keyboardHook;
    private readonly AppCatalogService _appCatalog = new();
    private readonly WindowsLauncherService _launcher = new();
    private readonly AutomationTabRegistry _tabs = new();
    private readonly List<AppBinding> _allCatalogApps = [];
    private LauncherSettings _settings;
    private LauncherTabModule? _launcherTab;
    private RoundedAcrylicBackdrop? _cardBackdrop;
    private AppBinding? _catalogSelection;
    private AppBinding? _capturedApp;
    private volatile bool _isPanelVisible;
    private bool _panelWasActivated;
    private bool _isCatalogMode;
    private bool _isAliasMode;
    private bool _usesSolidBackdropFallback;
    private int _selectedTab = 1;
    private int _queryGeneration;
    private Button? _recordKeyButton;

    public ObservableCollection<AppBinding> BoundApps { get; } = [];
    public ObservableCollection<AppBinding> CatalogApps { get; } = [];
    public IntPtr Handle { get; }
    public event EventHandler? ExitRequested;

    public MainWindow(LauncherSettings settings, SettingsStore settingsStore, KeyboardHookService keyboardHook)
    {
        AppLogger.Info("Window.Creating", "Creating the launcher window.");
        InitializeComponent();
        // Keep the outer panel's rounded backdrop free of a bright rim. The XAML
        // surface remains unchanged so its established runtime resources still load.
        PanelSurface.BorderThickness = new Thickness(0);
        PanelSurface.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        _settings = settings;
        _settingsStore = settingsStore;
        _keyboardHook = keyboardHook;
        Handle = WindowNative.GetWindowHandle(this);
        foreach (var binding in settings.Bindings) BoundApps.Add(binding);

        _launcherTab = new LauncherTabModule(LauncherTabSurface, _launcher);
        _tabs.Register(1, _launcherTab);
        _launcherTab = _tabs.Get(1) as LauncherTabModule;
        BuildTabButtons();
        ApplyTheme(settings.Theme);
        ConfigureNativeWindow();
        Activated += OnWindowActivated;
        _keyboardHook.ToggleRequested += OnToggleRequested;
        _keyboardHook.KeyPressed += OnGlobalKeyPressed;
        _keyboardHook.KeyRecorded += OnKeyRecorded;
        AppLogger.Info("Window.Created", "The launcher window and keyboard event handlers are ready.",
            new Dictionary<string, object?> { ["windowHandle"] = $"0x{Handle.ToInt64():X}" });
    }

    private void ConfigureNativeWindow()
    {
        var style = NativeMethods.GetWindowLongPtr(Handle, NativeMethods.GWL_STYLE).ToInt64();
        style = (style & ~NativeMethods.WS_CAPTION & ~NativeMethods.WS_THICKFRAME) | NativeMethods.WS_POPUP;
        NativeMethods.SetWindowLongPtr(Handle, NativeMethods.GWL_STYLE, new IntPtr(style));

        var extended = NativeMethods.GetWindowLongPtr(Handle, NativeMethods.GWL_EXSTYLE).ToInt64();
        extended |= NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_TOPMOST;
        NativeMethods.SetWindowLongPtr(Handle, NativeMethods.GWL_EXSTYLE, new IntPtr(extended));
        NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, 0, 0, PanelWidth, PanelHeight,
            NativeMethods.SWP_FRAMECHANGED | NativeMethods.SWP_NOACTIVATE);
        ConfigureNativeCorners();

        ConfigureAcrylicBackdrop();
    }

    private void ConfigureNativeCorners()
    {
        // The XAML card clips its content, but the borderless WS_POPUP HWND also needs
        // a rounded region. DWM's corner preference is only a hint and does not clip
        // the HWND's rectangular client surface on this custom window.
        var preference = NativeMethods.DWMWCP_ROUND;
        var hresult = NativeMethods.DwmSetWindowAttribute(
            Handle,
            NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE,
            ref preference,
            sizeof(uint));

        var borderHresult = int.MinValue;
        if (hresult == 0)
        {
            var borderColor = NativeMethods.DWMWA_COLOR_NONE;
            borderHresult = NativeMethods.DwmSetWindowAttribute(
                Handle,
                NativeMethods.DWMWA_BORDER_COLOR,
                ref borderColor,
                sizeof(uint));
        }

        // Always clip the native window itself, including when DWM accepts its hint.
        // This removes the rectangular backing that otherwise shows outside the XAML card.
        if (!NativeMethods.GetWindowRect(Handle, out var bounds))
        {
            AppLogger.Warning("Window.NativeCornersUnavailable", "Could not read the launcher window bounds for rounded clipping.",
                properties: new Dictionary<string, object?>
                {
                    ["method"] = "WindowRegion",
                    ["dwmHresult"] = $"0x{hresult:X8}",
                    ["borderHresult"] = hresult == 0 ? $"0x{borderHresult:X8}" : null,
                    ["win32Error"] = System.Runtime.InteropServices.Marshal.GetLastWin32Error()
                });
            return;
        }

        var width = bounds.Right - bounds.Left;
        var height = bounds.Bottom - bounds.Top;
        var dpi = NativeMethods.GetDpiForWindow(Handle);
        if (dpi == 0) dpi = 96;
        var cornerDiameter = Math.Max(1, (int)Math.Round(32d * dpi / 96d));
        var region = NativeMethods.CreateRoundRectRgn(0, 0, width, height, cornerDiameter, cornerDiameter);
        if (region == IntPtr.Zero)
        {
            AppLogger.Warning("Window.NativeCornersUnavailable", "Could not create a rounded window region.",
                properties: new Dictionary<string, object?>
                {
                    ["method"] = "WindowRegion",
                    ["dwmHresult"] = $"0x{hresult:X8}",
                    ["borderHresult"] = hresult == 0 ? $"0x{borderHresult:X8}" : null,
                    ["win32Error"] = System.Runtime.InteropServices.Marshal.GetLastWin32Error(),
                    ["width"] = width,
                    ["height"] = height,
                    ["dpi"] = dpi
                });
            return;
        }

        if (NativeMethods.SetWindowRgn(Handle, region, redraw: true))
        {
            // SetWindowRgn transfers ownership of the region to Windows on success.
            AppLogger.Info("Window.NativeCornersConfigured", "Applied a rounded region to the native window.",
                new Dictionary<string, object?>
                {
                    ["method"] = hresult == 0 ? "DWM+WindowRegion" : "WindowRegion",
                    ["dwmHresult"] = $"0x{hresult:X8}",
                    ["dwmPreferenceApplied"] = hresult == 0,
                    ["nativeBorderSuppressed"] = hresult == 0 && borderHresult == 0,
                    ["borderHresult"] = hresult == 0 ? $"0x{borderHresult:X8}" : null,
                    ["width"] = width,
                    ["height"] = height,
                    ["dpi"] = dpi,
                    ["cornerDiameter"] = cornerDiameter
                });
            return;
        }

        var setRegionError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
        NativeMethods.DeleteObject(region);
        AppLogger.Warning("Window.NativeCornersUnavailable", "Could not apply a rounded native window region.",
            properties: new Dictionary<string, object?>
            {
                ["method"] = "WindowRegion",
                ["dwmHresult"] = $"0x{hresult:X8}",
                ["borderHresult"] = hresult == 0 ? $"0x{borderHresult:X8}" : null,
                ["win32Error"] = setRegionError,
                ["width"] = width,
                ["height"] = height,
                ["dpi"] = dpi
            });
    }

    private void ConfigureAcrylicBackdrop()
    {
        if (!DesktopAcrylicController.IsSupported())
        {
            UseSolidBackdropFallback();
            AppLogger.Warning("Window.BackdropUnsupported", "Desktop Acrylic is unavailable; using the solid panel fallback.",
                properties: new Dictionary<string, object?> { ["osVersion"] = Environment.OSVersion.VersionString });
            return;
        }

        try
        {
            PanelSurface.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(RoundedAcrylicBackdrop.GetFallbackColor(_settings.Theme));
            _cardBackdrop = new RoundedAcrylicBackdrop(_settings.Theme);
            _cardBackdrop.AttachmentChanged += OnBackdropAttachmentChanged;
            BackdropElement.SystemBackdrop = _cardBackdrop;
            AppLogger.Info("Window.BackdropConfigured", "Configured Desktop Acrylic for the rounded panel card.",
                new Dictionary<string, object?>
                {
                    ["supported"] = true,
                    ["theme"] = _settings.Theme.ToString(),
                    ["target"] = "SystemBackdropElement",
                    ["state"] = _cardBackdrop.State
                });
        }
        catch (Exception exception)
        {
            _cardBackdrop?.Dispose();
            _cardBackdrop = null;
            UseSolidBackdropFallback();
            AppLogger.Error("Window.BackdropSetupFailed", "Could not initialize Desktop Acrylic; using the solid panel fallback.", exception,
                new Dictionary<string, object?> { ["osVersion"] = Environment.OSVersion.VersionString });
        }
    }

    private void UpdateAcrylicTheme()
    {
        _cardBackdrop?.UpdateTheme(_settings.Theme);
        if (_cardBackdrop is not null) return;
        else if (_usesSolidBackdropFallback)
        {
            PanelSurface.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(RoundedAcrylicBackdrop.GetFallbackColor(_settings.Theme));
        }
    }

    private void UseSolidBackdropFallback()
    {
        _usesSolidBackdropFallback = true;
        PanelSurface.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(RoundedAcrylicBackdrop.GetFallbackColor(_settings.Theme));
    }

    private void OnBackdropAttachmentChanged(object? sender, bool isAttached)
    {
        PanelSurface.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(isAttached
            ? Color.FromArgb(0x00, 0x00, 0x00, 0x00)
            : RoundedAcrylicBackdrop.GetFallbackColor(_settings.Theme));
    }

    private void BuildTabButtons()
    {
        for (var slot = 1; slot <= 9; slot++)
        {
            var currentSlot = slot;
            var button = new Button
            {
                Content = slot.ToString(),
                Tag = slot,
                MinWidth = 38,
                Height = 36,
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(10),
            };
            ToolTipService.SetToolTip(button, slot == 1 ? "App launcher" : $"Automation slot {slot}");
            button.Click += (_, _) => SelectTab(currentSlot);
            TabStrip.Children.Add(button);
        }
        SelectTab(1);
    }

    private void SelectTab(int slot)
    {
        if (slot is < 1 or > 9) return;
        _selectedTab = slot;
        _isCatalogMode = false;
        _isAliasMode = false;
        SetLauncherMode();
        QueryBox.Text = string.Empty;
        ReservedTabPanel.Visibility = slot == 1 ? Visibility.Collapsed : Visibility.Visible;
        ReservedTabTitle.Text = $"Automation slot {slot}";
        LauncherTabSurface.Visibility = slot == 1 ? Visibility.Visible : Visibility.Collapsed;
        UpdateTabButtonStyles();
    }

    private void UpdateTabButtonStyles()
    {
        var isDark = _settings.Theme == AppTheme.Dark;
        var idleBackground = isDark
            ? Color.FromArgb(0x76, 0x3E, 0x4C, 0x56)
            : Color.FromArgb(0x52, 0xFF, 0xFF, 0xFF);
        var idleForeground = isDark
            ? Color.FromArgb(0xFF, 0xF3, 0xF7, 0xF8)
            : Color.FromArgb(0xFF, 0x24, 0x38, 0x40);
        var borderColor = isDark
            ? Color.FromArgb(0x72, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x8C, 0xFF, 0xFF, 0xFF);

        foreach (var child in TabStrip.Children.OfType<Button>())
        {
            var active = (int)child.Tag == _selectedTab;
            child.FontWeight = active ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
            child.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(active
                ? Color.FromArgb(0xEE, 0x62, 0xC6, 0xDB)
                : idleBackground);
            child.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(borderColor);
            child.BorderThickness = new Thickness(1);
            child.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(active || !isDark
                ? Color.FromArgb(0xFF, 0x14, 0x2A, 0x31)
                : idleForeground);
        }
    }

    public void TogglePanel()
    {
        if (_isPanelVisible)
        {
            HidePanel("ToggleHotkey");
            return;
        }

        // Capture before the panel covers the window beneath the pointer.
        _capturedApp = _appCatalog.CaptureUnderCursor();
        SelectTab(1);
        _isPanelVisible = true;
        _panelWasActivated = false;
        _keyboardHook.NavigationKeysEnabled = true;
        AppLogger.Info("Panel.OpenRequested", "Opening the launcher panel.", new Dictionary<string, object?>
        {
            ["capturedApp"] = _capturedApp?.Name,
            ["selectedTab"] = _selectedTab
        });
        ActivatePanelWindow("TogglePanel");
    }

    public void ActivateLauncher()
    {
        if (!_isPanelVisible)
        {
            TogglePanel();
            return;
        }

        AppLogger.Info("Panel.Reactivated", "A second launch requested the visible launcher window.");
        ActivatePanelWindow("SecondInstance");
    }

    public void HidePanel(string reason = "Unspecified")
    {
        if (_isPanelVisible)
        {
            AppLogger.Info("Panel.HideRequested", "Hiding the launcher panel.", new Dictionary<string, object?>
            {
                ["reason"] = reason,
                ["wasActivated"] = _panelWasActivated
            });
        }
        _isPanelVisible = false;
        _panelWasActivated = false;
        _keyboardHook.NavigationKeysEnabled = false;
        NativeMethods.ShowWindow(Handle, NativeMethods.SW_HIDE);
    }

    public void ShowFallbackMode()
    {
        AppLogger.Warning("Window.FallbackMode", "The notification area is unavailable, so the launcher is being shown directly.");
        ExitButton.Visibility = Visibility.Visible;
        StatusText.Text = "Notification area unavailable. Automator is open here.";
        TogglePanel();
    }

    public void ShowHookFailure(int errorCode)
    {
        AppLogger.Error("Keyboard.HookUnavailable", "The configured global opener could not be installed.", properties:
            new Dictionary<string, object?> { ["win32Error"] = errorCode });
        StatusText.Text = errorCode == 0
            ? "The keyboard opener could not be installed. Check Windows keyboard-hook permissions."
            : $"The keyboard opener could not be installed (Windows error {errorCode}).";
        if (!_isPanelVisible) TogglePanel();
    }

    private void OnExitClicked(object sender, RoutedEventArgs args) => ExitRequested?.Invoke(this, EventArgs.Empty);

    private void PositionNearPointer()
    {
        NativeMethods.GetCursorPos(out var pointer);
        var monitor = NativeMethods.MonitorFromPoint(pointer, 2);
        var monitorInfo = new NativeMethods.MonitorInfo { Size = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        var work = NativeMethods.GetMonitorInfo(monitor, ref monitorInfo)
            ? monitorInfo.Work
            : new NativeMethods.Rect { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };
        var x = Math.Clamp(pointer.X - PanelWidth / 2, work.Left, Math.Max(work.Left, work.Right - PanelWidth));
        var y = Math.Clamp(pointer.Y - 38, work.Top, Math.Max(work.Top, work.Bottom - PanelHeight));
        var positioned = NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, x, y, PanelWidth, PanelHeight,
            NativeMethods.SWP_FRAMECHANGED);
        if (!positioned)
        {
            AppLogger.Warning("Focus.PositionFailed", "Windows could not position the launcher panel.",
                properties: new Dictionary<string, object?>
                {
                    ["win32Error"] = System.Runtime.InteropServices.Marshal.GetLastWin32Error(),
                    ["x"] = x,
                    ["y"] = y,
                    ["width"] = PanelWidth,
                    ["height"] = PanelHeight
                });
            return;
        }

        AppLogger.Info("Panel.Positioned", "Positioned the launcher near the pointer.",
            new Dictionary<string, object?>
            {
                ["monitor"] = $"0x{monitor.ToInt64():X}",
                ["x"] = x,
                ["y"] = y,
                ["width"] = PanelWidth,
                ["height"] = PanelHeight,
                ["workLeft"] = work.Left,
                ["workTop"] = work.Top,
                ["workRight"] = work.Right,
                ["workBottom"] = work.Bottom
            });
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        var isDeactivated = args.WindowActivationState == WindowActivationState.Deactivated;
        _cardBackdrop?.SetInputActive(!isDeactivated);
        AppLogger.Info("Focus.WindowActivationChanged", "WinUI reported a launcher window activation change.",
            new Dictionary<string, object?>
            {
                ["activationState"] = args.WindowActivationState.ToString(),
                ["panelVisible"] = _isPanelVisible,
                ["foregroundWindow"] = $"0x{NativeMethods.GetForegroundWindow().ToInt64():X}"
            });

        if (isDeactivated)
        {
            if (_isPanelVisible && _panelWasActivated) HidePanel("WindowDeactivated");
            return;
        }

        _panelWasActivated = true;
        if (_isPanelVisible)
        {
            var focused = FocusCurrentInput();
            if (focused)
                AppLogger.Info("Focus.XamlInputFocused", "Applied XAML focus after window activation.");
            else
                AppLogger.Warning("Focus.XamlInputFocusFailed", "WinUI did not accept focus for the active launcher input.");
        }
    }

    private void OnToggleRequested(object? sender, EventArgs args)
    {
        AppLogger.Info("Hotkey.ToggleRequested", "The configured opener key was tapped.");
        var queued = DispatcherQueue.TryEnqueue(TogglePanel);
        AppLogger.Info("Hotkey.ToggleQueued", "Queued the panel toggle on the WinUI thread.",
            new Dictionary<string, object?> { ["queued"] = queued });
        if (!queued)
            AppLogger.Warning("Hotkey.DispatchUnavailable", "The panel toggle could not be queued on the WinUI thread.");
    }

    private bool FocusCurrentInput() => _isAliasMode
        ? AliasBox.Focus(FocusState.Programmatic)
        : QueryBox.Focus(FocusState.Programmatic);

    private void ActivatePanelWindow(string reason)
    {
        var foregroundBefore = NativeMethods.GetForegroundWindow();
        PositionNearPointer();
        AppLogger.Info("Focus.WinUiActivate", "Calling WinUI Window.Activate before the native focus handoff.",
            new Dictionary<string, object?> { ["reason"] = reason });
        Activate();
        NativeMethods.ShowWindow(Handle, NativeMethods.SW_SHOW);

        var foregroundAtAttempt = NativeMethods.GetForegroundWindow();
        var foregroundThread = foregroundAtAttempt == IntPtr.Zero
            ? 0
            : NativeMethods.GetWindowThreadProcessId(foregroundAtAttempt, out _);
        var uiThread = NativeMethods.GetCurrentThreadId();
        var attachedInput = false;
        var attachError = 0;
        var raised = false;
        var foregroundSet = false;
        var activeWindowBefore = IntPtr.Zero;
        var previousInputFocus = IntPtr.Zero;
        var currentInputFocus = IntPtr.Zero;
        var xamlFocus = false;
        var foregroundAfter = IntPtr.Zero;
        var properties = new Dictionary<string, object?>
        {
            ["reason"] = reason,
            ["windowHandle"] = $"0x{Handle.ToInt64():X}",
            ["foregroundBefore"] = $"0x{foregroundBefore.ToInt64():X}",
            ["foregroundAtAttempt"] = $"0x{foregroundAtAttempt.ToInt64():X}",
            ["foregroundBeforeThread"] = foregroundThread,
            ["uiThread"] = uiThread
        };

        if (foregroundThread != 0 && foregroundThread != uiThread)
        {
            attachedInput = NativeMethods.AttachThreadInput(uiThread, foregroundThread, true);
            if (!attachedInput) attachError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
        }
        properties["inputQueuesAttached"] = attachedInput;
        properties["attachError"] = attachError;

        try
        {
            raised = NativeMethods.BringWindowToTop(Handle);
            foregroundSet = NativeMethods.SetForegroundWindow(Handle);
            activeWindowBefore = NativeMethods.SetActiveWindow(Handle);
            previousInputFocus = NativeMethods.SetFocus(Handle);
            xamlFocus = FocusCurrentInput();
            currentInputFocus = NativeMethods.GetFocus();
            foregroundAfter = NativeMethods.GetForegroundWindow();
            if (foregroundAfter != Handle)
            {
                foregroundSet = NativeMethods.SetForegroundWindow(Handle) || foregroundSet;
                foregroundAfter = NativeMethods.GetForegroundWindow();
            }
        }
        catch (Exception exception)
        {
            AppLogger.Error("Focus.ActivationException", "An exception occurred while activating the launcher window.", exception,
                properties);
        }
        finally
        {
            if (attachedInput && !NativeMethods.AttachThreadInput(uiThread, foregroundThread, false))
            {
                AppLogger.Warning("Focus.InputQueueDetachFailed", "Could not detach the temporarily joined input queues.",
                    properties: new Dictionary<string, object?>
                    {
                        ["win32Error"] = System.Runtime.InteropServices.Marshal.GetLastWin32Error(),
                        ["foregroundThread"] = foregroundThread,
                        ["uiThread"] = uiThread
                    });
            }
        }

        var isForeground = foregroundAfter == Handle;
        properties["inputQueuesAttached"] = attachedInput;
        properties["attachError"] = attachError;
        properties["bringToTopSucceeded"] = raised;
        properties["setForegroundSucceeded"] = foregroundSet;
        properties["previousActiveWindow"] = $"0x{activeWindowBefore.ToInt64():X}";
        properties["previousInputFocus"] = $"0x{previousInputFocus.ToInt64():X}";
        properties["currentInputFocus"] = $"0x{currentInputFocus.ToInt64():X}";
        properties["foregroundAfter"] = $"0x{foregroundAfter.ToInt64():X}";
        properties["isForeground"] = isForeground;
        properties["xamlFocusSucceeded"] = xamlFocus;

        if (isForeground)
            AppLogger.Info("Focus.ActivationSucceeded", "The launcher window is the foreground window.", properties);
        else
            AppLogger.Warning("Focus.ActivationNotConfirmed", "Windows did not make the launcher window foreground; see the focus diagnostics.",
                properties: properties);
    }

    private void OnGlobalKeyPressed(object? sender, GlobalKeyEventArgs args)
    {
        var queued = DispatcherQueue.TryEnqueue(() =>
        {
            if (!_isPanelVisible) return;
            if (args.Key == "Escape")
            {
                HidePanel("EscapeKey");
                return;
            }

            if (args.Key.Length == 1 && TabKeyParser.Parse(args.Key[0]) is int tab)
            {
                SelectTab(tab + 1);
                return;
            }

            if (_selectedTab == 1 && args.Key == "/") OpenCatalogAsync();
        });
        if (!queued)
            AppLogger.Warning("Keyboard.NavigationDispatchUnavailable", "Could not dispatch a navigation key to the WinUI thread.",
                properties: new Dictionary<string, object?> { ["key"] = args.Key });
    }

    private void OnKeyRecorded(object? sender, string key) => DispatcherQueue.TryEnqueue(() =>
    {
        _settings = _settings with { Hotkey = key };
        _keyboardHook.SetOpener(key);
        AppLogger.Info("Keyboard.OpenerChanged", "Updated the configured opener key.", new Dictionary<string, object?> { ["key"] = key });
        SaveSettings();
        if (_recordKeyButton is not null) _recordKeyButton.Content = $"Recorded: {key}";
        StatusText.Text = $"Opener key set to {key}.";
    });

    private void OnRootKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (args.Key == VirtualKey.Escape)
        {
            HidePanel("RootEscapeKey");
            args.Handled = true;
        }
    }

    private void OnQueryKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (args.Key == VirtualKey.Escape)
        {
            HidePanel("QueryEscapeKey");
            args.Handled = true;
        }
        else if (args.Key == VirtualKey.Enter)
        {
            if (_isCatalogMode && CatalogApps.Count > 0)
                SelectCatalogApp(CatalogApps[0]);
            else if (_isAliasMode)
                SaveAlias();
            args.Handled = true;
        }
    }

    private void OnQueryTextChanged(object sender, TextChangedEventArgs args)
    {
        if (_isAliasMode) return;
        if (_isCatalogMode)
        {
            FilterCatalog(QueryBox.Text);
            return;
        }

        var match = AliasMatcher.Resolve(QueryBox.Text, BoundApps);
        if (match.Kind is not (AliasMatchKind.Exact or AliasMatchKind.AmbiguousExact)) return;
        var exact = BoundApps.FirstOrDefault(app => string.Equals(app.Alias, QueryBox.Text.Trim(), StringComparison.OrdinalIgnoreCase));
        if (exact is null) return;
        var generation = ++_queryGeneration;
        _ = LaunchAfterTypingDelayAsync(exact, generation);
    }

    private async Task LaunchAfterTypingDelayAsync(AppBinding binding, int generation)
    {
        await Task.Delay(420);
        if (generation != _queryGeneration || !_isPanelVisible || _isCatalogMode || _isAliasMode
            || !string.Equals(QueryBox.Text.Trim(), binding.Alias, StringComparison.OrdinalIgnoreCase)) return;
        await LaunchBindingAsync(binding);
    }

    private async Task LaunchBindingAsync(AppBinding binding)
    {
        if (_launcherTab is null) return;
        AppLogger.Info("Launcher.ActionRequested", "Running the launcher action for a bound application.",
            new Dictionary<string, object?> { ["appName"] = binding.Name, ["targetPath"] = binding.TargetPath });
        try
        {
            var result = await _launcherTab.ExecuteAsync("launch", binding);
            if (result.Status == Automator.Core.Plugins.AutomationStatus.Error)
            {
                AppLogger.Error("Launcher.ActionReturnedError", result.Message,
                    properties: new Dictionary<string, object?> { ["appName"] = binding.Name, ["targetPath"] = binding.TargetPath });
                StatusText.Text = result.Message;
                return;
            }

            HidePanel("AppLaunch");
        }
        catch (Exception exception)
        {
            AppLogger.Error("Launcher.ActionUnhandled", "An unexpected error occurred while launching an application.", exception,
                new Dictionary<string, object?> { ["appName"] = binding.Name, ["targetPath"] = binding.TargetPath });
            StatusText.Text = $"Could not launch {binding.Name}: {exception.Message}";
        }
    }

    private void OnBoundAppClicked(object sender, ItemClickEventArgs args)
    {
        if (args.ClickedItem is AppBinding binding) _ = LaunchBindingAsync(binding);
    }

    private async void OpenCatalogAsync()
    {
        if (_selectedTab != 1 || _isCatalogMode) return;
        _isCatalogMode = true;
        _isAliasMode = false;
        SetLauncherMode();
        QueryBox.Text = string.Empty;
        AddCustomAppButton.Visibility = Visibility.Visible;
        CatalogApps.Clear();
        if (_capturedApp is not null) CatalogApps.Add(_capturedApp);
        try
        {
            var discovered = await _appCatalog.DiscoverAsync();
            _allCatalogApps.Clear();
            _allCatalogApps.AddRange(discovered);
            FilterCatalog(QueryBox.Text);
            AppLogger.Info("Catalog.ResultsDisplayed", "Application catalog results were loaded.",
                new Dictionary<string, object?> { ["applicationCount"] = CatalogApps.Count });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            AppLogger.Error("Catalog.DiscoveryFailed", "Could not discover installed applications.", exception);
            StatusText.Text = $"Some apps could not be discovered: {exception.Message}";
        }
        QueryBox.Focus(FocusState.Programmatic);
    }

    private void FilterCatalog(string query)
    {
        CatalogApps.Clear();
        if (_capturedApp is not null && Matches(_capturedApp, query)) CatalogApps.Add(_capturedApp);
        foreach (var app in _allCatalogApps)
        {
            if (_capturedApp?.Id == app.Id || !Matches(app, query)) continue;
            CatalogApps.Add(app);
        }
        if (CatalogApps.Count == 0) StatusText.Text = "No apps match. Use Add an app… to choose an EXE or shortcut.";
    }

    private static bool Matches(AppBinding app, string query) =>
        string.IsNullOrWhiteSpace(query)
        || app.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)
        || app.TargetPath.Contains(query, StringComparison.OrdinalIgnoreCase);

    private void OnCatalogItemClicked(object sender, ItemClickEventArgs args)
    {
        if (args.ClickedItem is AppBinding app) SelectCatalogApp(app);
    }

    private void SelectCatalogApp(AppBinding app)
    {
        _catalogSelection = app;
        _isCatalogMode = false;
        _isAliasMode = true;
        SetLauncherMode();
        AliasPrompt.Text = $"Bind {app.Name}";
        AliasBox.Text = BoundApps.FirstOrDefault(existing => existing.Id == app.Id)?.Alias ?? string.Empty;
        StatusText.Text = string.Empty;
        AliasBox.Focus(FocusState.Programmatic);
    }

    private void OnAliasTextChanged(object sender, TextChangedEventArgs args)
    {
        if (!_isAliasMode || _catalogSelection is null) return;
        var existing = BoundApps.Where(app => app.Id != _catalogSelection.Id).Select(app => app.Alias);
        var error = AliasValidator.Validate(AliasBox.Text, existing);
        StatusText.Text = error switch
        {
            AliasValidationError.Empty => "Enter an alias using letters.",
            AliasValidationError.LettersOnly => "Aliases can contain letters only.",
            AliasValidationError.Duplicate => "That alias is already in use.",
            _ => string.Empty
        };
    }

    private void OnAliasKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (args.Key == VirtualKey.Enter)
        {
            SaveAlias();
            args.Handled = true;
        }
        else if (args.Key == VirtualKey.Escape)
        {
            _isAliasMode = false;
            SetLauncherMode();
            args.Handled = true;
        }
    }

    private void OnSaveAliasClicked(object sender, RoutedEventArgs args) => SaveAlias();

    private void SaveAlias()
    {
        if (_catalogSelection is null) return;
        var existing = BoundApps.Where(app => app.Id != _catalogSelection.Id).Select(app => app.Alias);
        var error = AliasValidator.Validate(AliasBox.Text, existing);
        if (error is not null)
        {
            StatusText.Text = error switch
            {
                AliasValidationError.Empty => "Enter an alias using letters.",
                AliasValidationError.LettersOnly => "Aliases can contain letters only.",
                AliasValidationError.Duplicate => "That alias is already in use.",
                _ => "Alias is invalid."
            };
            return;
        }

        var binding = _catalogSelection with { Alias = AliasBox.Text.Trim() };
        var index = FindBindingIndex(binding.Id);
        if (index >= 0) BoundApps[index] = binding;
        else BoundApps.Add(binding);
        _catalogSelection = null;
        _isAliasMode = false;
        SetLauncherMode();
        SaveSettings();
        StatusText.Text = $"{binding.Name} is bound to “{binding.Alias}”.";
        QueryBox.Focus(FocusState.Programmatic);
    }

    private int FindBindingIndex(string id)
    {
        for (var i = 0; i < BoundApps.Count; i++) if (BoundApps[i].Id == id) return i;
        return -1;
    }

    private void SetLauncherMode()
    {
        BoundAppsView.Visibility = !_isCatalogMode && !_isAliasMode ? Visibility.Visible : Visibility.Collapsed;
        CatalogView.Visibility = _isCatalogMode ? Visibility.Visible : Visibility.Collapsed;
        AliasEntryPanel.Visibility = _isAliasMode ? Visibility.Visible : Visibility.Collapsed;
        AddCustomAppButton.Visibility = _isCatalogMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void OnAddCustomAppClicked(object sender, RoutedEventArgs args)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".exe");
        picker.FileTypeFilter.Add(".lnk");
        InitializeWithWindow.Initialize(picker, Handle);
        var file = await picker.PickSingleFileAsync();
        if (file is null) return;
        try
        {
            var custom = AppCatalogService.FromCustomPath(file.Path);
            _capturedApp = null;
            _allCatalogApps.RemoveAll(app => app.Id == custom.Id);
            _allCatalogApps.Insert(0, custom);
            QueryBox.Text = string.Empty;
            FilterCatalog(string.Empty);
            SelectCatalogApp(custom);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            AppLogger.Error("Catalog.CustomAppAddFailed", "Could not add the selected application to the catalog.", exception);
            StatusText.Text = $"Could not add that app: {exception.Message}";
        }
    }

    private void OnSettingsClicked(object sender, RoutedEventArgs args) => _ = ShowSettingsDialogAsync();

    public void ShowSettings()
    {
        var queued = DispatcherQueue.TryEnqueue(async () =>
        {
            if (!_isPanelVisible) TogglePanel();
            await ShowSettingsDialogAsync();
        });
        AppLogger.Info("Settings.OpenRequested", "Requested the settings dialog.", new Dictionary<string, object?> { ["queued"] = queued });
        if (!queued) AppLogger.Warning("Settings.DispatchUnavailable", "Could not dispatch settings to the WinUI thread.");
    }

    private async Task ShowSettingsDialogAsync()
    {
        var themePicker = new ComboBox { Width = 180, SelectedIndex = _settings.Theme == AppTheme.Light ? 0 : 1 };
        themePicker.Items.Add("Light");
        themePicker.Items.Add("Dark");
        var startupToggle = new CheckBox { Content = "Start with Windows", IsChecked = _settings.StartWithWindows };
        var missingPathsButton = new Button
        {
            Content = "Relink missing app paths…",
            HorizontalAlignment = HorizontalAlignment.Left,
            Visibility = _settingsStore.FindMissingPaths(_settings).Count > 0 ? Visibility.Visible : Visibility.Collapsed
        };
        missingPathsButton.Click += async (_, _) =>
        {
            _settings = await RelinkMissingPathsAsync(_settings);
            BoundApps.Clear();
            foreach (var binding in _settings.Bindings) BoundApps.Add(binding);
            SaveSettings();
            var count = _settingsStore.FindMissingPaths(_settings).Count;
            missingPathsButton.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
            missingPathsButton.Content = count > 0 ? $"Relink missing app paths ({count})…" : "All app paths relinked";
            StatusText.Text = count > 0 ? $"{count} app path(s) still need relinking." : "All app paths were relinked.";
        };
        _recordKeyButton = new Button { Content = $"Record opener key ({_settings.Hotkey})" };
        _recordKeyButton.Click += (_, _) =>
        {
            _recordKeyButton.Content = "Press the key to use as the opener…";
            _keyboardHook.StartRecording();
        };
        var exportButton = new Button { Content = "Export settings…", HorizontalAlignment = HorizontalAlignment.Left };
        exportButton.Click += async (_, _) => await ExportSettingsAsync();
        var importButton = new Button { Content = "Import settings…", HorizontalAlignment = HorizontalAlignment.Left };
        var openLogsButton = new Button { Content = "Open log folder", HorizontalAlignment = HorizontalAlignment.Left };
        openLogsButton.Click += (_, _) => OpenLogFolder();

        var content = new StackPanel { Spacing = 12 };
        content.Children.Add(new TextBlock { Text = "Appearance", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        content.Children.Add(themePicker);
        content.Children.Add(startupToggle);
        content.Children.Add(new TextBlock { Text = "Global opener key", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        content.Children.Add(_recordKeyButton);
        content.Children.Add(exportButton);
        content.Children.Add(importButton);
        content.Children.Add(missingPathsButton);
        content.Children.Add(new TextBlock
        {
            Text = $"Logs: {AppLogger.LogDirectory}",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["MutedTextBrush"]
        });
        content.Children.Add(openLogsButton);
        importButton.Click += async (_, _) => await ImportSettingsAsync(() =>
        {
            themePicker.SelectedIndex = _settings.Theme == AppTheme.Light ? 0 : 1;
            startupToggle.IsChecked = _settings.StartWithWindows;
            var missingCount = _settingsStore.FindMissingPaths(_settings).Count;
            missingPathsButton.Visibility = missingCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            missingPathsButton.Content = $"Relink missing app paths ({missingCount})…";
            if (_recordKeyButton is not null) _recordKeyButton.Content = $"Record opener key ({_settings.Hotkey})";
        });

        var dialog = new ContentDialog
        {
            Title = "Automator settings",
            Content = content,
            PrimaryButtonText = "Save",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = RootGrid.XamlRoot
        };

        try
        {
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                _settings = _settings with
                {
                    Theme = themePicker.SelectedIndex == 1 ? AppTheme.Dark : AppTheme.Light,
                    StartWithWindows = startupToggle.IsChecked == true
                };
                ApplyTheme(_settings.Theme);
                SaveSettings();
            }
        }
        catch (InvalidOperationException exception)
        {
            AppLogger.Error("Settings.DialogFailed", "Could not show the settings dialog.", exception);
            StatusText.Text = $"Could not show settings: {exception.Message}";
        }
        finally
        {
            _recordKeyButton = null;
        }
    }

    private async Task ExportSettingsAsync()
    {
        var picker = new FileSavePicker
        {
            SuggestedFileName = "Automator-settings"
        };
        picker.FileTypeChoices.Add("JSON settings", new List<string> { ".json" });
        InitializeWithWindow.Initialize(picker, Handle);
        var file = await picker.PickSaveFileAsync();
        if (file is null) return;
        try
        {
            _settingsStore.Export(file.Path, CurrentSettings());
            StatusText.Text = "Settings exported.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            AppLogger.Error("Settings.ExportFailed", "Could not export settings from the UI.", exception);
            StatusText.Text = $"Could not export settings: {exception.Message}";
        }
    }

    private void OpenLogFolder()
    {
        try
        {
            var path = AppLogger.LogDirectory;
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("The log folder is not available.");

            Directory.CreateDirectory(path);
            var startInfo = new ProcessStartInfo { FileName = "explorer.exe", UseShellExecute = true };
            startInfo.ArgumentList.Add(path);
            Process.Start(startInfo);
            AppLogger.Info("Logging.FolderOpened", "Opened the Automator log folder.");
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException)
        {
            AppLogger.Error("Logging.FolderOpenFailed", "Could not open the Automator log folder.", exception);
            StatusText.Text = $"Could not open logs: {exception.Message}";
        }
    }

    private async Task ImportSettingsAsync(Action? updateSettingsControls = null)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".json");
        InitializeWithWindow.Initialize(picker, Handle);
        var file = await picker.PickSingleFileAsync();
        if (file is null) return;

        try
        {
            var imported = _settingsStore.Import(file.Path);
            KeyNames.ToVirtualKey(imported.Hotkey);
            _settings = imported;
            BoundApps.Clear();
            foreach (var binding in imported.Bindings) BoundApps.Add(binding);
            _keyboardHook.SetOpener(imported.Hotkey);
            ApplyTheme(imported.Theme);
            SaveSettings();
            updateSettingsControls?.Invoke();
            var missingCount = _settingsStore.FindMissingPaths(imported).Count;
            StatusText.Text = missingCount == 0
                ? "Settings imported."
                : $"Settings imported with {missingCount} missing app path(s). You can relink them in Settings.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or System.Text.Json.JsonException or ArgumentException)
        {
            AppLogger.Error("Settings.ImportFailed", "Could not import settings from the UI.", exception);
            StatusText.Text = $"Could not import settings: {exception.Message}";
        }
    }

    private async Task<LauncherSettings> RelinkMissingPathsAsync(LauncherSettings settings)
    {
        var bindings = settings.Bindings.ToList();
        foreach (var missing in _settingsStore.FindMissingPaths(settings))
        {
            var bindingIndex = bindings.FindIndex(binding => string.Equals(binding.TargetPath, missing, StringComparison.OrdinalIgnoreCase));
            if (bindingIndex < 0) continue;
            var original = bindings[bindingIndex];
            StatusText.Text = $"Choose the new location for {original.Name}. Cancel to skip this app.";
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".exe");
            picker.FileTypeFilter.Add(".lnk");
            InitializeWithWindow.Initialize(picker, Handle);
            var replacement = await picker.PickSingleFileAsync();
            if (replacement is null) continue;
            var resolved = AppCatalogService.FromCustomPath(replacement.Path);
            var indices = bindings
                .Select((binding, index) => (binding, index))
                .Where(item => string.Equals(item.binding.TargetPath, missing, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.index)
                .ToArray();
            foreach (var index in indices)
            {
                bindings[index] = bindings[index] with
                {
                    Id = resolved.Id,
                    TargetPath = resolved.TargetPath,
                    Arguments = resolved.Arguments
                };
            }
        }
        return settings with { Bindings = bindings };
    }

    private LauncherSettings CurrentSettings() => _settings with { Bindings = BoundApps.ToList() };

    private void SaveSettings()
    {
        _settings = CurrentSettings();
        try
        {
            _settingsStore.Save(_settings);
            _settingsStore.ApplyStartupSetting(_settings.StartWithWindows);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            AppLogger.Error("Settings.SaveFailed", "Could not save the current settings from the UI.", exception);
            StatusText.Text = $"Could not save settings: {exception.Message}";
        }
    }

    private void ApplyTheme(AppTheme theme)
    {
        PanelSurface.RequestedTheme = theme == AppTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
        UpdateTabButtonStyles();
        UpdateAcrylicTheme();
    }

    public void DisposeServices()
    {
        AppLogger.Info("Window.DisposingServices", "Disposing launcher services.");
        BackdropElement.SystemBackdrop = null;
        if (_cardBackdrop is not null)
        {
            _cardBackdrop.AttachmentChanged -= OnBackdropAttachmentChanged;
            _cardBackdrop.Dispose();
            _cardBackdrop = null;
        }
        _launcher.Dispose();
    }
}
