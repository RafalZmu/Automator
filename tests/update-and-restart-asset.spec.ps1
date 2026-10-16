$ErrorActionPreference = 'Stop'

$fixtureRoot = Join-Path $PSScriptRoot ('..\artifacts\test-data\update-and-restart-' + [Guid]::NewGuid().ToString('N'))
$assetPath = (Resolve-Path (Join-Path $PSScriptRoot '..\src\Automator.Application\Automation\Templates\update-and-restart.v1.ps1')).Path
$originalPath = $env:PATH

function Invoke-Git([string]$repo, [string[]]$arguments) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $text = & git -C $repo -c user.name=spec -c user.email=spec@example.invalid @arguments 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) { throw "git $($arguments -join ' ') failed: $text" }
    }
    finally { $ErrorActionPreference = $previous }
}

# Runs the asset the way Script Runner does: one positional argument per declared parameter.
function Invoke-Asset([string]$folder, [string]$rebuild = 'false') {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $text = & powershell.exe -NoProfile -NonInteractive -File $assetPath $folder $rebuild 2>&1 | Out-String
        [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $text }
    }
    finally { $ErrorActionPreference = $previous }
}

function Get-Lines([string]$path) {
    if (Test-Path -LiteralPath $path) { @(Get-Content -LiteralPath $path | Where-Object { $_ -ne '' }) } else { @() }
}

function Wait-ForText([string]$path, [string]$pattern, [int]$seconds = 90) {
    $deadline = (Get-Date).AddSeconds($seconds)
    while ((Get-Date) -lt $deadline) {
        try {
            if ((Test-Path -LiteralPath $path) -and ((Get-Content -LiteralPath $path -Raw) -match $pattern)) { return $true }
        }
        catch { }
        Start-Sleep -Milliseconds 300
    }
    $false
}

function Get-LogPath([string]$output) {
    if ($output -notmatch 'Progress log: (.+)') { throw "No progress log in the output: $output" }
    $Matches[1].Trim()
}

try {
    $bin = Join-Path $fixtureRoot 'bin'
    $origin = Join-Path $fixtureRoot 'origin.git'
    $seed = Join-Path $fixtureRoot 'seed'
    $work = Join-Path $fixtureRoot 'checkout'
    $npmCalls = Join-Path $bin 'npm-calls.txt'
    $restartCalls = Join-Path $work 'restart-calls.txt'
    New-Item -ItemType Directory -Path $bin, $seed -Force | Out-Null

    # A fake npm records its arguments and produces the files a real package:win build would.
    Set-Content -LiteralPath (Join-Path $bin 'npm.cmd') -Encoding ASCII -Value @'
@echo off
echo %*>>"%~dp0npm-calls.txt"
rem Like npm with install scripts blocked: dependencies arrive, Electron's runtime does not.
if "%1"=="ci" mkdir node_modules\electron 2>nul
if "%1"=="ci" copy /y "%~dp0fake-electron-install.js" node_modules\electron\install.js >nul
if not "%1 %2"=="run package:win" exit /b 0
if exist "%~dp0fail-package.flag" goto fail
mkdir artifacts\electron-dist\win-unpacked 2>nul
echo exe>artifacts\electron-dist\win-unpacked\Automator.exe
exit /b 0
:fail
echo packaging blew up 1>&2
exit /b 3
'@
    Set-Content -LiteralPath (Join-Path $bin 'fake-electron-install.js') -Encoding ASCII -Value @'
const fs = require('fs');
const path = require('path');
fs.mkdirSync(path.join(__dirname, 'dist'), { recursive: true });
fs.writeFileSync(path.join(__dirname, 'dist', 'electron.exe'), 'x');
'@
    Set-Content -LiteralPath (Join-Path $seed 'notes.txt') -Value 'notes' -Encoding ASCII
    Set-Content -LiteralPath (Join-Path $seed 'package.json') -Value '{"name":"fixture"}' -Encoding ASCII
    Set-Content -LiteralPath (Join-Path $seed 'package-lock.json') -Value '{"lockfileVersion":3}' -Encoding ASCII
    New-Item -ItemType Directory -Path (Join-Path $seed 'scripts') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $seed 'scripts\Restart-Automator.ps1') -Encoding ASCII -Value @'
param([switch]$Show, [switch]$StopOnly)
if ($Show -and (Test-Path -LiteralPath (Join-Path $PSScriptRoot '..\fail-restart.flag'))) { throw 'Could not find a complete Automator build.' }
$mode = 'plain'
if ($StopOnly) { $mode = 'stop' } elseif ($Show) { $mode = 'show' }
Add-Content -LiteralPath (Join-Path $PSScriptRoot '..\restart-calls.txt') -Value $mode
'@
    Invoke-Git $fixtureRoot @('init', '--bare', '-b', 'main', $origin)
    Invoke-Git $seed @('init', '-b', 'main')
    Invoke-Git $seed @('add', '.')
    Invoke-Git $seed @('commit', '-m', 'Initial commit')
    Invoke-Git $seed @('remote', 'add', 'origin', $origin)
    Invoke-Git $seed @('push', 'origin', 'main')
    Invoke-Git $fixtureRoot @('clone', $origin, $work)
    $env:PATH = "$bin;$originalPath"
    # Like a system gitconfig with pull.rebase=true: a plain 'git pull --ff-only' would rebase and refuse a dirty tree.
    $env:GIT_CONFIG_COUNT = '1'
    $env:GIT_CONFIG_KEY_0 = 'pull.rebase'
    $env:GIT_CONFIG_VALUE_0 = 'true'
    # An uncommitted edit that the pull must leave alone.
    Set-Content -LiteralPath (Join-Path $work 'notes.txt') -Value 'my edit' -Encoding ASCII

    # Invalid input stops before anything is touched.
    $result = Invoke-Asset 'relative\folder'
    if ($result.ExitCode -eq 0 -or $result.Output -notlike '*fully qualified*') { throw 'Relative checkout folder was accepted' }
    $result = Invoke-Asset (Join-Path $fixtureRoot 'missing')
    if ($result.ExitCode -eq 0 -or $result.Output -notlike '*does not exist*') { throw 'Missing checkout folder was accepted' }
    $result = Invoke-Asset $fixtureRoot
    if ($result.ExitCode -eq 0 -or $result.Output -notlike '*not an Automator checkout*') { throw 'A folder without a checkout was accepted' }

    # Nothing new on the remote: the running app is left alone.
    $result = Invoke-Asset $work
    if ($result.ExitCode -ne 0 -or $result.Output -notlike '*Already up to date*') { throw "Up-to-date run failed: $($result.Output)" }
    if ((Get-Lines $restartCalls).Count -ne 0 -or (Get-Lines $npmCalls).Count -ne 0) { throw 'An up-to-date checkout was rebuilt' }

    # New commit on the remote: pull, then rebuild in a worker that survives the launcher's process tree.
    Set-Content -LiteralPath (Join-Path $seed 'feature.txt') -Value 'new work' -Encoding ASCII
    Invoke-Git $seed @('add', '.')
    Invoke-Git $seed @('commit', '-m', 'Add the new feature')
    Invoke-Git $seed @('push', 'origin', 'main')
    $backendScript = Join-Path $fixtureRoot 'fake-backend.ps1'
    $backendOutput = Join-Path $fixtureRoot 'fake-backend.txt'
    Set-Content -LiteralPath $backendScript -Encoding ASCII -Value @'
param($Asset, $Repo, $Marker)
& powershell.exe -NoProfile -NonInteractive -File $Asset $Repo 'false' *> $Marker
Start-Sleep -Seconds 120
'@
    $backend = Start-Process powershell.exe -PassThru -WindowStyle Hidden -ArgumentList @(
        '-NoProfile', '-File', $backendScript, $assetPath, $work, $backendOutput)
    if (-not (Wait-ForText $backendOutput 'Progress log:' 60)) { throw 'The launcher never reported the background rebuild' }
    $launcherOutput = Get-Content -LiteralPath $backendOutput -Raw
    # Like Automator stopping its backend: end the launcher's whole process tree before the worker has started.
    & taskkill.exe /PID $backend.Id /T /F | Out-Null
    if ($launcherOutput -notlike '*Updated *' -or $launcherOutput -notlike '*Add the new feature*') { throw "Pull summary is missing: $launcherOutput" }
    if (-not (Test-Path -LiteralPath (Join-Path $work 'feature.txt'))) { throw 'The pull did not bring in the new commit' }
    $log = Get-LogPath $launcherOutput
    if (-not (Wait-ForText $log 'Finished:|FAILED:')) { throw 'The worker never finished after its launcher was killed' }
    if ((Get-Content -LiteralPath $log -Raw) -notmatch 'Finished: Automator was rebuilt and restarted') { throw "Worker failed: $(Get-Content -LiteralPath $log -Raw)" }
    if ((Get-Lines $restartCalls) -join ',' -ne 'stop,show') { throw "Expected stop then show, got: $((Get-Lines $restartCalls) -join ',')" }
    if ((Get-Lines $npmCalls) -join '|' -ne 'ci|run package:win') { throw "Expected ci then package, got: $((Get-Lines $npmCalls) -join '|')" }
    if (-not (Test-Path -LiteralPath (Join-Path $work 'node_modules\.automator-dependency-stamp'))) { throw 'The dependency stamp was not written' }
    if ((Get-Content -LiteralPath $log -Raw) -notlike '*==> Install the Electron Windows runtime*' -or
        -not (Test-Path -LiteralPath (Join-Path $work 'node_modules\electron\dist\electron.exe'))) { throw 'The missing Electron runtime was not installed' }
    if ((Get-Content -LiteralPath (Join-Path $work 'notes.txt') -Raw).Trim() -ne 'my edit') { throw 'The pull changed an uncommitted edit' }

    # Rebuild anyway: dependencies are unchanged, so only the package step repeats.
    $result = Invoke-Asset $work 'true'
    if ($result.ExitCode -ne 0 -or $result.Output -notlike '*rebuilding anyway*') { throw "Forced rebuild did not start: $($result.Output)" }
    $log = Get-LogPath $result.Output
    if (-not (Wait-ForText $log 'Finished:|FAILED:')) { throw 'The forced rebuild never finished' }
    if ((Get-Content -LiteralPath $log -Raw) -notmatch 'Finished:') { throw "Forced rebuild failed: $(Get-Content -LiteralPath $log -Raw)" }
    if ((Get-Content -LiteralPath $log -Raw) -like '*Electron Windows runtime*') { throw 'An installed Electron runtime was installed again' }
    if ((Get-Lines $npmCalls) -join '|' -ne 'ci|run package:win|run package:win') { throw "Dependencies were reinstalled: $((Get-Lines $npmCalls) -join '|')" }

    # A failed build is logged. The app is started again when a build is left, and the outcome says whether it was.
    function Invoke-FailingWorker([string]$logFile) {
        $previous = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $text = & powershell.exe -NoProfile -NonInteractive -File $assetPath -Worker -RepositoryFolder $work -RebuildAnyway true -LogPath $logFile -ToolDirectories $bin -NoDialog 2>&1 | Out-String
            [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $text }
        }
        finally { $ErrorActionPreference = $previous }
    }
    Set-Content -LiteralPath (Join-Path $bin 'fail-package.flag') -Value 'x' -Encoding ASCII
    Remove-Item -LiteralPath $restartCalls -Force
    $failedLog = Join-Path $fixtureRoot 'failed-worker.log'
    $failed = Invoke-FailingWorker $failedLog
    if ($failed.ExitCode -eq 0) { throw "A failed build exited successfully: $($failed.Output) $(Get-Content -LiteralPath $failedLog -Raw)" }
    $failedText = Get-Content -LiteralPath $failedLog -Raw
    if ($failedText -notlike '*packaging blew up*' -or $failedText -notmatch 'FAILED: Build the packaged Windows app failed') { throw "Failure was not logged: $failedText" }
    if ((Get-Lines $restartCalls) -join ',' -ne 'stop,show') { throw "Expected the app to be restarted after the failure, got: $((Get-Lines $restartCalls) -join ',')" }
    if ($failedText -notlike '*Automator was restarted with the previous build.*') { throw "Restart outcome was not logged: $failedText" }

    # When no build is left (packaging clears the old one first), the outcome says Automator stays closed.
    Set-Content -LiteralPath (Join-Path $work 'fail-restart.flag') -Value 'x' -Encoding ASCII
    $closedLog = Join-Path $fixtureRoot 'closed-worker.log'
    $closed = Invoke-FailingWorker $closedLog
    $closedText = Get-Content -LiteralPath $closedLog -Raw
    if ($closed.ExitCode -eq 0 -or $closedText -notlike '*Automator is closed: no intact build is left to start*' -or $closedText -like '*was restarted with the previous build*') { throw "Closed outcome was not reported: $closedText" }
    Remove-Item -LiteralPath (Join-Path $work 'fail-restart.flag'), (Join-Path $bin 'fail-package.flag') -Force

    # Local commits that diverge from the remote make the pull fail before the app is touched.
    Set-Content -LiteralPath (Join-Path $work 'local.txt') -Value 'local' -Encoding ASCII
    Invoke-Git $work @('add', 'local.txt')
    Invoke-Git $work @('commit', '-m', 'Local only')
    Set-Content -LiteralPath (Join-Path $seed 'remote.txt') -Value 'remote' -Encoding ASCII
    Invoke-Git $seed @('add', '.')
    Invoke-Git $seed @('commit', '-m', 'Remote only')
    Invoke-Git $seed @('push', 'origin', 'main')
    Remove-Item -LiteralPath $restartCalls -Force -ErrorAction SilentlyContinue
    $headBefore = (& git -C $work rev-parse HEAD)
    $result = Invoke-Asset $work
    if ($result.ExitCode -eq 0 -or $result.Output -notlike '*git pull failed*') { throw "A diverged checkout was not rejected: $($result.Output)" }
    if ((Get-Lines $restartCalls).Count -ne 0) { throw 'Automator was stopped although the pull failed' }
    # With pull.rebase=true an unguarded pull would have rewritten the local commit instead of refusing.
    if ((& git -C $work rev-parse HEAD) -ne $headBefore) { throw 'The pull rewrote a local commit' }

    Write-Output 'Update and restart asset: input checks, up-to-date skip, pull with pull.rebase set and an uncommitted edit, detached rebuild, Electron runtime install, forced rebuild, failure outcomes, and failed pull passed.'
}
finally {
    Remove-Item Env:GIT_CONFIG_COUNT, Env:GIT_CONFIG_KEY_0, Env:GIT_CONFIG_VALUE_0 -ErrorAction SilentlyContinue
    $env:PATH = $originalPath
    if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force -ErrorAction SilentlyContinue }
}
