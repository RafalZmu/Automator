param(
    [Parameter(Mandatory = $true, Position = 0)][string]$RepositoryFolder,
    [Parameter(Mandatory = $true, Position = 1)][ValidateSet('true', 'false')][string]$RebuildAnyway,
    # The remaining parameters are internal. Script Runner never passes them: the script starts itself
    # again, detached, as the worker that stops, rebuilds, and restarts Automator.
    [switch]$Worker,
    [int]$WaitForProcessId = 0,
    [string]$LogPath,
    [string]$ToolDirectories,
    [switch]$NoDialog
)

$ErrorActionPreference = 'Stop'

# Runs a native command with stderr merged into the returned lines. Windows PowerShell 5.1 raises a
# terminating error for native stderr output under 'Stop', and git and npm write progress there.
function Invoke-Native {
    param([string]$FilePath, [string[]]$Arguments, [string]$AppendTo)
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $lines = New-Object System.Collections.Generic.List[string]
        & $FilePath @Arguments 2>&1 | ForEach-Object {
            $line = "$_"
            $lines.Add($line)
            if ($AppendTo) { Add-Content -LiteralPath $AppendTo -Value $line -Encoding UTF8 }
        }
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previous
    }
    [pscustomobject]@{ ExitCode = $exitCode; Lines = $lines.ToArray() }
}

function Get-RepositoryRoot([string]$folder) {
    # Windows PowerShell's .NET Framework lacks Path.IsPathFullyQualified.
    # Accept drive roots and UNC server/share roots, rejecting C:folder and \folder.
    if ($folder -notmatch '^(?:[A-Za-z]:[\\/]|\\\\[^\\/]+[\\/][^\\/]+(?:[\\/]|$))') { throw 'The repository folder must be a fully qualified absolute path.' }
    # A trailing backslash would escape the closing quote when the path is passed to the worker.
    $root = $folder.TrimEnd('\', '/')
    if (-not (Test-Path -LiteralPath $root -PathType Container)) { throw 'The repository folder does not exist.' }
    foreach ($required in '.git', 'package.json', 'scripts\Restart-Automator.ps1') {
        if (-not (Test-Path -LiteralPath (Join-Path $root $required))) { throw "The repository folder is not an Automator checkout: $required is missing." }
    }
    $root
}

function Find-Tool([string]$name, [string]$purpose) {
    $command = Get-Command $name -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $command) { throw "$name was not found on PATH. It is needed to $purpose." }
    $command.Source
}

function Invoke-Update {
    $root = Get-RepositoryRoot $RepositoryFolder
    $git = Find-Tool 'git.exe' 'pull the changes'
    $npm = Find-Tool 'npm.cmd' 'install dependencies and package the app'
    $node = Find-Tool 'node.exe' 'install the Electron runtime and package the app'
    $dotnet = Find-Tool 'dotnet.exe' 'publish the backend'

    # Never wait for credentials: fail with a message instead.
    $env:GIT_TERMINAL_PROMPT = '0'
    $branch = (Invoke-Native $git @('-C', $root, 'rev-parse', '--abbrev-ref', 'HEAD')).Lines | Select-Object -First 1
    $before = (Invoke-Native $git @('-C', $root, 'rev-parse', 'HEAD')).Lines | Select-Object -First 1
    Write-Output "Pulling the latest changes into $root (branch $branch)..."
    # --no-rebase: a pull.rebase setting would otherwise rebase local commits and refuse any uncommitted file.
    $pull = Invoke-Native $git @('-C', $root, 'pull', '--ff-only', '--no-rebase')
    foreach ($line in $pull.Lines) { Write-Output "  $line" }
    if ($pull.ExitCode -ne 0) {
        throw 'git pull failed, so nothing was rebuilt and Automator was left running. Resolve the problem above (for example local commits that diverge from the remote), then run this again.'
    }

    $after = (Invoke-Native $git @('-C', $root, 'rev-parse', 'HEAD')).Lines | Select-Object -First 1
    if ($before -eq $after) {
        if ($RebuildAnyway -ne 'true') {
            Write-Output 'Already up to date, so nothing was rebuilt and Automator was left running. Turn on "Rebuild even if nothing new was pulled" to rebuild anyway.'
            return
        }
        Write-Output 'Nothing new was pulled; rebuilding anyway.'
    }
    else {
        Write-Output "Updated $($before.Substring(0, 7))..$($after.Substring(0, 7)):"
        $log = Invoke-Native $git @('-C', $root, 'log', '--oneline', '--no-decorate', '-n', '10', "$before..$after")
        foreach ($line in $log.Lines) { Write-Output "  $line" }
    }

    $logDirectory = Join-Path $root 'artifacts\update-and-restart'
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
    Get-ChildItem -LiteralPath $logDirectory -Filter 'update-*.log' -File |
        Sort-Object LastWriteTime -Descending | Select-Object -Skip 9 |
        Remove-Item -Force -ErrorAction SilentlyContinue
    $logPath = Join-Path $logDirectory ('update-{0:yyyyMMdd-HHmmss}.log' -f (Get-Date))
    $toolDirectories = (@($npm, $node, $dotnet) | ForEach-Object { Split-Path -Parent $_ } | Select-Object -Unique) -join ';'

    # Automator stops its own child processes, which include this script and anything it starts normally.
    # Win32_Process.Create starts the worker under WMI instead, outside that process tree and without
    # inheriting this script's redirected output handles.
    $powershell = (Get-Process -Id $PID).Path
    $commandLine = '"{0}" -NoProfile -ExecutionPolicy Bypass -File "{1}" -Worker -RepositoryFolder "{2}" -RebuildAnyway {3} -WaitForProcessId {4} -LogPath "{5}" -ToolDirectories "{6}"' -f
        $powershell, $PSCommandPath, $root, $RebuildAnyway, $PID, $logPath, $toolDirectories
    $startup = New-CimInstance -ClassName Win32_ProcessStartup -ClientOnly -Property @{ ShowWindow = [uint16]0 }
    $started = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{
        CommandLine = $commandLine; CurrentDirectory = $root; ProcessStartupInformation = $startup
    }
    if ($started.ReturnValue -ne 0) { throw "The background update could not be started (Win32_Process.Create returned $($started.ReturnValue))." }

    Write-Output 'Rebuilding in the background. Automator will close in a few seconds and reopen when the build finishes (several minutes).'
    Write-Output "Progress log: $logPath"
}

function Invoke-UpdateWorker {
    $root = Get-RepositoryRoot $RepositoryFolder
    if (-not $LogPath) { throw 'The worker needs a log path.' }
    New-Item -ItemType Directory -Path (Split-Path -Parent $LogPath) -Force | Out-Null
    $powershell = (Get-Process -Id $PID).Path
    $restart = Join-Path $root 'scripts\Restart-Automator.ps1'

    function Write-Log([string]$text) {
        Add-Content -LiteralPath $LogPath -Value ('{0:HH:mm:ss} {1}' -f (Get-Date), $text) -Encoding UTF8
    }
    function Invoke-Step([string]$label, [string]$file, [string[]]$arguments) {
        Write-Log "==> $label"
        $result = Invoke-Native $file $arguments -AppendTo $LogPath
        if ($result.ExitCode -ne 0) { throw "$label failed with exit code $($result.ExitCode)." }
    }

    try {
        Write-Log "Updating Automator in $root"
        # A process started through WMI gets the user's default environment, not this machine's current one.
        if ($ToolDirectories) { $env:PATH = "$ToolDirectories;$env:PATH" }
        if ($WaitForProcessId -gt 0) {
            Wait-Process -Id $WaitForProcessId -Timeout 60 -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 5
        }
        Set-Location -LiteralPath $root

        Invoke-Step 'Stop Automator' $powershell @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $restart, '-StopOnly')

        # Same stamp as scripts\Build-And-Run-Automator.ps1, so either one knows what is installed.
        $modules = Join-Path $root 'node_modules'
        $stampPath = Join-Path $modules '.automator-dependency-stamp'
        $stamp = @(
            (Get-FileHash -LiteralPath (Join-Path $root 'package.json') -Algorithm SHA256).Hash
            (Get-FileHash -LiteralPath (Join-Path $root 'package-lock.json') -Algorithm SHA256).Hash
        ) -join [System.Environment]::NewLine
        $installedStamp = $null
        if (Test-Path -LiteralPath $stampPath -PathType Leaf) { $installedStamp = (Get-Content -LiteralPath $stampPath -Raw).TrimEnd() }
        if (-not (Test-Path -LiteralPath $modules -PathType Container) -or $installedStamp -ne $stamp) {
            Invoke-Step 'Install locked npm dependencies' 'npm.cmd' @('ci')
        }

        # npm can skip Electron's postinstall (newer npm blocks install scripts), which downloads the runtime
        # that packaging needs. Same check as scripts\Build-And-Run-Automator.ps1.
        $electronExecutable = Join-Path $modules 'electron\dist\electron.exe'
        if (-not (Test-Path -LiteralPath $electronExecutable -PathType Leaf)) {
            $electronInstaller = Join-Path $modules 'electron\install.js'
            if (-not (Test-Path -LiteralPath $electronInstaller -PathType Leaf)) { throw "Electron's installer is missing: $electronInstaller. Check the npm dependency installation." }
            Invoke-Step 'Install the Electron Windows runtime' 'node.exe' @($electronInstaller)
            if (-not (Test-Path -LiteralPath $electronExecutable -PathType Leaf)) { throw "Electron's Windows runtime is still missing: $electronExecutable" }
        }
        # Written last, so a failed Electron install makes the next run install dependencies again.
        if ($installedStamp -ne $stamp) { Set-Content -LiteralPath $stampPath -Value $stamp -NoNewline -Encoding ASCII }

        Invoke-Step 'Build the packaged Windows app' 'npm.cmd' @('run', 'package:win')
        $executable = Join-Path $root 'artifacts\electron-dist\win-unpacked\Automator.exe'
        if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) { throw "Packaging finished without $executable." }

        Invoke-Step 'Start Automator' $powershell @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $restart, '-Show')
        Write-Log 'Finished: Automator was rebuilt and restarted.'
    }
    catch {
        $message = $_.Exception.Message
        Write-Log "FAILED: $message"
        # Try to bring the app back. package:win clears the previous package only when packaging itself starts,
        # so a failure before that (dependencies, type check, compile, backend publish) leaves a build to start.
        # A failure while packaging does not, and Automator stays closed.
        $outcome = 'Automator was restarted with the previous build.'
        try { Invoke-Step 'Restart Automator after the failure' $powershell @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $restart, '-Show') }
        catch {
            Write-Log "FAILED: $($_.Exception.Message)"
            $outcome = 'Automator is closed: no intact build is left to start. Fix the problem above, then run this again or run npm.cmd run package:win in the checkout.'
        }
        Write-Log $outcome
        if (-not $NoDialog) {
            try {
                Add-Type -AssemblyName System.Windows.Forms
                [void][System.Windows.Forms.MessageBox]::Show("Updating Automator failed: $message`n`n$outcome`n`nLog: $LogPath", 'Automator update', 'OK', 'Error')
            }
            catch { }
        }
        exit 1
    }
}

try {
    try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false) } catch { }
    if ($Worker) { Invoke-UpdateWorker } else { Invoke-Update }
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
