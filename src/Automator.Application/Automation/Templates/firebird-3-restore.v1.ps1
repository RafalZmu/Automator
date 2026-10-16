param(
    [Parameter(Mandatory = $true, Position = 0)][string]$Database,
    [Parameter(Mandatory = $true, Position = 1)][string]$Backup,
    [Parameter(Mandatory = $true, Position = 2)][string]$Gbak,
    [Parameter(Mandatory = $true, Position = 3)][string]$Username,
    [Parameter(Mandatory = $true, Position = 4)][string]$Password,
    [Parameter(Mandatory = $true, Position = 5)][ValidateSet('true', 'false')][string]$Replace
)
$ErrorActionPreference = 'Stop'
$tempDatabase = $null
$tempReserved = $false
$complete = $false
try {
    foreach ($path in @($Database, $Backup, $Gbak)) {
        if ($path -notmatch '^(?:[A-Za-z]:[\\/]|\\\\[^\\/]+[\\/][^\\/]+(?:[\\/]|$))') { throw 'All paths must be fully qualified absolute paths.' }
    }
    if (-not (Test-Path -LiteralPath $Backup -PathType Leaf)) { throw 'Backup file does not exist.' }
    if (-not (Test-Path -LiteralPath $Gbak -PathType Leaf)) { throw 'Firebird gbak executable does not exist.' }
    if ([System.IO.Path]::GetExtension($Database) -ine '.fdb') { throw 'Database file must use the .fdb extension.' }
    if ((Get-Item -LiteralPath $Backup).Length -eq 0) { throw 'Backup file is empty.' }
    $directory = [System.IO.Path]::GetDirectoryName($Database)
    if (-not (Test-Path -LiteralPath $directory -PathType Container)) { throw 'Database directory does not exist.' }
    if ([string]::Equals([System.IO.Path]::GetFullPath($Database), [System.IO.Path]::GetFullPath($Backup), [System.StringComparison]::OrdinalIgnoreCase)) { throw 'Database and backup must be different files.' }
    $replaceExisting = $Replace -eq 'true'
    if ((Test-Path -LiteralPath $Database) -and -not $replaceExisting) { throw 'Database already exists. Choose a new database path or enable replace.' }
    # The restore goes to a temporary file beside the target, so a failed restore never damages an existing database.
    $tempDatabase = Join-Path $directory ([System.IO.Path]::GetFileName($Database) + '.restoring-' + [Guid]::NewGuid().ToString('N') + '.tmp')
    try {
        $reservation = [System.IO.File]::Open($tempDatabase, [System.IO.FileMode]::CreateNew)
        $reservation.Dispose()
    }
    catch { throw 'Database directory is not writable.' }
    Remove-Item -LiteralPath $tempDatabase -Force
    $tempReserved = $true
    & $Gbak '-c' '-user' $Username '-password' $Password $Backup $tempDatabase *> $null
    if ($LASTEXITCODE -ne 0) { throw 'Firebird restore failed. Check the backup, credentials, and gbak version.' }
    if (-not (Test-Path -LiteralPath $tempDatabase -PathType Leaf) -or (Get-Item -LiteralPath $tempDatabase).Length -eq 0) { throw 'Firebird produced an empty database.' }
    if (Test-Path -LiteralPath $Database) {
        if (-not $replaceExisting) { throw 'Database already exists. Choose a new database path or enable replace.' }
        Move-Item -LiteralPath $tempDatabase -Destination $Database -Force
    }
    else {
        Move-Item -LiteralPath $tempDatabase -Destination $Database
    }
    $complete = $true
    Write-Output 'Firebird restore completed.'
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
finally {
    $Password = $null
    if (-not $complete -and $tempReserved -and $tempDatabase) { Remove-Item -LiteralPath $tempDatabase -Force -ErrorAction SilentlyContinue }
}
