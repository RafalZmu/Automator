using System.Diagnostics;
using System.Runtime.InteropServices;
using Automator.Application.Launcher;
using Automator.Application.Logging;
using Automator.Core.Launcher;
using Automator.Windows.Interop;

namespace Automator.Windows;

/// <summary>Finds an app's MRU top-level window, or starts it and maximizes its first window.</summary>
public sealed class WindowsApplicationActivator : IApplicationActivator
{
    private readonly ForegroundWindowMonitor _foregroundMonitor;
    private readonly IApplicationLog _log;
    private readonly int _hostProcessId;
    private readonly string _hostExecutablePath;

    public WindowsApplicationActivator(ForegroundWindowMonitor foregroundMonitor, int hostProcessId, string hostExecutablePath, IApplicationLog? log = null)
    {
        _foregroundMonitor = foregroundMonitor;
        _hostProcessId = hostProcessId;
        _hostExecutablePath = NormalizePath(hostExecutablePath);
        _log = log ?? NullApplicationLog.Instance;
    }

    public async Task ActivateAsync(AppBinding binding, string? previousForegroundHwnd, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(binding.TargetPath)) throw new FileNotFoundException("The selected application no longer exists.", binding.TargetPath);
        var candidates = FindWindows(binding.TargetPath);
        var previous = ParseHwnd(previousForegroundHwnd);
        _log.Write(ApplicationLogLevel.Information, "Launcher.ActivationRequested", "Requested app activation using the saved pre-panel foreground context.",
            properties: new Dictionary<string, object?>
            {
                ["appName"] = binding.Name,
                ["targetPath"] = binding.TargetPath,
                ["matchingWindows"] = candidates.Count,
                ["previousForegroundWindow"] = FormatHwnd(previous)
            });

        var targetForeground = previous != IntPtr.Zero && candidates.Contains(previous) ? previous : IntPtr.Zero;
        if (targetForeground != IntPtr.Zero)
        {
            WindowsNativeMethods.ShowWindowAsync(targetForeground, WindowsNativeMethods.SwMinimize);
            _log.Write(ApplicationLogLevel.Information, "Launcher.MinimizedPreviouslyForegroundApp", "The bound application that was foreground before the launcher opened was minimized.",
                properties: new Dictionary<string, object?> { ["appName"] = binding.Name, ["windowHandle"] = FormatHwnd(targetForeground) });
            return;
        }

        if (candidates.Count > 0)
        {
            var window = candidates[0];
            await RestoreAndMaximizeAsync(window, binding.Name, cancellationToken).ConfigureAwait(false);
            return;
        }

        Process? started;
        try
        {
            started = Process.Start(new ProcessStartInfo(binding.TargetPath)
            {
                Arguments = binding.Arguments,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(binding.TargetPath) ?? Environment.CurrentDirectory
            });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or FileNotFoundException or UnauthorizedAccessException)
        {
            _log.Write(ApplicationLogLevel.Error, "Launcher.ProcessStartFailed", "Windows could not start the selected application.", exception,
                new Dictionary<string, object?> { ["targetPath"] = binding.TargetPath, ["appName"] = binding.Name });
            throw new InvalidOperationException($"Could not start {binding.Name}: {exception.Message}", exception);
        }

        if (started is null) throw new InvalidOperationException($"Windows did not return a process for {binding.Name}.");
        using (started)
        {
            _log.Write(ApplicationLogLevel.Information, "Launcher.ProcessStarted", "Started a new application process.",
                properties: new Dictionary<string, object?> { ["processId"] = started.Id, ["targetPath"] = binding.TargetPath });
            var deadline = DateTime.UtcNow.AddSeconds(8);
            while (DateTime.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var window = FindWindows(binding.TargetPath).FirstOrDefault();
                started.Refresh();
                if (window == IntPtr.Zero && started.MainWindowHandle != IntPtr.Zero && WindowsNativeMethods.IsWindowVisible(started.MainWindowHandle))
                    window = started.MainWindowHandle;
                if (window != IntPtr.Zero)
                {
                    await RestoreAndMaximizeAsync(window, binding.Name, cancellationToken).ConfigureAwait(false);
                    _log.Write(ApplicationLogLevel.Information, "Launcher.NewWindowReady", "The newly launched app window was maximized.",
                        properties: new Dictionary<string, object?> { ["processId"] = started.Id, ["windowHandle"] = FormatHwnd(window) });
                    return;
                }

                await Task.Delay(150, cancellationToken).ConfigureAwait(false);
            }

            _log.Write(ApplicationLogLevel.Warning, "Launcher.WindowTimeout", "No window appeared before the launch timeout.",
                properties: new Dictionary<string, object?> { ["targetPath"] = binding.TargetPath, ["processId"] = started.Id });
            throw new TimeoutException($"{binding.Name} started, but its window did not appear within eight seconds.");
        }
    }

    private async Task RestoreAndMaximizeAsync(IntPtr window, string name, CancellationToken cancellationToken)
    {
        if (!WindowsNativeMethods.IsWindow(window)) throw new InvalidOperationException("The application window has already closed.");
        if (WindowsNativeMethods.IsIconic(window)) WindowsNativeMethods.ShowWindowAsync(window, WindowsNativeMethods.SwRestore);
        WindowsNativeMethods.ShowWindowAsync(window, WindowsNativeMethods.SwMaximize);
        ForegroundWindowAttempt? lastAttempt = null;
        var attempts = 0;
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (DateTime.UtcNow < deadline && WindowsNativeMethods.IsWindow(window))
        {
            cancellationToken.ThrowIfCancellationRequested();
            lastAttempt = WindowsForegroundWindow.TryBringToForeground(window);
            attempts++;
            if (lastAttempt.IsForeground)
            {
                _log.Write(ApplicationLogLevel.Information, "Launcher.ForegroundVerified", "The target app HWND became foreground.",
                    properties: new Dictionary<string, object?>
                    {
                        ["appName"] = name,
                        ["windowHandle"] = FormatHwnd(window),
                        ["foregroundHwnd"] = lastAttempt.ForegroundWindowHex,
                        ["inputQueuesAttached"] = lastAttempt.InputQueuesAttached,
                        ["attachError"] = lastAttempt.AttachErrorCode,
                        ["bringToTopSucceeded"] = lastAttempt.BringToTopSucceeded,
                        ["setForegroundSucceeded"] = lastAttempt.SetForegroundSucceeded,
                        ["attempts"] = attempts
                    });
                return;
            }
            await Task.Delay(40, cancellationToken).ConfigureAwait(false);
        }

        var finalAttempt = lastAttempt ?? WindowsForegroundWindow.TryBringToForeground(window);
        _log.Write(ApplicationLogLevel.Error, "Launcher.ForegroundNotVerified", "Windows did not confirm that the target app became foreground.",
            properties: new Dictionary<string, object?>
            {
                ["appName"] = name,
                ["windowHandle"] = FormatHwnd(window),
                ["foregroundHwnd"] = finalAttempt.ForegroundWindowHex,
                ["inputQueuesAttached"] = finalAttempt.InputQueuesAttached,
                ["attachError"] = finalAttempt.AttachErrorCode,
                ["bringToTopSucceeded"] = finalAttempt.BringToTopSucceeded,
                ["setForegroundSucceeded"] = finalAttempt.SetForegroundSucceeded,
                ["attempts"] = attempts
            });
        throw new InvalidOperationException($"Windows did not bring {name} to the foreground.");
    }

    private List<IntPtr> FindWindows(string targetPath)
    {
        var expected = NormalizePath(targetPath);
        var found = new List<IntPtr>();
        WindowsNativeMethods.EnumWindows((window, _) =>
        {
            if (!WindowsNativeMethods.IsWindowVisible(window)) return true;
            WindowsNativeMethods.GetWindowThreadProcessId(window, out var processId);
            if (ShouldExcludeProcess(processId)) return true;
            try
            {
                using var process = Process.GetProcessById((int)processId);
                var actualPath = process.MainModule?.FileName;
                if (actualPath is not null && string.Equals(NormalizePath(actualPath), expected, StringComparison.OrdinalIgnoreCase)) found.Add(window);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
            {
                // The target can exit or deny process inspection between enumeration and lookup.
            }
            return true;
        }, IntPtr.Zero);

        var recency = _foregroundMonitor.SnapshotRecency();
        return found.OrderByDescending(window => recency.TryGetValue(window, out var timestamp) ? timestamp : 0).ToList();
    }

    private bool ShouldExcludeProcess(uint processId)
    {
        if (processId == 0 || processId == (uint)Environment.ProcessId || processId == (uint)_hostProcessId) return true;
        try
        {
            using var process = Process.GetProcessById((int)processId);
            var path = process.MainModule?.FileName;
            return path is not null && string.Equals(NormalizePath(path), _hostExecutablePath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
        {
            return true;
        }
    }

    private static IntPtr ParseHwnd(string? value)
    {
        if (value is null || value.Length is < 3 or > 18 || !value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            || !ulong.TryParse(value.AsSpan(2), System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out var handle)) return IntPtr.Zero;
        return new IntPtr(unchecked((long)handle));
    }

    private static string FormatHwnd(IntPtr hwnd) => $"0x{unchecked((ulong)hwnd.ToInt64()):X}";

    private static string NormalizePath(string path)
    {
        try { return Path.GetFullPath(path); }
        catch (ArgumentException) { return path; }
    }
}
