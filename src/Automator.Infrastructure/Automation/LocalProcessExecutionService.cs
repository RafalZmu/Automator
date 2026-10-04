using System.Diagnostics;
using System.Text;
using Automator.Application.Automation;
using Automator.Core.Automation;

namespace Automator.Infrastructure.Automation;

/// <summary>Runs explicitly configured executables without a shell and bounds all retained output.</summary>
public sealed class LocalProcessExecutionService : IAutomationProcessService
{
    public async Task<AutomationProcessResult> ExecuteAsync(AutomationProcessRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        Validate(request);

        var startInfo = new ProcessStartInfo
        {
            FileName = request.ExecutablePath,
            WorkingDirectory = request.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardErrorEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        foreach (var argument in request.Arguments) startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var stopwatch = Stopwatch.StartNew();
        if (!process.Start()) throw new InvalidOperationException("The configured process could not be started.");
        if (request.StandardInput is null) process.StandardInput.Close();

        var standardOutput = ReadBoundedAsync(process.StandardOutput);
        var standardError = ReadBoundedAsync(process.StandardError);
        using var timeout = new CancellationTokenSource(request.Timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        var standardInput = request.StandardInput is null
            ? Task.CompletedTask
            : WriteStandardInputAsync(process, request.StandardInput, linked.Token);
        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
            try { await standardInput.ConfigureAwait(false); }
            catch (IOException) when (process.HasExited) { }
        }
        catch (OperationCanceledException)
        {
            timedOut = !cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested;
            if (!process.HasExited)
            {
                try { process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }
            }
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            try { await standardInput.ConfigureAwait(false); }
            catch (IOException) when (process.HasExited) { }
            await Task.WhenAll(standardOutput, standardError).ConfigureAwait(false);
            if (!timedOut) throw new OperationCanceledException(cancellationToken);
        }

        var output = await standardOutput.ConfigureAwait(false);
        var error = await standardError.ConfigureAwait(false);
        stopwatch.Stop();
        return new AutomationProcessResult(timedOut ? null : process.ExitCode, timedOut,
            output.Text, error.Text, output.Truncated, error.Truncated, stopwatch.ElapsedMilliseconds);
    }

    private static async Task<(string Text, bool Truncated)> ReadBoundedAsync(StreamReader reader)
    {
        var buffer = new char[4096];
        var retained = new StringBuilder(Math.Min(buffer.Length, AutomationProcessLimits.MaximumOutputCharacters));
        var truncated = false;
        while (true)
        {
            var count = await reader.ReadAsync(buffer.AsMemory()).ConfigureAwait(false);
            if (count == 0) break;
            var remaining = AutomationProcessLimits.MaximumOutputCharacters - retained.Length;
            if (remaining > 0) retained.Append(buffer, 0, Math.Min(remaining, count));
            if (count > remaining) truncated = true;
        }
        return (retained.ToString(), truncated);
    }

    private static async Task WriteStandardInputAsync(Process process, string input, CancellationToken cancellationToken)
    {
        try
        {
            await process.StandardInput.WriteAsync(input.AsMemory(), cancellationToken).ConfigureAwait(false);
            await process.StandardInput.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await process.StandardInput.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static void Validate(AutomationProcessRequest request)
    {
        if (!Path.IsPathFullyQualified(request.ExecutablePath) || !File.Exists(request.ExecutablePath))
            throw new FileNotFoundException("The configured executable was not found.", request.ExecutablePath);
        if (!Path.IsPathFullyQualified(request.WorkingDirectory) || !Directory.Exists(request.WorkingDirectory))
            throw new DirectoryNotFoundException("The configured working directory was not found.");
        if (request.Arguments is null || request.Arguments.Count > AutomationProcessLimits.MaximumArgumentCount
            || request.Arguments.Any(argument => argument is null || argument.Length > AutomationProcessLimits.MaximumArgumentLength))
            throw new InvalidDataException("The process argument list is invalid or too large.");
        if (request.StandardInput is { } standardInput
            && (standardInput.Length > AutomationProcessLimits.MaximumStandardInputCharacters
                || Encoding.UTF8.GetByteCount(standardInput) > AutomationProcessLimits.MaximumStandardInputBytes))
            throw new InvalidDataException("The process standard input is too large.");
        if (request.Timeout <= TimeSpan.Zero || request.Timeout > AutomationProcessLimits.MaximumTimeout)
            throw new InvalidDataException("The process timeout must be between zero and one hour.");
    }
}
