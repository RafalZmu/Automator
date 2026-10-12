$ErrorActionPreference = 'Stop'
$fixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('Automator-backup-asset-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixtureRoot | Out-Null
$databasePath = Join-Path $fixtureRoot 'live database.fdb'
$backupPath = Join-Path $fixtureRoot 'backup file.fbk'
$toolPath = Join-Path $fixtureRoot 'mock gbak.cmd'
$assetPath = Join-Path $PSScriptRoot '..\src\Automator.Application\Automation\Templates\firebird-3-backup.v1.ps1'
try {
    Set-Content -LiteralPath $databasePath -Value 'database fixture'
    Set-Content -LiteralPath $toolPath -Value @('@echo off', 'echo backup-data>"%~7"', 'exit /b 0')
    & powershell.exe -NoProfile -NonInteractive -File $assetPath $databasePath $backupPath $toolPath 'fixture-user' 'fixture-secret'
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $backupPath)) { throw 'Backup success failed' }
    $before = [System.IO.File]::ReadAllText($backupPath)
    & powershell.exe -NoProfile -NonInteractive -File $assetPath $databasePath $backupPath $toolPath 'fixture-user' 'fixture-secret'
    if ($LASTEXITCODE -eq 0 -or [System.IO.File]::ReadAllText($backupPath) -ne $before) { throw 'Existing backup overwritten' }
    Remove-Item -LiteralPath $backupPath
    Set-Content -LiteralPath $toolPath -Value @('@echo off', 'exit /b 7')
    & powershell.exe -NoProfile -NonInteractive -File $assetPath $databasePath $backupPath $toolPath 'fixture-user' 'fixture-secret'
    if ($LASTEXITCODE -eq 0 -or (Test-Path -LiteralPath $backupPath)) { throw 'Failure cleanup failed' }
    Write-Output 'Plain backup asset: success, overwrite refusal, and failure cleanup passed.'
}
finally {
    foreach ($path in @($databasePath, $backupPath, $toolPath)) { if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path } }
    Remove-Item -LiteralPath $fixtureRoot
}
