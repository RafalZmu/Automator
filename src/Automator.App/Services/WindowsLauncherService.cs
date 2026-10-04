using System.Diagnostics;
using System.Runtime.InteropServices;
using Automator.Core.Launcher;

namespace Automator.Services;

public sealed class WindowsLauncherService : IDisposable
{
    private readonly object _sync = new();
    private readonly Dictionary<IntPtr, long> _lastForeground = [];
    private readonly NativeMethods.WinEventProc _eventCallback;
    private readonly IntPtr _selfProcessId = Environment.ProcessId;
    private readonly IntPtr _foregroundHook;

    public WindowsLauncherService()
    {
        _eventCallback = OnForegroundChanged;
        _foregroundHook = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero,
            _eventCallback,
            0,
            0,
            NativeMethods.WINEVENT_OUTOFCONTEXT);
        if (_foregroundHook == IntPtr.Zero)
            AppLogger.Warning("Launcher.ForegroundHookUnavailable", "Could not monitor foreground window changes.",
                properties: new Dictionary<string, object?> { ["win32Error"] = Marshal.GetLastWin32Error() });
        else
            AppLogger.Info("Launcher.ForegroundHookInstalled", "Installed the foreground-window recency hook.");
    }

    public Task<LaunchAction> ActivateAsync(AppBinding binding, CancellationToken cancellationToken = default)
    {
        var candidates = FindWindows(binding.TargetPath);
        var foreground = NativeMethods.GetForegroundWindow();
        AppLogger.Info("Launcher.ActivationRequested", "Requested activation of a bound application.", new Dictionary<string, object?>
        {
            ["appName"] = binding.Name,
            ["targetPath"] = binding.TargetPath,
            ["matchingWindows"] = candidates.Count,
            ["foregroundWindow"] = $"0x{foreground.ToInt64():X}"
        });
        var foregroundMatch = candidates.FirstOrDefault(window => window == foreground);
        if (foregroundMatch != IntPtr.Zero)
        {
            NativeMethods.ShowWindowAsync(foregroundMatch, NativeMethods.SW_MINIMIZE);
            AppLogger.Info("Launcher.MinimizedForegroundApp", "The bound application was already foreground and was minimized.",
                new Dictionary<string, object?> { ["appName"] = binding.Name });
            return Task.FromResult(LaunchAction.Minimize);
        }

        if (candidates.Count > 0)
        {
            var window = candidates[0];
            if (NativeMethods.IsIconic(window)) NativeMethods.ShowWindowAsync(window, NativeMethods.SW_RESTORE);
            NativeMethods.ShowWindowAsync(window, NativeMethods.SW_MAXIMIZE);
            var foregroundSet = NativeMethods.SetForegroundWindow(window);
            AppLogger.Info("Launcher.RestoredApp", "Restored and maximized an existing application window.", new Dictionary<string, object?>
            {
                ["appName"] = binding.Name,
                ["windowHandle"] = $"0x{window.ToInt64():X}",
                ["setForegroundSucceeded"] = foregroundSet
            });
            return Task.FromResult(LaunchAction.RestoreAndMaximize);
        }

        var startInfo = new ProcessStartInfo(binding.TargetPath)
        {
            Arguments = binding.Arguments,
            UseShellExecute = true
        };
        var started = Process.Start(startInfo);
        if (started is not null)
        {
            AppLogger.Info("Launcher.ProcessStarted", "Started a new application process.", new Dictionary<string, object?>
            {
                ["appName"] = binding.Name,
                ["targetPath"] = binding.TargetPath,
                ["processId"] = started.Id
            });
            _ = WaitAndMaximizeAsync(binding.TargetPath, started, cancellationToken);
        }
        else
        {
            AppLogger.Warning("Launcher.ProcessStartReturnedNull", "Windows did not return a process for the requested launch.",
                properties: new Dictionary<string, object?> { ["appName"] = binding.Name, ["targetPath"] = binding.TargetPath });
        }
        return Task.FromResult(LaunchAction.StartAndMaximize);
    }

    private async Task WaitAndMaximizeAsync(string targetPath, Process started, CancellationToken cancellationToken)
    {
        using (started)
        {
            AppLogger.Info("Launcher.WaitingForWindow", "Waiting briefly for the newly launched app window.",
                new Dictionary<string, object?> { ["processId"] = started.Id, ["targetPath"] = targetPath });
            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(8);
                while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
                {
                    var window = FindWindows(targetPath).FirstOrDefault();
                    if (window != IntPtr.Zero)
                    {
                        NativeMethods.ShowWindowAsync(window, NativeMethods.SW_MAXIMIZE);
                        var foregroundSet = NativeMethods.SetForegroundWindow(window);
                        AppLogger.Info("Launcher.NewWindowReady", "Maximized the newly launched application window.",
                            new Dictionary<string, object?>
                            {
                                ["processId"] = started.Id,
                                ["windowHandle"] = $"0x{window.ToInt64():X}",
                                ["setForegroundSucceeded"] = foregroundSet
                            });
                        return;
                    }

                    started.Refresh();
                    if (started.MainWindowHandle != IntPtr.Zero)
                    {
                        NativeMethods.ShowWindowAsync(started.MainWindowHandle, NativeMethods.SW_MAXIMIZE);
                        var foregroundSet = NativeMethods.SetForegroundWindow(started.MainWindowHandle);
                        AppLogger.Info("Launcher.NewWindowReady", "Maximized the launched process main window.",
                            new Dictionary<string, object?>
                            {
                                ["processId"] = started.Id,
                                ["windowHandle"] = $"0x{started.MainWindowHandle.ToInt64():X}",
                                ["setForegroundSucceeded"] = foregroundSet
                            });
                        return;
                    }

                    await Task.Delay(175, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or OperationCanceledException)
            {
                if (exception is OperationCanceledException)
                {
                    AppLogger.Info("Launcher.WaitForWindowCancelled", "Stopped waiting for the new application's window.",
                        new Dictionary<string, object?> { ["targetPath"] = targetPath });
                    return;
                }

                AppLogger.Warning("Launcher.WaitForWindowInterrupted", "Could not wait for the new application's window.", exception,
                    new Dictionary<string, object?> { ["targetPath"] = targetPath });
            }

            AppLogger.Warning("Launcher.WindowTimeout", "No window appeared before the launch timeout.",
                properties: new Dictionary<string, object?> { ["targetPath"] = targetPath });
        }
    }

    private List<IntPtr> FindWindows(string targetPath)
    {
        var expectedPath = NormalizePath(targetPath);
        var found = new List<IntPtr>();
        NativeMethods.EnumWindows((window, _) =>
        {
            if (!NativeMethods.IsWindowVisible(window)) return true;
            NativeMethods.GetWindowThreadProcessId(window, out var processId);
            if (processId == (uint)_selfProcessId) return true;
            try
            {
                using var process = Process.GetProcessById((int)processId);
                var actualPath = process.MainModule?.FileName;
                if (actualPath is not null && string.Equals(NormalizePath(actualPath), expectedPath, StringComparison.OrdinalIgnoreCase))
                    found.Add(window);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
            {
                // Ignore processes which exited or whose executable path is protected.
            }

            return true;
        }, IntPtr.Zero);

        Dictionary<IntPtr, long> recency;
        lock (_sync) recency = new Dictionary<IntPtr, long>(_lastForeground);
        return found
            .OrderByDescending(window => recency.TryGetValue(window, out var timestamp) ? timestamp : 0)
            .ToList();
    }

    private void OnForegroundChanged(IntPtr hook, uint eventType, IntPtr window, int objectId, int childId, uint eventThread, uint eventTime)
    {
        if (window == IntPtr.Zero) return;
        lock (_sync) _lastForeground[window] = Stopwatch.GetTimestamp();
    }

    private static string NormalizePath(string path)
    {
        try { return Path.GetFullPath(path); }
        catch (ArgumentException) { return path; }
    }

    public void Dispose()
    {
        if (_foregroundHook != IntPtr.Zero && !NativeMethods.UnhookWinEvent(_foregroundHook))
            AppLogger.Warning("Launcher.ForegroundHookRemovalFailed", "Could not remove the foreground-window hook.",
                properties: new Dictionary<string, object?> { ["win32Error"] = Marshal.GetLastWin32Error() });
    }
}
