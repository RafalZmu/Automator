<#
.SYNOPSIS
Pulls the latest changes, rebuilds the packaged app, and restarts it on your normal (production) data.

.DESCRIPTION
Runs the "Update Automator and restart" script (src\Automator.Application\Automation\Templates\update-and-restart.v1.ps1,
also offered in the Script Runner Library) against this checkout, straight from the repository, so it needs no
installed copy or Script Runner profile.

It runs `git pull --ff-only --no-rebase` first. If that fails, nothing is rebuilt and Automator keeps running. If it
succeeds, a background worker closes Automator, installs dependencies when they changed, builds the package, and
starts it with `scripts\Restart-Automator.ps1 -Show`, which uses the data in %LOCALAPPDATA%\Automator. Unlike
Build-And-Run-Automator.ps1 this never starts the app in test mode.

.PARAMETER RebuildAnyway
Rebuild and restart even when the pull brought nothing new.

.PARAMETER Checkout
The Automator checkout to update. Defaults to the checkout this script is in.

.EXAMPLE
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Update-Automator.ps1
#>
[CmdletBinding()]
param(
    [switch]$RebuildAnyway,
    [string]$Checkout
)

$ErrorActionPreference = 'Stop'
try {
    $root = if ($Checkout) { $Checkout } else { Split-Path -Parent $PSScriptRoot }
    $root = [System.IO.Path]::GetFullPath($root).TrimEnd('\')
    $templates = Join-Path $root 'src\Automator.Application\Automation\Templates'
    # The highest template version wins, so a future v2 is picked up without editing this file.
    $template = Get-ChildItem -LiteralPath $templates -Filter 'update-and-restart.v*.ps1' -File -ErrorAction Stop |
        Sort-Object Name -Descending | Select-Object -First 1
    if (-not $template) { throw "The update script is missing from $templates." }

    # Windows PowerShell 5.1 runs the script, as the Script Runner Library does and AGENTS.md asks for.
    $powershell = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $rebuild = if ($RebuildAnyway) { 'true' } else { 'false' }
    & $powershell -NoProfile -ExecutionPolicy Bypass -File $template.FullName $root $rebuild
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "Production data: $(Join-Path $env:LOCALAPPDATA 'Automator')"
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
