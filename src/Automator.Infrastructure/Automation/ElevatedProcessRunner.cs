using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Automator.Core.Automation;

namespace Automator.Infrastructure.Automation;

/// <summary>
/// Runs one process elevated. Windows cannot redirect the streams of an elevated process started with the
/// "runas" verb, so a small Windows PowerShell wrapper starts the real process and relays its output, error,
/// and exit code through files in a private temporary folder. The wrapper travels on the command line and is
/// never written to disk, so nothing executable sits in a user-writable folder.
/// </summary>
internal static class ElevatedProcessRunner
{
    private const int ErrorCancelled = 1223;
    // CreateProcess limit is 32,767 characters; stay below it.
    private const int MaximumCommandLineCharacters = 30000;

    // Reads one JSON payload, starts the child with redirected streams, copies raw bytes to files, enforces the
    // deadline and a cancel marker, and always writes the exit code last.
    private const string Wrapper = """
$ErrorActionPreference = 'Stop'
$p = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{PAYLOAD}')) | ConvertFrom-Json
$code = 1
try {
  $psi = New-Object System.Diagnostics.ProcessStartInfo
  $psi.FileName = $p.exe
  $psi.Arguments = $p.commandLine
  $psi.WorkingDirectory = $p.cwd
  $psi.UseShellExecute = $false
  $psi.CreateNoWindow = $true
  $psi.RedirectStandardInput = $true
  $psi.RedirectStandardOutput = $true
  $psi.RedirectStandardError = $true
  $proc = [System.Diagnostics.Process]::Start($psi)
  $outFile = [IO.File]::Create($p.stdoutPath)
  $errFile = [IO.File]::Create($p.stderrPath)
  $outTask = $proc.StandardOutput.BaseStream.CopyToAsync($outFile)
  $errTask = $proc.StandardError.BaseStream.CopyToAsync($errFile)
  if ($p.hasInput) {
    $bytes = [IO.File]::ReadAllBytes($p.stdinPath)
    $proc.StandardInput.BaseStream.Write($bytes, 0, $bytes.Length)
  }
  $proc.StandardInput.Close()
  $deadline = [DateTime]::UtcNow.AddSeconds($p.timeoutSeconds + 5)
  while (-not $proc.WaitForExit(200)) {
    if ([IO.File]::Exists($p.cancelPath) -or [DateTime]::UtcNow -gt $deadline) {
      & "$env:SystemRoot\System32\taskkill.exe" /T /F /PID $proc.Id | Out-Null
      break
    }
  }
  [void]$proc.WaitForExit(10000)
  [void]$outTask.Wait(5000)
  [void]$errTask.Wait(5000)
  $outFile.Dispose()
  $errFile.Dispose()
  $code = $proc.ExitCode
} catch {
  try { [IO.File]::WriteAllText($p.stderrPath, $_.Exception.Message) } catch {}
} finally {
  [IO.File]::WriteAllText($p.exitPath, [string]$code)
}
""";

    public static async Task<AutomationProcessResult> ExecuteAsync(AutomationProcessRequest request, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(Path.GetTempPath(), "Automator-elevated-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var stdoutPath = Path.Combine(directory, "stdout.bin");
            var stderrPath = Path.Combine(directory, "stderr.bin");
            var stdinPath = Path.Combine(directory, "stdin.bin");
            var exitPath = Path.Combine(directory, "exit.txt");
            var cancelPath = Path.Combine(directory, "cancel.flag");
            var hasInput = request.StandardInput is not null;
            if (hasInput) await File.WriteAllBytesAsync(stdinPath, new UTF8Encoding(false).GetBytes(request.StandardInput!), cancellationToken).ConfigureAwait(false);

            var payload = JsonSerializer.Serialize(new
            {
                exe = request.ExecutablePath,
                commandLine = string.Join(' ', request.Arguments.Select(QuoteArgument)),
                cwd = request.WorkingDirectory,
                timeoutSeconds = (int)Math.Ceiling(request.Timeout.TotalSeconds),
                stdoutPath, stderrPath, stdinPath, exitPath, cancelPath, hasInput,
            });
            var script = Wrapper.Replace("{PAYLOAD}", Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)), StringComparison.Ordinal);
            var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            var windowsPowerShell = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
            var arguments = "-NoLogo -NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand " + encoded;
            if (windowsPowerShell.Length + arguments.Length + 8 > MaximumCommandLineCharacters)
                throw new InvalidDataException("The script arguments are too long to run as administrator.");

            var startInfo = new ProcessStartInfo
            {
                FileName = windowsPowerShell,
                Arguments = arguments,
                WorkingDirectory = request.WorkingDirectory,
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            var stopwatch = Stopwatch.StartNew();
            Process process;
            try { process = Process.Start(startInfo) ?? throw new InvalidOperationException("The elevated process could not be started."); }
            catch (Win32Exception exception) when (exception.NativeErrorCode == ErrorCancelled)
            {
                throw new InvalidOperationException("Administrator approval was declined, so the script did not run.");
            }
            using (process)
            {
                using var timeout = new CancellationTokenSource(request.Timeout);
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
                var timedOut = false;
                try { await process.WaitForExitAsync(linked.Token).ConfigureAwait(false); }
                catch (OperationCanceledException)
                {
                    timedOut = !cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested;
                    // An elevated process cannot be killed from here; ask the wrapper to stop its child instead.
                    try { await File.WriteAllTextAsync(cancelPath, "stop", CancellationToken.None).ConfigureAwait(false); } catch (IOException) { }
                    using var grace = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                    try { await process.WaitForExitAsync(grace.Token).ConfigureAwait(false); } catch (OperationCanceledException) { }
                    if (!timedOut) throw new OperationCanceledException(cancellationToken);
                }
                stopwatch.Stop();
                var output = ReadBounded(stdoutPath);
                var error = ReadBounded(stderrPath);
                int? exitCode = null;
                if (!timedOut)
                {
                    exitCode = File.Exists(exitPath) && int.TryParse(File.ReadAllText(exitPath).Trim(), out var parsed) ? parsed
                        : process.HasExited ? process.ExitCode : -1;
                }
                return new AutomationProcessResult(exitCode, timedOut, output.Text, error.Text, output.Truncated, error.Truncated, stopwatch.ElapsedMilliseconds);
            }
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    private static (string Text, bool Truncated) ReadBounded(string path)
    {
        if (!File.Exists(path)) return (string.Empty, false);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, new UTF8Encoding(false));
        var buffer = new char[AutomationProcessLimits.MaximumOutputCharacters + 1];
        var count = reader.ReadBlock(buffer, 0, buffer.Length);
        var truncated = count > AutomationProcessLimits.MaximumOutputCharacters;
        return (new string(buffer, 0, Math.Min(count, AutomationProcessLimits.MaximumOutputCharacters)), truncated);
    }

    /// <summary>Quotes one argument for a Windows command line (CommandLineToArgvW rules).</summary>
    internal static string QuoteArgument(string argument)
    {
        if (argument.Length > 0 && argument.IndexOfAny([' ', '\t', '\n', '\v', '"']) < 0) return argument;
        var builder = new StringBuilder("\"");
        var backslashes = 0;
        foreach (var character in argument)
        {
            if (character == '\\') { backslashes++; continue; }
            if (character == '"') builder.Append('\\', backslashes * 2 + 1).Append('"');
            else builder.Append('\\', backslashes).Append(character);
            backslashes = 0;
        }
        return builder.Append('\\', backslashes * 2).Append('"').ToString();
    }
}
