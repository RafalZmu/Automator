param(
    [Parameter(Mandatory = $true, Position = 0)][string]$Database,
    [Parameter(Mandatory = $true, Position = 1)][string]$Backup,
    [Parameter(Mandatory = $true, Position = 2)][string]$Gbak,
    [Parameter(Mandatory = $true, Position = 3)][string]$Username,
    [Parameter(Mandatory = $true, Position = 4)][string]$Password
)
$ErrorActionPreference = 'Stop'
$backupReserved = $false
$complete = $false
try {
    foreach ($path in @($Database, $Backup, $Gbak)) {
        if ($path -notmatch '^(?:[A-Za-z]:[\\/]|\\\\[^\\/]+[\\/][^\\/]+(?:[\\/]|$))') { throw 'All paths must be fully qualified absolute paths.' }
    }
    if (-not (Test-Path -LiteralPath $Database -PathType Leaf)) { throw 'Database file does not exist.' }
    if (-not (Test-Path -LiteralPath $Gbak -PathType Leaf)) { throw 'Firebird gbak executable does not exist.' }
    if ([System.IO.Path]::GetExtension($Database) -ine '.fdb') { throw 'Database file must use the .fdb extension.' }
    if (-not (Test-Path -LiteralPath ([System.IO.Path]::GetDirectoryName($Backup)) -PathType Container)) { throw 'Output directory does not exist.' }
    if (Test-Path -LiteralPath $Backup) { throw 'Output already exists. Choose a new output path.' }
    $reservation = [System.IO.File]::Open($Backup, [System.IO.FileMode]::CreateNew)
    $reservation.Dispose()
    $backupReserved = $true
    & $Gbak '-b' '-user' $Username '-password' $Password $Database $Backup *> $null
    if ($LASTEXITCODE -ne 0) { throw 'Firebird backup failed. Check the database, credentials, and gbak version.' }
    if ((Get-Item -LiteralPath $Backup).Length -eq 0) { throw 'Firebird produced an empty backup.' }
    $complete = $true
    Write-Output 'Firebird backup completed.'
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
finally {
    $Password = $null
    if (-not $complete -and $backupReserved) { Remove-Item -LiteralPath $Backup -ErrorAction SilentlyContinue }
}
