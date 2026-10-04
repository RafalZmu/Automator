using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Automator.Core.Launcher;
using Microsoft.Win32;

namespace Automator.Services;

public sealed class AppCatalogService
{
    public Task<IReadOnlyList<AppBinding>> DiscoverAsync() => Task.Run(() =>
    {
        try { return Discover(); }
        catch (Exception exception)
        {
            AppLogger.Error("Catalog.DiscoveryCrashed", "Application discovery failed unexpectedly.", exception);
            throw;
        }
    });

    public AppBinding? CaptureUnderCursor()
    {
        if (!NativeMethods.GetCursorPos(out var point))
        {
            AppLogger.Warning("Catalog.CursorCaptureFailed", "Could not read the pointer position when opening the launcher.",
                properties: new Dictionary<string, object?> { ["win32Error"] = Marshal.GetLastWin32Error() });
            return null;
        }

        var window = NativeMethods.GetAncestor(NativeMethods.WindowFromPoint(point), NativeMethods.GA_ROOT);
        if (window == IntPtr.Zero)
        {
            return null;
        }

        NativeMethods.GetWindowThreadProcessId(window, out var processId);
        try
        {
            using var process = Process.GetProcessById((int)processId);
            var path = process.MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(path) || processId == (uint)Environment.ProcessId)
            {
                return null;
            }

            var name = process.MainWindowTitle;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = process.ProcessName;
            }

            return new AppBinding(CreateId(path, string.Empty), name, path, string.Empty);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
        {
            AppLogger.Warning("Catalog.ForegroundAppCaptureFailed", "Could not capture the application under the pointer.", exception,
                new Dictionary<string, object?> { ["processId"] = processId });
            return null;
        }
    }

    public static AppBinding FromCustomPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var (target, arguments) = fullPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)
            ? ResolveShortcut(fullPath)
            : (fullPath, string.Empty);
        var name = Path.GetFileNameWithoutExtension(fullPath);
        return new AppBinding(CreateId(target, arguments), name, target, string.Empty, arguments);
    }

    private static IReadOnlyList<AppBinding> Discover()
    {
        AppLogger.Info("Catalog.DiscoveryStarted", "Started discovering installed applications.");
        var entries = new Dictionary<string, AppBinding>(StringComparer.OrdinalIgnoreCase);
        var startMenuRoots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
        };

        foreach (var root in startMenuRoots.Where(Directory.Exists))
        {
            IEnumerable<string> shortcuts;
            try
            {
                shortcuts = Directory.EnumerateFiles(root, "*.lnk", SearchOption.AllDirectories).ToArray();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                AppLogger.Warning("Catalog.StartMenuReadFailed", "Could not enumerate a Start menu folder.", exception,
                    new Dictionary<string, object?> { ["folder"] = root });
                continue;
            }

            foreach (var shortcut in shortcuts)
            {
                try
                {
                    var (target, arguments) = ResolveShortcut(shortcut);
                    AddEntry(entries, Path.GetFileNameWithoutExtension(shortcut), target, arguments);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or COMException or InvalidOperationException)
                {
                    // Ignore stale shortcuts; the user can add their target manually.
                    AppLogger.Warning("Catalog.ShortcutSkipped", "A Start menu shortcut could not be resolved.", exception,
                        new Dictionary<string, object?> { ["shortcut"] = shortcut });
                }
            }
        }

        AddRegisteredAppPaths(entries, Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths"));
        AddRegisteredAppPaths(entries, Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths"));

        var discovered = entries.Values.OrderBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
        AppLogger.Info("Catalog.DiscoveryCompleted", "Application discovery completed.",
            new Dictionary<string, object?> { ["applicationCount"] = discovered.Length });
        return discovered;
    }

    private static void AddRegisteredAppPaths(IDictionary<string, AppBinding> entries, RegistryKey? root)
    {
        using (root)
        {
            if (root is null)
            {
                return;
            }

            foreach (var childName in root.GetSubKeyNames())
            {
                using var child = root.OpenSubKey(childName);
                var target = child?.GetValue(null) as string;
                if (!string.IsNullOrWhiteSpace(target))
                {
                    AddEntry(entries, Path.GetFileNameWithoutExtension(childName), target, string.Empty);
                }
            }
        }
    }

    private static void AddEntry(IDictionary<string, AppBinding> entries, string name, string target, string arguments)
    {
        target = Environment.ExpandEnvironmentVariables(target.Trim().Trim('"'));
        if (!Path.IsPathFullyQualified(target) || !File.Exists(target))
        {
            return;
        }

        var normalized = Path.GetFullPath(target);
        var id = CreateId(normalized, arguments);
        entries.TryAdd(id, new AppBinding(id, name, normalized, string.Empty, arguments));
    }

    private static (string Target, string Arguments) ResolveShortcut(string path)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host is unavailable for shortcut resolution.");
        object? shell = null;
        object? shortcut = null;
        try
        {
            shell = Activator.CreateInstance(shellType);
            dynamic automationShell = shell ?? throw new InvalidOperationException("Windows Script Host could not be started.");
            shortcut = automationShell.CreateShortcut(path);
            dynamic shellLink = shortcut;
            string target = shellLink.TargetPath;
            string arguments = shellLink.Arguments;
            if (string.IsNullOrWhiteSpace(target))
            {
                throw new InvalidOperationException("The shortcut has no target.");
            }

            return (target, arguments ?? string.Empty);
        }
        finally
        {
            if (shortcut is not null && Marshal.IsComObject(shortcut))
            {
                Marshal.FinalReleaseComObject(shortcut);
            }

            if (shell is not null && Marshal.IsComObject(shell))
            {
                Marshal.FinalReleaseComObject(shell);
            }
        }
    }

    private static string CreateId(string target, string arguments)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{target}\n{arguments}".ToUpperInvariant()));
        return Convert.ToHexString(bytes.AsSpan(0, 8)).ToLowerInvariant();
    }
}
