$ErrorActionPreference = 'Stop'
$fixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('Automator-restore-asset-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixtureRoot | Out-Null
$databasePath = Join-Path $fixtureRoot 'live database.fdb'
$backupPath = Join-Path $fixtureRoot 'backup file.fbk'
$toolPath = Join-Path $fixtureRoot 'mock gbak.cmd'
$assetPath = Join-Path $PSScriptRoot '..\src\Automator.Application\Automation\Templates\firebird-3-restore.v1.ps1'
function Invoke-Restore([string]$tool, [string]$replace) {
    & powershell.exe -NoProfile -NonInteractive -File $assetPath $databasePath $backupPath $tool 'fixture-user' 'fixture-secret' $replace
    return $LASTEXITCODE
}
try {
    Set-Content -LiteralPath $backupPath -Value 'backup fixture'
    # Mock gbak: -c -user U -password P <backup> <database>; the database is argument 7.
    Set-Content -LiteralPath $toolPath -Value @('@echo off', 'echo restored-data>"%~7"', 'exit /b 0')
    if ((Invoke-Restore $toolPath 'false') -ne 0 -or -not (Test-Path -LiteralPath $databasePath)) { throw 'Restore success failed' }
    $before = [System.IO.File]::ReadAllText($databasePath)
    if ((Invoke-Restore $toolPath 'false') -eq 0 -or [System.IO.File]::ReadAllText($databasePath) -ne $before) { throw 'Existing database overwritten' }
    Set-Content -LiteralPath $toolPath -Value @('@echo off', 'exit /b 7')
    if ((Invoke-Restore $toolPath 'true') -eq 0 -or [System.IO.File]::ReadAllText($databasePath) -ne $before) { throw 'Failed replace damaged the existing database' }
    Set-Content -LiteralPath $toolPath -Value @('@echo off', 'echo replaced-data>"%~7"', 'exit /b 0')
    if ((Invoke-Restore $toolPath 'true') -ne 0 -or [System.IO.File]::ReadAllText($databasePath) -eq $before) { throw 'Replace failed' }
    # A right-clicked .fbk is passed as both database and (defaulted) backup: it restores to the .fdb beside it.
    $upperBackup = Join-Path $fixtureRoot 'right click.FBK'
    $derivedDatabase = Join-Path $fixtureRoot 'right click.fdb'
    Set-Content -LiteralPath $upperBackup -Value 'backup fixture'
    & powershell.exe -NoProfile -NonInteractive -File $assetPath $upperBackup $upperBackup $toolPath 'fixture-user' 'fixture-secret' 'false'
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $derivedDatabase)) { throw 'Right-clicked .fbk was not restored' }
    & powershell.exe -NoProfile -NonInteractive -File $assetPath $upperBackup $upperBackup $toolPath 'fixture-user' 'fixture-secret' 'false'
    if ($LASTEXITCODE -eq 0) { throw 'Right-clicked .fbk overwrote an existing .fdb without replace' }
    Remove-Item -LiteralPath $upperBackup, $derivedDatabase
    Remove-Item -LiteralPath $backupPath
    if ((Invoke-Restore $toolPath 'false') -eq 0) { throw 'Missing backup accepted' }
    if (@(Get-ChildItem -LiteralPath $fixtureRoot -Filter '*.restoring-*.tmp').Count -ne 0) { throw 'Temporary restore file left behind' }
    Write-Output 'Restore asset: success, overwrite refusal, replace, failure safety, and missing backup passed.'
}
finally {
    foreach ($path in @($databasePath, $backupPath, $toolPath)) { if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path } }
    Remove-Item -LiteralPath $fixtureRoot
}
