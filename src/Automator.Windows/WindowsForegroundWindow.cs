using Automator.Windows.Interop;
using System.Runtime.InteropServices;

namespace Automator.Windows;

public sealed record ForegroundWindowAttempt(
    bool IsForeground,
    string? ForegroundWindowHex,
    bool InputQueuesAttached,
    int AttachErrorCode,
    bool BringToTopSucceeded,
    bool SetForegroundSucceeded);

public static class WindowsForegroundWindow
{
    public static IntPtr CurrentHandle => WindowsNativeMethods.GetForegroundWindow();

    /// <summary>Attempts a bounded native foreground handoff from the backend message thread.</summary>
    public static ForegroundWindowAttempt TryBringToForeground(IntPtr window, int expectedProcessId = 0)
    {
        if (window == IntPtr.Zero || !WindowsNativeMethods.IsWindow(window))
            return new ForegroundWindowAttempt(false, CurrentHandleHex, false, 0, false, false);

        var ownerThread = WindowsNativeMethods.GetWindowThreadProcessId(window, out var ownerProcessId);
        if (ownerThread == 0 || !WindowsNativeMethods.IsWindowVisible(window)
            || expectedProcessId > 0 && ownerProcessId != (uint)expectedProcessId)
        {
            return new ForegroundWindowAttempt(false, CurrentHandleHex, false, 0, false, false);
        }

        var foreground = WindowsNativeMethods.GetForegroundWindow();
        var foregroundThread = foreground == IntPtr.Zero ? 0 : WindowsNativeMethods.GetWindowThreadProcessId(foreground, out _);
        var currentThread = WindowsNativeMethods.GetCurrentThreadId();
        _ = WindowsNativeMethods.PeekMessage(out _, IntPtr.Zero, 0, 0, WindowsNativeMethods.PmNoRemove);

        var attached = false;
        var attachError = 0;
        var raised = false;
        var foregroundSet = false;
        if (foregroundThread != 0 && foregroundThread != currentThread)
        {
            attached = WindowsNativeMethods.AttachThreadInput(currentThread, foregroundThread, true);
            if (!attached) attachError = Marshal.GetLastWin32Error();
        }

        try
        {
            raised = WindowsNativeMethods.BringWindowToTop(window);
            foregroundSet = WindowsNativeMethods.SetForegroundWindow(window);
        }
        finally
        {
            if (attached && !WindowsNativeMethods.AttachThreadInput(currentThread, foregroundThread, false))
                attachError = Marshal.GetLastWin32Error();
        }

        var actual = CurrentHandle;
        var actualHex = actual == IntPtr.Zero ? null : $"0x{unchecked((ulong)actual.ToInt64()):X}";
        return new ForegroundWindowAttempt(actual == window, actualHex, attached, attachError, raised, foregroundSet);
    }

    public static string? CurrentHandleHex
    {
        get
        {
            var handle = CurrentHandle;
            return handle == IntPtr.Zero ? null : $"0x{unchecked((ulong)handle.ToInt64()):X}";
        }
    }
}
