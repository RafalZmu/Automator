using System.Runtime.InteropServices;

namespace Automator.Services;

public sealed class TrayIconService : IDisposable
{
    private const uint CallbackMessage = NativeMethods.WM_USER + 0x241;
    internal const uint ActivateMessage = NativeMethods.WM_USER + 0x242;
    private const uint MenuShow = 1001;
    private const uint MenuSettings = 1002;
    private const uint MenuExit = 1003;
    private readonly IntPtr _window;
    private readonly Action _showLauncher;
    private readonly Action _activateLauncher;
    private readonly Action _showSettings;
    private readonly Action _exit;
    private readonly NativeMethods.SubclassProc _subclassProc;
    private bool _disposed;
    private bool _subclassAttached;

    public bool IsRegistered { get; }

    public TrayIconService(IntPtr window, Action showLauncher, Action activateLauncher, Action showSettings, Action exit)
    {
        _window = window;
        _showLauncher = showLauncher;
        _activateLauncher = activateLauncher;
        _showSettings = showSettings;
        _exit = exit;
        _subclassProc = WindowProc;
        if (!NativeMethods.SetWindowSubclass(_window, _subclassProc, 1, 0))
        {
            AppLogger.Error("Tray.SubclassInstallFailed", "Could not attach the tray and activation message handler.",
                properties: new Dictionary<string, object?> { ["win32Error"] = Marshal.GetLastWin32Error() });
            throw new InvalidOperationException("Could not attach the notification-area message handler.");
        }
        _subclassAttached = true;

        var data = new NativeMethods.NotifyIconData
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.NotifyIconData>(),
            Window = _window,
            Id = 1,
            Flags = NativeMethods.NIF_MESSAGE | NativeMethods.NIF_ICON | NativeMethods.NIF_TIP,
            CallbackMessage = CallbackMessage,
            Icon = NativeMethods.LoadIcon(IntPtr.Zero, new IntPtr(NativeMethods.IDI_APPLICATION)),
            Tip = "Automator",
            Info = string.Empty,
            InfoTitle = string.Empty
        };
        if (!NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_ADD, ref data))
        {
            AppLogger.Warning("Tray.RegisterFailed", "Shell_NotifyIcon could not register the notification-area icon.",
                properties: new Dictionary<string, object?> { ["win32Error"] = Marshal.GetLastWin32Error() });
            IsRegistered = false;
            return;
        }

        IsRegistered = true;
    }

    private IntPtr WindowProc(IntPtr window, uint message, nuint wParam, nint lParam, nuint subclassId, nuint referenceData)
    {
        if (message == ActivateMessage)
        {
            AppLogger.Info("Tray.ActivateMessage", "Received an activation request from another Automator process.");
            InvokeSafely(_activateLauncher, "Tray.ActivationHandlerFailed");
            return IntPtr.Zero;
        }

        if (message == CallbackMessage)
        {
            var mouseEvent = unchecked((uint)lParam.ToInt64());
            if (mouseEvent == NativeMethods.WM_LBUTTONUP)
            {
                AppLogger.Info("Tray.LeftClicked", "The notification-area icon was left-clicked.");
                InvokeSafely(_showLauncher, "Tray.LauncherHandlerFailed");
                return IntPtr.Zero;
            }

            if (mouseEvent == NativeMethods.WM_RBUTTONUP)
            {
                AppLogger.Info("Tray.ContextMenuRequested", "The notification-area context menu was requested.");
                ShowMenu();
                return IntPtr.Zero;
            }
        }

        if (message == NativeMethods.WM_COMMAND)
        {
            switch ((uint)(wParam & 0xFFFF))
            {
                case MenuShow: InvokeSafely(_showLauncher, "Tray.LauncherHandlerFailed"); return IntPtr.Zero;
                case MenuSettings: InvokeSafely(_showSettings, "Tray.SettingsHandlerFailed"); return IntPtr.Zero;
                case MenuExit: InvokeSafely(_exit, "Tray.ExitHandlerFailed"); return IntPtr.Zero;
            }
        }

        return NativeMethods.DefSubclassProc(window, message, wParam, lParam);
    }

    private static void InvokeSafely(Action action, string eventName)
    {
        try { action(); }
        catch (Exception exception) { AppLogger.Error(eventName, "The notification-area action failed.", exception); }
    }

    private void ShowMenu()
    {
        var menu = NativeMethods.CreatePopupMenu();
        if (menu == IntPtr.Zero) return;
        try
        {
            NativeMethods.AppendMenu(menu, NativeMethods.MF_STRING, MenuShow, "Open Automator");
            NativeMethods.AppendMenu(menu, NativeMethods.MF_STRING, MenuSettings, "Settings");
            NativeMethods.AppendMenu(menu, NativeMethods.MF_STRING, MenuExit, "Exit");
            NativeMethods.GetCursorPos(out var point);
            NativeMethods.SetForegroundWindow(_window);
            var command = NativeMethods.TrackPopupMenu(menu, 0x0100 | NativeMethods.TPM_RIGHTBUTTON, point.X, point.Y, 0, _window, IntPtr.Zero);
            if (command != 0) NativeMethods.PostMessage(_window, NativeMethods.WM_COMMAND, command, 0);
        }
        finally
        {
            NativeMethods.DestroyMenu(menu);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        AppLogger.Info("Tray.Disposing", "Removing the notification-area icon and message handler.");
        if (IsRegistered)
        {
            var data = new NativeMethods.NotifyIconData
            {
                Size = (uint)Marshal.SizeOf<NativeMethods.NotifyIconData>(),
                Window = _window,
                Id = 1,
                Tip = string.Empty,
                Info = string.Empty,
                InfoTitle = string.Empty
            };
            NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref data);
        }

        if (_subclassAttached)
        {
            NativeMethods.RemoveWindowSubclass(_window, _subclassProc, 1);
            _subclassAttached = false;
        }
    }
}
