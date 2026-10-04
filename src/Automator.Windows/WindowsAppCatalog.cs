using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Automator.Application.Logging;
using Automator.Core.Launcher;
using Automator.Windows.Interop;
using Microsoft.Win32;

namespace Automator.Windows;

public sealed class WindowsAppCatalog
{
    private readonly int _hostProcessId;
    private readonly string _hostExecutablePath;
    private readonly IApplicationLog _log;

    public WindowsAppCatalog(int hostProcessId, string hostExecutablePath, IApplicationLog? log = null)
    {
        _hostProcessId = hostProcessId;
        _hostExecutablePath = NormalizePath(hostExecutablePath);
        _log = log ?? NullApplicationLog.Instance;
    }

    public Task<IReadOnlyList<AppBinding>> DiscoverAsync(CancellationToken cancellationToken = default) => Task.Run(
        () => Discover(cancellationToken), cancellationToken);

    public AppBinding? CaptureUnderCursor()
    {
        if (!WindowsNativeMethods.GetCursorPos(out var point))
        {
            _log.Write(ApplicationLogLevel.Warning, "Catalog.CursorCaptureFailed", "Could not read the pointer position.",
                properties: new Dictionary<string, object?> { ["win32Error"] = Marshal.GetLastWin32Error() });
            return null;
        }

        var window = WindowsNativeMethods.GetAncestor(WindowsNativeMethods.WindowFromPoint(point), WindowsNativeMethods.GaRoot);
        if (window == IntPtr.Zero) return null;
        WindowsNativeMethods.GetWindowThreadProcessId(window, out var processId);
        if (ShouldExcludeProcess(processId)) return null;

        try
        {
            using var process = Process.GetProcessById((int)processId);
            var path = process.MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(path)) return null;
            var title = new StringBuilder(512);
            WindowsNativeMethods.GetWindowText(window, title, title.Capacity);
            var name = title.ToString().Trim();
            if (name.Length == 0) name = process.ProcessName;
            var normalized = NormalizePath(path);
            return new AppBinding(CreateId(normalized, string.Empty), name, normalized, string.Empty);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
        {
            _log.Write(ApplicationLogLevel.Warning, "Catalog.ForegroundAppCaptureFailed", "Could not read the app under the pointer.", exception,
                new Dictionary<string, object?> { ["processId"] = processId });
            return null;
        }
    }

    public AppBinding FromCustomPath(string path)
    {
        if (!Path.IsPathFullyQualified(path) || !File.Exists(path)) throw new FileNotFoundException("The selected app path does not exist.", path);
        var fullPath = NormalizePath(path);
        var (target, arguments) = fullPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)
            ? ResolveShortcut(fullPath)
            : (fullPath, string.Empty);
        if (!File.Exists(target)) throw new FileNotFoundException("The selected app target does not exist.", target);
        if (ShouldExcludeTarget(target)) throw new InvalidOperationException("Automator cannot be added as an application binding.");
        var name = Path.GetFileNameWithoutExtension(path);
        return new AppBinding(CreateId(target, arguments), name, NormalizePath(target), string.Empty, arguments);
    }

    private IReadOnlyList<AppBinding> Discover(CancellationToken cancellationToken)
    {
        var entries = new Dictionary<string, AppBinding>(StringComparer.OrdinalIgnoreCase);
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
        };
        foreach (var root in roots.Where(Directory.Exists))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string[] shortcuts;
            try { shortcuts = Directory.EnumerateFiles(root, "*.lnk", SearchOption.AllDirectories).ToArray(); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _log.Write(ApplicationLogLevel.Warning, "Catalog.StartMenuReadFailed", "Could not enumerate a Start menu folder.", exception,
                    new Dictionary<string, object?> { ["folder"] = root });
                continue;
            }

            foreach (var shortcut in shortcuts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var (target, arguments) = ResolveShortcut(shortcut);
                    AddEntry(entries, Path.GetFileNameWithoutExtension(shortcut), target, arguments);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or COMException or InvalidOperationException)
                {
                    _log.Write(ApplicationLogLevel.Warning, "Catalog.ShortcutSkipped", "A Start menu shortcut could not be resolved.", exception,
                        new Dictionary<string, object?> { ["shortcut"] = shortcut });
                }
            }
        }

        AddRegisteredAppPaths(entries, Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths"));
        AddRegisteredAppPaths(entries, Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths"));
        var apps = entries.Values.OrderBy(app => app.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
        _log.Write(ApplicationLogLevel.Information, "Catalog.DiscoveryCompleted", "Application discovery completed.",
            properties: new Dictionary<string, object?> { ["applicationCount"] = apps.Length });
        return apps;
    }

    private void AddEntry(IDictionary<string, AppBinding> entries, string name, string target, string arguments)
    {
        target = Environment.ExpandEnvironmentVariables(target.Trim().Trim('"'));
        if (!Path.IsPathFullyQualified(target) || !File.Exists(target)) return;
        var normalized = NormalizePath(target);
        if (ShouldExcludeTarget(normalized)) return;
        var id = CreateId(normalized, arguments);
        entries.TryAdd(id, new AppBinding(id, name, normalized, string.Empty, arguments));
    }

    private void AddRegisteredAppPaths(IDictionary<string, AppBinding> entries, RegistryKey? root)
    {
        using (root)
        {
            if (root is null) return;
            foreach (var childName in root.GetSubKeyNames())
            {
                using var child = root.OpenSubKey(childName);
                if (child?.GetValue(null) is string target && !string.IsNullOrWhiteSpace(target))
                    AddEntryStatic(entries, Path.GetFileNameWithoutExtension(childName), target, string.Empty);
            }
        }
    }

    private void AddEntryStatic(IDictionary<string, AppBinding> entries, string name, string target, string arguments)
    {
        target = Environment.ExpandEnvironmentVariables(target.Trim().Trim('"'));
        if (!Path.IsPathFullyQualified(target) || !File.Exists(target)) return;
        var normalized = NormalizePath(target);
        if (ShouldExcludeTarget(normalized)) return;
        var id = CreateId(normalized, arguments);
        entries.TryAdd(id, new AppBinding(id, name, normalized, string.Empty, arguments));
    }

    private static (string Target, string Arguments) ResolveShortcut(string path)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host is unavailable.");
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
            if (string.IsNullOrWhiteSpace(target)) throw new InvalidOperationException("Shortcut has no target.");
            return (Environment.ExpandEnvironmentVariables(target), arguments ?? string.Empty);
        }
        finally
        {
            if (shortcut is not null && Marshal.IsComObject(shortcut)) Marshal.FinalReleaseComObject(shortcut);
            if (shell is not null && Marshal.IsComObject(shell)) Marshal.FinalReleaseComObject(shell);
        }
    }

    private bool ShouldExcludeProcess(uint processId)
    {
        if (processId is 0 || processId == (uint)Environment.ProcessId || processId == (uint)_hostProcessId) return true;
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

    private bool ShouldExcludeTarget(string path) =>
        string.Equals(NormalizePath(path), _hostExecutablePath, StringComparison.OrdinalIgnoreCase)
        || string.Equals(NormalizePath(path), NormalizePath(Environment.ProcessPath ?? string.Empty), StringComparison.OrdinalIgnoreCase);

    private static string NormalizePath(string path)
    {
        try { return Path.GetFullPath(path); }
        catch (ArgumentException) { return path; }
    }

    private static string CreateId(string target, string arguments)
    {
        var normalized = $"{NormalizePath(target).ToUpperInvariant()}\n{arguments}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)).AsSpan(0, 8)).ToLowerInvariant();
    }
}
