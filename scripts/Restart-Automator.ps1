[CmdletBinding()]
param(
    [string]$ExecutablePath,
    [switch]$Development,
    [switch]$Show,
    [switch]$StopOnly,
    [switch]$TestMode
)

$ErrorActionPreference = 'Stop'
$workspaceRoot = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$workspacePrefix = $workspaceRoot.TrimEnd('\') + '\'
$buildId = $null
$logDirectory = $null
$startArguments = @()
$isLegacyWinUi = $false
$isElectronBuild = $false
$isPortableBuild = $false
$testDataDirectory = $null
$manifestBuildId = $null

function Test-PathInsideWorkspace([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    try {
        $fullPath = [System.IO.Path]::GetFullPath($Path)
        return $fullPath.StartsWith($workspacePrefix, [System.StringComparison]::OrdinalIgnoreCase)
    } catch {
        return $false
    }
}

function Test-AbsoluteExecutablePath([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    if ($Path -notmatch '^[A-Za-z]:\\' -and $Path -notmatch '^\\\\[^\\]+\\[^\\]+\\') { return $false }
    try {
        $fullPath = [System.IO.Path]::GetFullPath($Path)
        return [string]::Equals($fullPath, $Path, [System.StringComparison]::OrdinalIgnoreCase)
    } catch {
        return $false
    }
}

if ($Development -and ($StopOnly -or -not [string]::IsNullOrWhiteSpace($ExecutablePath) -or $TestMode)) {
    throw 'Use either -Development or -ExecutablePath, not both.'
}

if ($Development) {
    $electronExecutable = Join-Path $workspaceRoot 'node_modules\electron\dist\electron.exe'
    $mainEntry = Join-Path $workspaceRoot 'dist-electron\main\main.cjs'
    $preloadEntry = Join-Path $workspaceRoot 'dist-electron\preload\preload.cjs'
    $rendererEntry = Join-Path $workspaceRoot 'dist-electron\renderer\index.html'
    $pngIcon = Join-Path $workspaceRoot 'assets\automator.png'
    $icoIcon = Join-Path $workspaceRoot 'assets\automator.ico'

    foreach ($requiredPath in @($electronExecutable, $mainEntry, $preloadEntry, $rendererEntry, $pngIcon, $icoIcon)) {
        if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
            throw "The development Electron build is incomplete; missing: $requiredPath. Run npm.cmd run build first."
        }
    }

    $ExecutablePath = $electronExecutable
    $startArguments = @((Join-Path 'dist-electron' 'main\main.cjs'))
    $buildId = 'dev-{0}-{1}' -f [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'), ([Guid]::NewGuid().ToString('N').Substring(0, 8))
    $logDirectory = Join-Path $workspaceRoot 'artifacts\test-data\electron-host\Logs'
    Write-Host "Using development Electron host: $electronExecutable"
    Write-Host "Renderer: $rendererEntry"
    Write-Host "Isolated test data: $(Split-Path -Parent $logDirectory)"
} elseif ($StopOnly) {
    Write-Host 'Stop-only mode: stopping workspace Automator processes without starting an application.'
} elseif ([string]::IsNullOrWhiteSpace($ExecutablePath)) {
    $stableDirectory = Join-Path $workspaceRoot 'artifacts\electron-dist\win-unpacked'
    $stableExecutable = Join-Path $stableDirectory 'Automator.exe'
    $manifestPath = Join-Path $stableDirectory 'automator-build.json'
    $asarPath = Join-Path $stableDirectory 'resources\app.asar'
    $backendPath = Join-Path $stableDirectory 'resources\backend\Automator.Backend.exe'
    $browserRuntimePath = Join-Path $stableDirectory 'resources\browser-runtime'
    $browserNodePath = Join-Path $browserRuntimePath 'node.exe'
    $browserWorkerPath = Join-Path $browserRuntimePath 'browser-worker.cjs'
    $playwrightPath = Join-Path $browserRuntimePath 'packages\playwright\package.json'
    $playwrightCorePath = Join-Path $browserRuntimePath 'packages\playwright-core\package.json'
    $validElectronBuild = $false
    if ((Test-Path -LiteralPath $stableExecutable -PathType Leaf) -and
        (Test-Path -LiteralPath $manifestPath -PathType Leaf) -and
        (Test-Path -LiteralPath $asarPath -PathType Leaf) -and
        (Test-Path -LiteralPath $backendPath -PathType Leaf) -and
        (Test-Path -LiteralPath $browserNodePath -PathType Leaf) -and
        (Test-Path -LiteralPath $browserWorkerPath -PathType Leaf) -and
        (Test-Path -LiteralPath $playwrightPath -PathType Leaf) -and
        (Test-Path -LiteralPath $playwrightCorePath -PathType Leaf)) {
        try {
            $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
            $validElectronBuild = ($manifest.formatVersion -eq 1) -and
                ($manifest.productName -eq 'Automator') -and
                ($manifest.executable -eq 'Automator.exe') -and
                ($manifest.applicationArchive -eq 'resources/app.asar') -and
                ($manifest.backendExecutable -eq 'resources/backend/Automator.Backend.exe') -and
                ($manifest.backendRuntime -eq 'win-x64-self-contained') -and
                ($manifest.browserNodeExecutable -eq 'resources/browser-runtime/node.exe') -and
                ($manifest.browserWorkerScript -eq 'resources/browser-runtime/browser-worker.cjs') -and
                ($manifest.playwrightPackage -eq 'resources/browser-runtime/packages/playwright') -and
                ($manifest.playwrightCorePackage -eq 'resources/browser-runtime/packages/playwright-core') -and
                ([int]$manifest.browserRuntimeNodeMajor -ge 20) -and
                (-not [string]::IsNullOrWhiteSpace([string]$manifest.buildId))
            if ($validElectronBuild) { $manifestBuildId = [string]$manifest.buildId }
        } catch { $validElectronBuild = $false }
    }

    if ($validElectronBuild) {
        $ExecutablePath = $stableExecutable
        $isElectronBuild = $true
        Write-Host "Selected the complete Electron build: $stableExecutable"
    } else {
    $searchRoots = @(
        (Join-Path $workspaceRoot 'artifacts'),
        (Join-Path $workspaceRoot 'src\Automator.App\bin\Release')
    )

    $candidates = @(
        foreach ($root in $searchRoots) {
            if (Test-Path -LiteralPath $root) {
                Get-ChildItem -LiteralPath $root -Filter 'Automator.App.exe' -File -Recurse -ErrorAction SilentlyContinue
            }
        }
    )

    $selectedExecutable = $candidates |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.DirectoryName 'Automator.App.pri') } |
        Sort-Object -Property LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $selectedExecutable) {
        throw "Could not find a complete Automator build under artifacts or src\Automator.App\bin\Release in $workspaceRoot. Each WinUI executable must have its Automator.App.pri resource bundle beside it."
    }

    $ExecutablePath = $selectedExecutable.FullName
    $isLegacyWinUi = $true
    }
} else {
    $isLegacyWinUi = [System.IO.Path]::GetFileName($ExecutablePath) -eq 'Automator.App.exe'
    $isElectronBuild = [System.IO.Path]::GetFileName($ExecutablePath) -eq 'Automator.exe'
    $isPortableBuild = [System.IO.Path]::GetFileName($ExecutablePath) -match '^Automator-.+-portable\.exe$'
}

if (-not $StopOnly) {
    $ExecutablePath = [System.IO.Path]::GetFullPath($ExecutablePath)
    if (-not (Test-Path -LiteralPath $ExecutablePath -PathType Leaf)) {
        throw "Automator executable does not exist: $ExecutablePath"
    }
    if (-not (Test-PathInsideWorkspace $ExecutablePath)) {
        throw "Refusing to start an executable outside this workspace: $ExecutablePath"
    }
    if ($isLegacyWinUi -and [System.IO.Path]::GetFileName($ExecutablePath) -ne 'Automator.App.exe') {
        throw "Expected Automator.App.exe for the WinUI rollback, got: $ExecutablePath"
    }
    if ($isElectronBuild) {
        $selectedFolder = Split-Path -Parent $ExecutablePath
        if (-not (Test-Path -LiteralPath (Join-Path $selectedFolder 'resources\app.asar') -PathType Leaf) -or
            -not (Test-Path -LiteralPath (Join-Path $selectedFolder 'resources\backend\Automator.Backend.exe') -PathType Leaf)) {
            throw "The selected Electron build is incomplete: $ExecutablePath"
        }
    }
    if ($isPortableBuild -and -not $TestMode) {
        throw 'Use the stable win-unpacked\Automator.exe path for a production restart. Pass -TestMode to inspect an isolated portable build.'
    }
}

$processSnapshot = @(Get-CimInstance -ClassName Win32_Process -ErrorAction Stop)
$portableHostIds = [System.Collections.Generic.HashSet[uint32]]::new()
$logDirectories = @(
    (Join-Path $env:LOCALAPPDATA 'Automator\Logs'),
    (Join-Path $workspaceRoot 'artifacts\test-data\electron-host\Logs')
)
foreach ($portableLogDirectory in $logDirectories) {
if (Test-Path -LiteralPath $portableLogDirectory -PathType Container) {
    $recentLogFiles = Get-ChildItem -LiteralPath $portableLogDirectory -Filter 'automator-*.jsonl' -File |
        Sort-Object -Property LastWriteTime -Descending |
        Select-Object -First 3
    foreach ($logFile in $recentLogFiles) {
        foreach ($line in (Get-Content -LiteralPath $logFile.FullName -Tail 500 -ErrorAction SilentlyContinue)) {
            try { $event = $line | ConvertFrom-Json } catch { continue }
            if ($event.Event -ne 'App.Starting' -or [string]::IsNullOrWhiteSpace([string]$event.portableExecutablePath)) { continue }
            $portablePath = [string]$event.portableExecutablePath
            if (-not (Test-PathInsideWorkspace $portablePath) -or -not (Test-Path -LiteralPath $portablePath -PathType Leaf)) { continue }
            $loggedExe = [string]$event.executable
            if (-not (Test-AbsoluteExecutablePath $loggedExe)) { continue }
            $matchingProcess = $processSnapshot | Where-Object { [uint32]$_.ProcessId -eq [uint32]$event.ProcessId } | Select-Object -First 1
            if ($null -eq $matchingProcess -or [string]$matchingProcess.Name -ne 'Automator.exe') { continue }
            if (-not [string]::Equals([string]$matchingProcess.ExecutablePath, $loggedExe, [System.StringComparison]::OrdinalIgnoreCase)) { continue }
            try {
                $started = (Get-Process -Id ([int]$event.ProcessId) -ErrorAction Stop).StartTime.ToUniversalTime()
                $loggedAt = [DateTime]::Parse([string]$event.Timestamp).ToUniversalTime()
                if ($started -ge $loggedAt.AddSeconds(-10)) { [void]$portableHostIds.Add([uint32]$event.ProcessId) }
            } catch { continue }
        }
    }
}
}
$rootProcesses = @(
    foreach ($candidate in $processSnapshot) {
        $name = [string]$candidate.Name
        $path = [string]$candidate.ExecutablePath
        $commandLine = [string]$candidate.CommandLine
        $verifiedWorkspacePath = Test-PathInsideWorkspace $path

        $isOldHost = $verifiedWorkspacePath -and $name -in @('Automator.App.exe', 'Automator.exe', 'Automator.Backend.exe')
        $isTrackedPortableHost = $name -eq 'Automator.exe' -and $portableHostIds.Contains([uint32]$candidate.ProcessId)
        $mainScriptRelative = Join-Path 'dist-electron' 'main\main.cjs'
        $mainScriptAbsolute = Join-Path $workspaceRoot $mainScriptRelative
        $isDevelopmentElectron =
            $name -eq 'electron.exe' -and
            (Test-PathInsideWorkspace $path) -and
            [string]::Equals($path, (Join-Path $workspaceRoot 'node_modules\electron\dist\electron.exe'), [System.StringComparison]::OrdinalIgnoreCase) -and
            ($commandLine.IndexOf($mainScriptAbsolute, [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
             $commandLine.IndexOf($mainScriptRelative, [System.StringComparison]::OrdinalIgnoreCase) -ge 0)
        $isDevelopmentController = $name -eq 'node.exe' -and
            $commandLine.IndexOf($workspacePrefix, [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and
            $commandLine.IndexOf((Join-Path $workspaceRoot 'scripts\dev.mjs'), [System.StringComparison]::OrdinalIgnoreCase) -ge 0
        $isDotNetBackend = $name -eq 'dotnet.exe' -and
            $commandLine.IndexOf((Join-Path $workspaceRoot 'artifacts\backend\'), [System.StringComparison]::OrdinalIgnoreCase) -ge 0

        if ($isOldHost -or $isTrackedPortableHost -or $isDevelopmentElectron -or $isDevelopmentController -or $isDotNetBackend) {
            $candidate
        }
    }
)

$processIds = [System.Collections.Generic.HashSet[uint32]]::new()
$processOrder = [System.Collections.Generic.List[uint32]]::new()
$queue = [System.Collections.Generic.Queue[uint32]]::new()
foreach ($rootProcess in $rootProcesses) {
    $id = [uint32]$rootProcess.ProcessId
    if ($processIds.Add($id)) { $queue.Enqueue($id) }
}

while ($queue.Count -gt 0) {
    $parentId = $queue.Dequeue()
    $processOrder.Add($parentId)
    foreach ($child in $processSnapshot) {
        $childId = [uint32]$child.ProcessId
        if ([uint32]$child.ParentProcessId -eq $parentId -and $processIds.Add($childId)) {
            $queue.Enqueue($childId)
        }
    }
}

for ($index = $processOrder.Count - 1; $index -ge 0; $index--) {
    $processId = [int]$processOrder[$index]
    $processInfo = $processSnapshot | Where-Object { [int]$_.ProcessId -eq $processId } | Select-Object -First 1
    $processPath = if ($null -ne $processInfo) { [string]$processInfo.ExecutablePath } else { $null }
    Write-Host "Stopping Automator process tree PID $processId$(if ($processPath) { ": $processPath" })"
    Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
}

$stopDeadline = (Get-Date).AddSeconds(10)
do {
    $remaining = @(
        foreach ($processId in $processOrder) {
            Get-Process -Id ([int]$processId) -ErrorAction SilentlyContinue
        }
    )
    if ($remaining.Count -eq 0) { break }
    Start-Sleep -Milliseconds 250
} while ((Get-Date) -lt $stopDeadline)

if ($remaining.Count -gt 0) {
    $ids = ($remaining | ForEach-Object Id) -join ', '
    throw "Automator processes did not exit: $ids"
}

if ($StopOnly) {
    Write-Host 'Workspace Automator processes have stopped; no application was started.'
    return
}

$workingDirectory = if ($Development) { $workspaceRoot } else { Split-Path -Parent $ExecutablePath }
$previousEnvironment = @{}
$startedProcess = $null
$environmentNames = @('AUTOMATOR_TEST_MODE', 'AUTOMATOR_TEST_TRAY', 'AUTOMATOR_TEST_DATA_DIRECTORY', 'AUTOMATOR_SHOW_ON_START', 'AUTOMATOR_BUILD_ID', 'AUTOMATOR_RENDERER_URL', 'PORTABLE_EXECUTABLE_FILE')
if ($Development) {
    foreach ($name in $environmentNames) {
        $previousEnvironment[$name] = [System.Environment]::GetEnvironmentVariable($name, 'Process')
    }

    [System.Environment]::SetEnvironmentVariable('AUTOMATOR_TEST_MODE', '1', 'Process')
    [System.Environment]::SetEnvironmentVariable('AUTOMATOR_TEST_TRAY', '1', 'Process')
    [System.Environment]::SetEnvironmentVariable('AUTOMATOR_SHOW_ON_START', $(if ($Show) { '1' } else { '0' }), 'Process')
    [System.Environment]::SetEnvironmentVariable('AUTOMATOR_BUILD_ID', $buildId, 'Process')
    [System.Environment]::SetEnvironmentVariable('AUTOMATOR_RENDERER_URL', $null, 'Process')
    [System.Environment]::SetEnvironmentVariable('AUTOMATOR_TEST_DATA_DIRECTORY', $null, 'Process')
    [System.Environment]::SetEnvironmentVariable('PORTABLE_EXECUTABLE_FILE', $null, 'Process')
    try {
        $startedProcess = Start-Process -FilePath $ExecutablePath -ArgumentList $startArguments -WorkingDirectory $workingDirectory -PassThru
    } finally {
        foreach ($name in $previousEnvironment.Keys) {
            [System.Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process')
        }
    }
} elseif ($isElectronBuild -or $isPortableBuild) {
    if ($TestMode -and -not ($isElectronBuild -or $isPortableBuild)) { throw '-TestMode is available only for Electron builds.' }
    $manifestIdPart = if ([string]::IsNullOrWhiteSpace($manifestBuildId)) { 'electron' } else { $manifestBuildId }
    $buildIdPrefix = if ($TestMode) { 'test-host' } else { 'host' }
    $buildId = '{0}-{1}-{2}-{3}' -f $buildIdPrefix, $manifestIdPart, [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'), ([Guid]::NewGuid().ToString('N').Substring(0, 8))
    if ($TestMode) {
        $testDataDirectory = Join-Path $workspaceRoot 'artifacts\test-data\electron-host'
        $logDirectory = Join-Path $testDataDirectory 'Logs'
        Write-Host "Using isolated package data: $testDataDirectory"
    } else {
        $logDirectory = Join-Path $env:LOCALAPPDATA 'Automator\Logs'
    }
    foreach ($name in $environmentNames) {
        $previousEnvironment[$name] = [System.Environment]::GetEnvironmentVariable($name, 'Process')
    }
    [System.Environment]::SetEnvironmentVariable('AUTOMATOR_TEST_MODE', $(if ($TestMode) { '1' } else { $null }), 'Process')
    [System.Environment]::SetEnvironmentVariable('AUTOMATOR_TEST_TRAY', $(if ($TestMode) { '1' } else { $null }), 'Process')
    [System.Environment]::SetEnvironmentVariable('AUTOMATOR_TEST_DATA_DIRECTORY', $testDataDirectory, 'Process')
    [System.Environment]::SetEnvironmentVariable('AUTOMATOR_RENDERER_URL', $null, 'Process')
    [System.Environment]::SetEnvironmentVariable('PORTABLE_EXECUTABLE_FILE', $null, 'Process')
    [System.Environment]::SetEnvironmentVariable('AUTOMATOR_BUILD_ID', $buildId, 'Process')
    [System.Environment]::SetEnvironmentVariable('AUTOMATOR_SHOW_ON_START', $(if ($Show) { '1' } else { $null }), 'Process')
    try {
        $startedProcess = Start-Process -FilePath $ExecutablePath -WorkingDirectory $workingDirectory -PassThru
    } finally {
        foreach ($name in $previousEnvironment.Keys) {
            [System.Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process')
        }
    }
} else {
    if ($TestMode) { throw '-TestMode is available only for Electron builds.' }
    $startedProcess = Start-Process -FilePath $ExecutablePath -ArgumentList $startArguments -WorkingDirectory $workingDirectory -PassThru
    $logDirectory = Join-Path $env:LOCALAPPDATA 'Automator\Logs'
}

$logDate = [DateTime]::UtcNow.ToString('yyyy-MM-dd')
$logPath = Join-Path $logDirectory "automator-$logDate.jsonl"
$startupDeadline = (Get-Date).AddSeconds(30)
$readyEvent = $null

do {
    $startingEvent = $null
    if (Test-Path -LiteralPath $logPath) {
        $events = Get-Content -LiteralPath $logPath -Tail 240 |
            ForEach-Object { try { $_ | ConvertFrom-Json } catch { } }
        if ($isPortableBuild -and $buildId) {
            $startingEvent = $events |
                Where-Object { $_.Event -eq 'App.Starting' -and $_.BuildId -eq $buildId -and $_.portableExecutablePath -eq $ExecutablePath } |
                Select-Object -Last 1
        }
        $hostProcessId = if ($isPortableBuild -and $null -ne $startingEvent) { [int]$startingEvent.ProcessId } else { $startedProcess.Id }
        $readyEvent = $events |
            Where-Object {
                $_.Event -eq 'App.Ready' -and
                $_.ProcessId -eq $hostProcessId -and
                ([string]::IsNullOrWhiteSpace($buildId) -or $_.BuildId -eq $buildId)
            } |
            Select-Object -Last 1
    }

    if ($null -ne $readyEvent) { break }
    $watchedProcessId = if ($isPortableBuild -and $null -ne $startingEvent) { [int]$startingEvent.ProcessId } else { $startedProcess.Id }
    $runningProcess = Get-Process -Id $watchedProcessId -ErrorAction SilentlyContinue
    $launcherStillRunning = Get-Process -Id $startedProcess.Id -ErrorAction SilentlyContinue
    if ($null -eq $runningProcess -and $null -eq $launcherStillRunning) {
        throw "Automator exited during startup. Inspect its log: $logPath"
    }
    Start-Sleep -Milliseconds 500
} while ((Get-Date) -lt $startupDeadline)

if ($null -eq $readyEvent) {
    throw "Automator host PID $($startedProcess.Id) build '$buildId' is running but did not log a fresh App.Ready within 30 seconds. Inspect: $logPath"
}

if ($Show -and ($Development -or $isElectronBuild -or $isPortableBuild) -and -not $readyEvent.visible) {
    throw "Automator logged App.Ready for PID $($readyEvent.ProcessId) build '$buildId' but its launcher panel is not visible. Inspect: $logPath"
}

Write-Host "Automator is ready (host PID $($readyEvent.ProcessId), launcher PID $($startedProcess.Id)$(if ($buildId) { ", build $buildId" }))."
Write-Host "Executable: $ExecutablePath"
Write-Host "Startup log: $logPath"
if ($Development) { Write-Host 'Development settings and logs are isolated under artifacts\test-data.' }
if ($TestMode) { Write-Host 'Test mode disabled startup registration and used isolated settings and logs.' }
