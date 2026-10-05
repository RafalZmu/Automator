param(
    [Parameter(Mandatory = $true, Position = 0)][string]$Database,
    [Parameter(Mandatory = $true, Position = 1)][string]$Backup,
    [Parameter(Mandatory = $true, Position = 2)][string]$Archive,
    [Parameter(Mandatory = $true, Position = 3)][string]$Gbak,
    [Parameter(Mandatory = $true, Position = 4)][string]$Username,
    [Parameter(Mandatory = $true, Position = 5)][string]$Password
)

$ErrorActionPreference = 'Stop'
$backupReserved = $false
$archiveReserved = $false
$complete = $false
try {
    foreach ($path in @($Database, $Backup, $Archive, $Gbak)) {
        if (-not [System.IO.Path]::IsPathRooted($path)) { throw 'All paths must be absolute.' }
    }
    if (-not (Test-Path -LiteralPath $Database -PathType Leaf)) { throw 'Database file does not exist.' }
    if (-not (Test-Path -LiteralPath $Gbak -PathType Leaf)) { throw 'Firebird gbak executable does not exist.' }
    if ([System.IO.Path]::GetExtension($Database) -ine '.fdb') { throw 'Database file must use the .fdb extension.' }
    if ([System.IO.Path]::GetExtension($Archive) -ine '.zip') { throw 'Archive file must use the .zip extension.' }
    foreach ($path in @($Backup, $Archive)) {
        if (-not (Test-Path -LiteralPath ([System.IO.Path]::GetDirectoryName($path)) -PathType Container)) { throw 'Output directory does not exist.' }
        if (Test-Path -LiteralPath $path) { throw 'Output already exists. Choose a new output path.' }
    }
    # Reserve both output names exclusively before creating any data.
    $reservation = [System.IO.File]::Open($Backup, [System.IO.FileMode]::CreateNew)
    $reservation.Dispose()
    $backupReserved = $true
    $reservation = [System.IO.File]::Open($Archive, [System.IO.FileMode]::CreateNew)
    $reservation.Dispose()
    $archiveReserved = $true
    # Suppress tool output so authentication values never enter captured diagnostics.
    & $Gbak '-b' '-user' $Username '-password' $Password $Database $Backup *> $null
    if ($LASTEXITCODE -ne 0) { throw 'Firebird backup failed. Check the database, credentials, and gbak version.' }
    if ((Get-Item -LiteralPath $Backup).Length -eq 0) { throw 'Firebird produced an empty backup.' }
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $stream = [System.IO.File]::Open($Archive, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    try {
        $zip = New-Object System.IO.Compression.ZipArchive($stream, [System.IO.Compression.ZipArchiveMode]::Create, $true)
        try { [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $Backup, [System.IO.Path]::GetFileName($Backup)) | Out-Null }
        finally { $zip.Dispose() }
    }
    finally { $stream.Dispose() }
    $complete = $true
    Write-Output 'Firebird backup and ZIP completed.'
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
finally {
    $Password = $null
    if (-not $complete) {
        if ($archiveReserved) { Remove-Item -LiteralPath $Archive -ErrorAction SilentlyContinue }
        if ($backupReserved) { Remove-Item -LiteralPath $Backup -ErrorAction SilentlyContinue }
    }
}
