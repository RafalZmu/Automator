$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

$fixtureRoot = Join-Path $PSScriptRoot ('..\artifacts\test-data\fdb-stale-archive-' + [Guid]::NewGuid().ToString('N'))
$source = Join-Path $fixtureRoot 'source folder'
$assetPath = Join-Path $PSScriptRoot '..\src\Automator.Application\Automation\Templates\fdb-stale-archive.v1.ps1'
$lock = $null

# Runs the asset the way Script Runner does: one positional argument per declared parameter.
function Invoke-Asset {
    param([string]$Folder, [string]$Days = '90', [string]$Subfolders = 'true', [string]$Preview = 'false')
    $ErrorActionPreference = 'Continue'
    $text = & powershell.exe -NoProfile -NonInteractive -File $assetPath $Folder $Days $Subfolders $Preview 2>&1 | Out-String
    [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $text }
}

function Get-ZipEntryHash([string]$zipPath) {
    $archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        if ($archive.Entries.Count -ne 1) { throw "$zipPath holds $($archive.Entries.Count) entries; expected one" }
        $sha = [System.Security.Cryptography.SHA256]::Create()
        $entryStream = $archive.Entries[0].Open()
        try {
            return [pscustomobject]@{ Name = $archive.Entries[0].Name; Hash = [BitConverter]::ToString($sha.ComputeHash($entryStream)).Replace('-', '') }
        }
        finally {
            $entryStream.Dispose()
            $sha.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }
}

# Uses lengths and existence only, so the check does not read files and reset their access times.
function Assert-Unchanged([string]$label, $files) {
    foreach ($relative in $files.Keys) {
        $path = Join-Path $source $relative
        if (-not (Test-Path -LiteralPath $path) -or (Get-Item -LiteralPath $path).Length -ne $files[$relative].Length) { throw "$label changed $relative" }
    }
    $zips = @(Get-ChildItem -LiteralPath $source -Recurse -Filter *.zip | ForEach-Object Name)
    if (($zips -join ',') -ne 'collide.fdb.zip') { throw "$label created archives: $($zips -join ', ')" }
}

try {
    New-Item -ItemType Directory -Path (Join-Path $source 'sub') -Force | Out-Null
    $files = [ordered]@{
        'stale one.fdb'     = 'stale one'
        'sub\stale two.FDB' = 'stale two'
        'sub\recent.fdb'    = 'recent'
        'collide.fdb'       = 'collide'
        'locked.fdb'        = 'locked'
        'readme.txt'        = 'not a database'
    }
    foreach ($relative in $files.Keys) {
        Set-Content -LiteralPath (Join-Path $source $relative) -Value $files[$relative] -NoNewline
    }
    Set-Content -LiteralPath (Join-Path $source 'collide.fdb.zip') -Value 'old archive' -NoNewline

    # Hash before the access times are set: hashing reads the files and would reset them.
    $hashes = @{}
    foreach ($relative in $files.Keys) {
        $hashes[$relative] = (Get-FileHash -LiteralPath (Join-Path $source $relative) -Algorithm SHA256).Hash
    }
    $old = (Get-Date).AddDays(-200)
    foreach ($relative in 'stale one.fdb', 'sub\stale two.FDB', 'collide.fdb', 'locked.fdb') {
        [IO.File]::SetLastAccessTime((Join-Path $source $relative), $old)
    }

    $result = Invoke-Asset -Folder 'relative\folder'
    if ($result.ExitCode -eq 0 -or $result.Output -notlike '*fully qualified*') { throw 'Relative source folder was accepted' }
    $result = Invoke-Asset -Folder $source -Days '0'
    if ($result.ExitCode -eq 0 -or $result.Output -notlike '*whole number*') { throw 'Zero days was accepted' }
    Assert-Unchanged 'invalid input' $files

    $result = Invoke-Asset -Folder $source -Subfolders 'false' -Preview 'true'
    if ($result.ExitCode -ne 0 -or $result.Output -notlike '*Preview only*') { throw 'Preview run failed' }
    if ($result.Output -notlike '*stale one.fdb*') { throw 'Preview did not list the top-level stale file' }
    if ($result.Output -like '*stale two*') { throw 'Preview without subfolders listed a nested file' }
    Assert-Unchanged 'preview' $files

    # Hold locked.fdb open so the real run must skip it.
    $lock = [IO.File]::Open((Join-Path $source 'locked.fdb'), [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try {
        $result = Invoke-Asset -Folder $source -Preview 'false'
    }
    finally {
        $lock.Dispose()
        $lock = $null
    }
    if ($result.ExitCode -ne 0) { throw "Real run failed: $($result.Output)" }
    if ($result.Output -notlike '*Skipped*locked.fdb*') { throw 'Locked file was not reported as skipped' }
    if ($result.Output -notlike '*collide.fdb.zip already exists; using collide.fdb.*') { throw 'Existing archive name was not avoided' }
    if ($result.Output -notlike '*Done: 3 archived, 1 skipped.*') { throw "Unexpected totals: $($result.Output)" }

    if (Test-Path -LiteralPath (Join-Path $source 'stale one.fdb')) { throw 'Original stale file was not deleted' }
    $oneZip = Get-ZipEntryHash (Join-Path $source 'stale one.fdb.zip')
    if ($oneZip.Name -ne 'stale one.fdb' -or $oneZip.Hash -ne $hashes['stale one.fdb']) { throw 'stale one.fdb.zip does not match the original' }

    if (Test-Path -LiteralPath (Join-Path $source 'sub\stale two.FDB')) { throw 'Nested original was not deleted' }
    $twoZip = Get-ZipEntryHash (Join-Path $source 'sub\stale two.FDB.zip')
    if ($twoZip.Name -ne 'stale two.FDB' -or $twoZip.Hash -ne $hashes['sub\stale two.FDB']) { throw 'Nested archive does not match the original' }

    if (Test-Path -LiteralPath (Join-Path $source 'collide.fdb')) { throw 'Colliding original was not deleted' }
    if ((Get-Content -LiteralPath (Join-Path $source 'collide.fdb.zip') -Raw) -ne 'old archive') { throw 'Existing archive was overwritten' }
    $timestamped = @(Get-ChildItem -LiteralPath $source -Filter 'collide.fdb.*.zip')
    if ($timestamped.Count -ne 1) { throw 'Timestamped archive for collide.fdb is missing' }
    $collideZip = Get-ZipEntryHash $timestamped[0].FullName
    if ($collideZip.Name -ne 'collide.fdb' -or $collideZip.Hash -ne $hashes['collide.fdb']) { throw 'Timestamped archive does not match the original' }

    if ((Get-Item -LiteralPath (Join-Path $source 'sub\recent.fdb')).Length -ne $files['sub\recent.fdb'].Length) { throw 'Recent file changed' }
    if ((Get-Item -LiteralPath (Join-Path $source 'locked.fdb')).Length -ne $files['locked.fdb'].Length) { throw 'Locked file changed' }
    if (Test-Path -LiteralPath (Join-Path $source 'locked.fdb.zip')) { throw 'Locked file produced an archive' }
    if (@(Get-ChildItem -LiteralPath $source -Recurse -Filter *.partial).Count -ne 0) { throw 'Partial archive left behind' }
    if (-not (Test-Path -LiteralPath (Join-Path $source 'readme.txt'))) { throw 'Non-database file was removed' }

    Write-Output 'FDB stale archive asset: input checks, preview, archive contents, name collision, and locked-file skip passed.'
}
finally {
    if ($lock) { $lock.Dispose() }
    if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force }
}
