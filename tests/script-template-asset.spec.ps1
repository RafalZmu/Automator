$ErrorActionPreference = 'Stop'
$fixtureRoot = Join-Path $PSScriptRoot ('..\artifacts\test-data\template-asset-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
$databasePath = Join-Path $fixtureRoot 'live database.fdb'
$backupPath = Join-Path $fixtureRoot 'backup file.fbk'
$archivePath = Join-Path $fixtureRoot 'archive file.zip'
$toolPath = Join-Path $fixtureRoot 'mock gbak.cmd'
$assetPath = Join-Path $PSScriptRoot '..\src\Automator.Application\Automation\Templates\firebird-3-backup-zip.v1.ps1'
try {
    Set-Content -LiteralPath $databasePath -Value 'database fixture'
    Set-Content -LiteralPath $toolPath -Value @('@echo off', 'echo backup-data>"%~7"', 'exit /b 0')
    & powershell.exe -NoProfile -NonInteractive -File $assetPath $databasePath $backupPath $archivePath $toolPath 'fixture-user' 'fixture-password'
    if ($LASTEXITCODE -ne 0) { throw 'Success fixture failed' }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($archivePath)
    try {
        if ($archive.Entries.Count -ne 1 -or $archive.Entries[0].Name -ne 'backup file.fbk') { throw 'Invalid ZIP output' }
    }
    finally { $archive.Dispose() }
    $backupBefore = [System.IO.File]::ReadAllBytes($backupPath)
    $archiveBefore = [System.IO.File]::ReadAllBytes($archivePath)
    & powershell.exe -NoProfile -NonInteractive -File $assetPath $databasePath $backupPath $archivePath $toolPath 'fixture-user' 'fixture-password'
    if ($LASTEXITCODE -eq 0 -or [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($backupPath)) -ne [Convert]::ToBase64String($backupBefore) -or
        [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($archivePath)) -ne [Convert]::ToBase64String($archiveBefore)) { throw 'Existing output check failed' }
    Remove-Item -LiteralPath $backupPath, $archivePath
    Set-Content -LiteralPath $toolPath -Value @('@echo off', 'exit /b 7')
    & powershell.exe -NoProfile -NonInteractive -File $assetPath $databasePath $backupPath $archivePath $toolPath 'fixture-user' 'fixture-password'
    if ($LASTEXITCODE -eq 0 -or (Test-Path -LiteralPath $archivePath) -or (Test-Path -LiteralPath $backupPath)) { throw 'Backup failure cleanup failed' }
    Write-Output 'Asset fixtures: ZIP success, existing-output refusal, and gbak-failure cleanup passed.'
}
finally {
    foreach ($path in @($databasePath, $backupPath, $archivePath, $toolPath)) {
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
    }
    Remove-Item -LiteralPath $fixtureRoot
}
