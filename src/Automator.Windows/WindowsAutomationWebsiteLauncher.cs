using System.Diagnostics;
using Microsoft.Win32;
using Automator.Application.Automation;

namespace Automator.Windows;

/// <summary>Opens HTTP(S) URL groups in the Windows default browser where its command line supports it.</summary>
public sealed class WindowsAutomationWebsiteLauncher : IAutomationWebsiteLauncher
{
    private readonly Action<ProcessStartInfo> _startProcess;
    private readonly string? _defaultBrowserExecutable;

    public WindowsAutomationWebsiteLauncher() : this(StartProcess, ResolveDefaultBrowserExecutable()) { }

    /// <summary>Allows specs to inspect requests and select a known browser without changing Windows associations.</summary>
    public WindowsAutomationWebsiteLauncher(Action<ProcessStartInfo> startProcess, string? defaultBrowserExecutable)
    {
        _startProcess = startProcess ?? throw new ArgumentNullException(nameof(startProcess));
        _defaultBrowserExecutable = defaultBrowserExecutable;
    }

    public Task LaunchAsync(IReadOnlyList<IReadOnlyList<Uri>> groups, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(groups);
        var validatedGroups = new List<Uri[]>(groups.Count);
        foreach (var group in groups)
        {
            ArgumentNullException.ThrowIfNull(group);
            var validated = new Uri[group.Count];
            for (var index = 0; index < group.Count; index++)
            {
                var url = group[index];
                if (url is null || !url.IsAbsoluteUri || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps))
                    throw new ArgumentException("Only absolute HTTP and HTTPS website URLs can be launched.", nameof(groups));
                validated[index] = url;
            }
            validatedGroups.Add(validated);
        }

        var browser = Classify(_defaultBrowserExecutable);
        foreach (var group in validatedGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (group.Length == 0) continue;
            if (browser == BrowserKind.Chromium)
            {
                var startInfo = new ProcessStartInfo(_defaultBrowserExecutable!) { UseShellExecute = false };
                startInfo.ArgumentList.Add("--new-window");
                foreach (var url in group) startInfo.ArgumentList.Add(url.AbsoluteUri);
                _startProcess(startInfo);
            }
            else if (browser == BrowserKind.Firefox)
            {
                var startInfo = new ProcessStartInfo(_defaultBrowserExecutable!) { UseShellExecute = false };
                startInfo.ArgumentList.Add("-new-window");
                startInfo.ArgumentList.Add(group[0].AbsoluteUri);
                foreach (var url in group.Skip(1))
                {
                    startInfo.ArgumentList.Add("-new-tab");
                    startInfo.ArgumentList.Add(url.AbsoluteUri);
                }
                _startProcess(startInfo);
            }
            else
            {
                foreach (var url in group)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _startProcess(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
                }
            }
        }

        return Task.CompletedTask;
    }

    private static BrowserKind Classify(string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath)) return BrowserKind.Unknown;
        var name = Path.GetFileNameWithoutExtension(executablePath);
        if (name.Equals("firefox", StringComparison.OrdinalIgnoreCase)) return BrowserKind.Firefox;
        if (name.Equals("chrome", StringComparison.OrdinalIgnoreCase)
            || name.Equals("msedge", StringComparison.OrdinalIgnoreCase)
            || name.Equals("brave", StringComparison.OrdinalIgnoreCase)
            || name.Equals("chromium", StringComparison.OrdinalIgnoreCase)
            || name.Equals("opera", StringComparison.OrdinalIgnoreCase)
            || name.Equals("vivaldi", StringComparison.OrdinalIgnoreCase)) return BrowserKind.Chromium;
        return BrowserKind.Unknown;
    }

    private static string? ResolveDefaultBrowserExecutable()
    {
        foreach (var scheme in new[] { "https", "http" })
        {
            try
            {
                using var userChoice = Registry.CurrentUser.OpenSubKey($@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\{scheme}\UserChoice");
                var programId = userChoice?.GetValue("ProgId") as string;
                if (string.IsNullOrWhiteSpace(programId)) continue;
                using var command = Registry.ClassesRoot.OpenSubKey($@"{programId}\shell\open\command");
                var commandLine = command?.GetValue(null) as string;
                var executable = ExtractExecutable(commandLine);
                if (!string.IsNullOrWhiteSpace(executable)) return executable;
            }
            catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                // Association lookup can be unavailable for managed or damaged profiles; fall back to shell URI handling.
            }
        }
        return null;
    }

    private static string? ExtractExecutable(string? commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine)) return null;
        var expanded = Environment.ExpandEnvironmentVariables(commandLine.Trim());
        if (expanded[0] == '"')
        {
            var endQuote = expanded.IndexOf('"', 1);
            return endQuote > 1 ? expanded[1..endQuote] : null;
        }

        var exeEnd = expanded.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        return exeEnd >= 0 ? expanded[..(exeEnd + 4)].Trim() : null;
    }

    private static void StartProcess(ProcessStartInfo startInfo)
    {
        using var process = Process.Start(startInfo);
    }

    private enum BrowserKind { Unknown, Chromium, Firefox }
}
