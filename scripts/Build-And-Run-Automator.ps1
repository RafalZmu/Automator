[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$workspaceRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$nodeModulesDirectory = Join-Path $workspaceRoot 'node_modules'
$dependencyStampPath = Join-Path $nodeModulesDirectory '.automator-dependency-stamp'
$packageManifestPath = Join-Path $workspaceRoot 'package.json'
$packageLockPath = Join-Path $workspaceRoot 'package-lock.json'
$electronExecutable = Join-Path $nodeModulesDirectory 'electron\dist\electron.exe'
$electronInstaller = Join-Path $nodeModulesDirectory 'electron\install.js'
$restartHelper = Join-Path $workspaceRoot 'scripts\Restart-Automator.ps1'

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory = $true)][string]$Command,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [Parameter(Mandatory = $true)][string]$Description
    )

    Write-Host ''
    Write-Host "==> $Description"
    & $Command @Arguments
    $commandExitCode = $LASTEXITCODE
    if ($commandExitCode -ne 0) {
        throw "$Description failed with exit code $commandExitCode."
    }
}

Push-Location $workspaceRoot
try {
    Invoke-CheckedCommand -Command 'powershell.exe' -Arguments @(
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $restartHelper, '-StopOnly'
    ) -Description 'Stop existing workspace Automator processes'

    $dependencyStamp = @(
        (Get-FileHash -LiteralPath $packageManifestPath -Algorithm SHA256).Hash
        (Get-FileHash -LiteralPath $packageLockPath -Algorithm SHA256).Hash
    ) -join [System.Environment]::NewLine

    $installedStamp = if (Test-Path -LiteralPath $dependencyStampPath -PathType Leaf) {
        (Get-Content -LiteralPath $dependencyStampPath -Raw).TrimEnd()
    } else {
        $null
    }

    $dependenciesNeedInstall = -not (Test-Path -LiteralPath $nodeModulesDirectory -PathType Container) -or
        $installedStamp -ne $dependencyStamp

    if ($dependenciesNeedInstall) {
        Invoke-CheckedCommand -Command 'npm.cmd' -Arguments @('ci') -Description 'Install locked npm dependencies'
    }

    if (-not (Test-Path -LiteralPath $electronExecutable -PathType Leaf)) {
        if (-not (Test-Path -LiteralPath $electronInstaller -PathType Leaf)) {
            throw "Electron's installer is missing: $electronInstaller. Check the npm dependency installation."
        }

        Invoke-CheckedCommand -Command 'node.exe' -Arguments @($electronInstaller) -Description 'Install the Electron Windows runtime'
    }

    if (-not (Test-Path -LiteralPath $electronExecutable -PathType Leaf)) {
        throw "Electron's Windows runtime is still missing: $electronExecutable"
    }

    if ($dependenciesNeedInstall) {
        Set-Content -LiteralPath $dependencyStampPath -Value $dependencyStamp -NoNewline -Encoding ASCII
    }

    Invoke-CheckedCommand -Command 'npm.cmd' -Arguments @('run', 'package:win') -Description 'Build the packaged Windows app'

    $stableExecutable = Join-Path $workspaceRoot 'artifacts\electron-dist\win-unpacked\Automator.exe'
    if (-not (Test-Path -LiteralPath $stableExecutable -PathType Leaf)) {
        throw "Packaging completed without the stable app executable: $stableExecutable"
    }

    Invoke-CheckedCommand -Command 'powershell.exe' -Arguments @(
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $restartHelper, '-TestMode', '-Show'
    ) -Description 'Launch the packaged app with isolated test data'

    $package = Get-Content -LiteralPath $packageManifestPath -Raw | ConvertFrom-Json
    $portableExecutable = Join-Path $workspaceRoot "artifacts\electron-dist\Automator-$($package.version)-x64-portable.exe"
    Write-Host ''
    Write-Host 'Packaged Windows app is ready.'
    Write-Host "Unpacked app: $stableExecutable"
    if (Test-Path -LiteralPath $portableExecutable -PathType Leaf) {
        Write-Host "Portable app: $portableExecutable"
    }
} finally {
    Pop-Location
}
