# -HelperPath tests another copy of the helper (for example an older revision, to see this spec fail).
param([string]$HelperPath)

$ErrorActionPreference = 'Stop'

# Runs scripts\Restart-Automator.ps1 against a fixture workspace with a fake Automator.exe, under Windows PowerShell 5.1
# and (when installed) PowerShell 7. The real app is never stopped or started: the helper only touches processes whose
# executable lives inside the fixture workspace, and LOCALAPPDATA points at the fixture.
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$fixtureRoot = Join-Path $repositoryRoot ('artifacts\test-data\restart-helper-' + [Guid]::NewGuid().ToString('N'))
$helperSource = if ($HelperPath) { (Resolve-Path -LiteralPath $HelperPath).Path } else { Join-Path $repositoryRoot 'scripts\Restart-Automator.ps1' }
$savedEnvironment = @{}
foreach ($name in 'LOCALAPPDATA', 'AUTOMATOR_RENDERER_URL', 'AUTOMATOR_TEST_MODE', 'AUTOMATOR_TEST_TRAY', 'AUTOMATOR_TEST_DATA_DIRECTORY', 'AUTOMATOR_BUILD_ID', 'AUTOMATOR_SHOW_ON_START', 'PORTABLE_EXECUTABLE_FILE') {
    $savedEnvironment[$name] = [System.Environment]::GetEnvironmentVariable($name, 'Process')
}

function Find-Csc {
    foreach ($folder in "$env:windir\Microsoft.NET\Framework64\v4.0.30319", "$env:windir\Microsoft.NET\Framework\v4.0.30319") {
        $candidate = Join-Path $folder 'csc.exe'
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { return $candidate }
    }
    throw 'The .NET Framework C# compiler (csc.exe) was not found.'
}

# Stands in for the Electron host. It records its AUTOMATOR_* and PORTABLE_EXECUTABLE_FILE environment, logs the same
# startup events as the real host, and stalls after Panel.NativeThemeInitialized (without logging Panel.ReadyToShow or
# App.Ready) when a launcher left one of those variables present but empty, as the real host did, or when
# stall.flag exists beside it.
$fakeHostSource = @'
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

static class FakeHost
{
    static readonly string[] Watched = {
        "AUTOMATOR_TEST_MODE", "AUTOMATOR_TEST_TRAY", "AUTOMATOR_TEST_DATA_DIRECTORY", "AUTOMATOR_SHOW_ON_START",
        "AUTOMATOR_BUILD_ID", "AUTOMATOR_RENDERER_URL", "PORTABLE_EXECUTABLE_FILE" };

    static void Log(string logPath, string name, bool visible)
    {
        string line = "{\"Timestamp\":\"" + DateTime.UtcNow.ToString("o") + "\",\"Level\":\"Information\",\"Event\":\"" + name
            + "\",\"ProcessId\":" + Process.GetCurrentProcess().Id
            + ",\"BuildId\":\"" + Environment.GetEnvironmentVariable("AUTOMATOR_BUILD_ID")
            + "\",\"visible\":" + (visible ? "true" : "false") + "}";
        File.AppendAllText(logPath, line + Environment.NewLine, new UTF8Encoding(false));
    }

    static int Main()
    {
        string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        List<string> seen = new List<string>();
        bool emptyVariable = false;
        foreach (string name in Watched)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (value == null) continue;
            seen.Add(name + "=" + value);
            if (value.Length == 0) emptyVariable = true;
        }
        File.WriteAllLines(Path.Combine(exeDirectory, "host-env.txt"), seen.ToArray());

        string testData = Environment.GetEnvironmentVariable("AUTOMATOR_TEST_DATA_DIRECTORY");
        string logDirectory = string.IsNullOrEmpty(testData)
            ? Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA"), "Automator", "Logs")
            : Path.Combine(testData, "Logs");
        Directory.CreateDirectory(logDirectory);
        string logPath = Path.Combine(logDirectory, "automator-" + DateTime.UtcNow.ToString("yyyy-MM-dd") + ".jsonl");

        Log(logPath, "App.Starting", false);
        Log(logPath, "Panel.NativeThemeInitialized", false);
        if (!emptyVariable && !File.Exists(Path.Combine(exeDirectory, "stall.flag")))
        {
            Log(logPath, "Panel.ReadyToShow", false);
            Log(logPath, "App.Ready", true);
        }
        Thread.Sleep(Timeout.Infinite);
        return 0;
    }
}
'@

# Runs the helper in a child process of the given shell and returns its exit code and output.
function Invoke-Helper([string]$shell, [string[]]$helperArguments) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $text = & $shell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File (Join-Path $fixtureRoot 'scripts\Restart-Automator.ps1') @helperArguments 2>&1 | Out-String
        [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $text }
    }
    finally { $ErrorActionPreference = $previous }
}

function Get-HostEnvironment {
    $path = Join-Path $fixtureRoot 'build\host-env.txt'
    if (-not (Test-Path -LiteralPath $path)) { throw 'The fake host never recorded its environment.' }
    @(Get-Content -LiteralPath $path | Where-Object { $_ -ne '' })
}

function Assert-HostEnvironment([string]$label, [string[]]$lines, [string[]]$expected, [string[]]$absent) {
    foreach ($line in $expected) {
        if (@($lines | Where-Object { $_ -like $line }).Count -eq 0) { throw "${label}: the host did not receive $line. It received: $($lines -join '; ')" }
    }
    foreach ($name in $absent) {
        if (@($lines | Where-Object { $_ -like "$name=*" }).Count -gt 0) { throw "${label}: the host received $name, which the helper must clear. It received: $($lines -join '; ')" }
    }
}

function Stop-FixtureProcesses {
    Get-CimInstance -ClassName Win32_Process -ErrorAction SilentlyContinue |
        Where-Object { $_.ExecutablePath -and $_.ExecutablePath.StartsWith($fixtureRoot, [System.StringComparison]::OrdinalIgnoreCase) } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
}

try {
    $build = Join-Path $fixtureRoot 'build'
    $hostExecutable = Join-Path $build 'Automator.exe'
    New-Item -ItemType Directory -Path (Join-Path $fixtureRoot 'scripts'), (Join-Path $build 'resources\backend'), (Join-Path $fixtureRoot 'localappdata') -Force | Out-Null
    Copy-Item -LiteralPath $helperSource -Destination (Join-Path $fixtureRoot 'scripts\Restart-Automator.ps1')
    Set-Content -LiteralPath (Join-Path $build 'resources\app.asar') -Value 'asar' -Encoding ASCII
    Set-Content -LiteralPath (Join-Path $build 'resources\backend\Automator.Backend.exe') -Value 'backend' -Encoding ASCII
    $sourcePath = Join-Path $fixtureRoot 'FakeHost.cs'
    Set-Content -LiteralPath $sourcePath -Value $fakeHostSource -Encoding ASCII
    $compile = & (Find-Csc) /nologo /target:winexe "/out:$hostExecutable" $sourcePath 2>&1 | Out-String
    if (-not (Test-Path -LiteralPath $hostExecutable -PathType Leaf)) { throw "The fake host did not compile: $compile" }

    $shells = [System.Collections.Generic.List[string]]::new()
    $shells.Add((Join-Path $env:windir 'System32\WindowsPowerShell\v1.0\powershell.exe'))
    $pwsh = Get-Command pwsh.exe -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($pwsh) { $shells.Add($pwsh.Source) } else { Write-Output 'PowerShell 7 (pwsh.exe) is not installed; only Windows PowerShell 5.1 was checked.' }

    # A development shell may export these. The helper must hand the packaged host none of them, and must not leave
    # an empty variable behind when it clears one: PowerShell 7 turns $null into '' for .NET string parameters.
    $env:LOCALAPPDATA = Join-Path $fixtureRoot 'localappdata'
    $env:AUTOMATOR_RENDERER_URL = 'http://127.0.0.1:5173'
    $env:AUTOMATOR_TEST_MODE = '1'
    $env:AUTOMATOR_TEST_TRAY = '1'
    $env:AUTOMATOR_TEST_DATA_DIRECTORY = Join-Path $fixtureRoot 'someone-elses-data'
    $env:PORTABLE_EXECUTABLE_FILE = Join-Path $fixtureRoot 'portable.exe'

    $everyWatched = 'AUTOMATOR_TEST_MODE', 'AUTOMATOR_TEST_TRAY', 'AUTOMATOR_TEST_DATA_DIRECTORY', 'AUTOMATOR_RENDERER_URL', 'PORTABLE_EXECUTABLE_FILE'
    foreach ($shell in $shells) {
        $label = [System.IO.Path]::GetFileNameWithoutExtension($shell) + ' ' + (Split-Path -Parent $shell | Split-Path -Leaf)

        # Normal start: the host sees only the variables the helper sets, none empty.
        $result = Invoke-Helper $shell @('-ExecutablePath', $hostExecutable, '-Show')
        if ($result.ExitCode -ne 0 -or $result.Output -notlike '*Automator is ready*') { throw "$label normal start failed: $($result.Output)" }
        Assert-HostEnvironment "$label normal start" (Get-HostEnvironment) @('AUTOMATOR_SHOW_ON_START=1', 'AUTOMATOR_BUILD_ID=host-*') $everyWatched

        # Test mode: isolated data directory under the fixture workspace, no leftover dev renderer URL.
        $result = Invoke-Helper $shell @('-ExecutablePath', $hostExecutable, '-Show', '-TestMode')
        if ($result.ExitCode -ne 0 -or $result.Output -notlike '*Automator is ready*') { throw "$label test mode start failed: $($result.Output)" }
        $testData = Join-Path $fixtureRoot 'artifacts\test-data\electron-host'
        Assert-HostEnvironment "$label test mode" (Get-HostEnvironment) @('AUTOMATOR_TEST_MODE=1', 'AUTOMATOR_TEST_TRAY=1', "AUTOMATOR_TEST_DATA_DIRECTORY=$testData", 'AUTOMATOR_BUILD_ID=test-host-*') @('AUTOMATOR_RENDERER_URL', 'PORTABLE_EXECUTABLE_FILE')
    }

    # A host that never signals readiness is reported as a stall that names the last event it logged, and the
    # helper fails instead of claiming success.
    foreach ($shell in $shells) {
        $label = [System.IO.Path]::GetFileNameWithoutExtension($shell) + ' ' + (Split-Path -Parent $shell | Split-Path -Leaf)
        Set-Content -LiteralPath (Join-Path $build 'stall.flag') -Value 'x' -Encoding ASCII
        $result = Invoke-Helper $shell @('-ExecutablePath', $hostExecutable, '-Show', '-StartupTimeoutSeconds', '5')
        Remove-Item -LiteralPath (Join-Path $build 'stall.flag') -Force
        if ($result.ExitCode -eq 0) { throw "$label reported success for a host that never became ready: $($result.Output)" }
        # The console wraps the error record at its width, even inside words, and PowerShell 7 adds a '|' gutter,
        # so compare without whitespace and gutters.
        $flat = $result.Output -replace '[\s|]', ''
        if ($flat -notlike '*didnotlogafreshApp.Readywithin5seconds*' -or $flat -notlike '*lastloggedeventwasPanel.NativeThemeInitialized*') {
            throw "$label stall report is wrong: $($result.Output)"
        }
    }

    Write-Output "Restart-Automator helper: host environment (no empty or leftover variables, normal and test mode) and truthful stall report passed under $($shells.Count) shell(s)."
}
finally {
    Stop-FixtureProcesses
    foreach ($name in $savedEnvironment.Keys) {
        if ($null -eq $savedEnvironment[$name]) { Remove-Item -LiteralPath "Env:$name" -ErrorAction SilentlyContinue }
        else { Set-Item -LiteralPath "Env:$name" -Value $savedEnvironment[$name] }
    }
    Start-Sleep -Milliseconds 300
    if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force -ErrorAction SilentlyContinue }
}
