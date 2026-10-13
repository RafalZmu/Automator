<#
.SYNOPSIS
    Zips .fdb files that have not been accessed for a number of days, then deletes the originals.

.DESCRIPTION
    Finds *.fdb files under -SourcePath whose last access time is older than
    -DaysNotAccessed days. Each match is zipped in the same folder as <name>.fdb.zip.
    The archive is compared with the original (SHA-256) before the .fdb is deleted,
    so a failed or incomplete zip never costs a database.

    Files that cannot be read (for example, a Firebird database held open by the
    server) are skipped and reported. If <name>.fdb.zip already exists, the new archive
    is named <name>.fdb.<yyyyMMdd_HHmmss>.zip instead, so an older archive is never
    overwritten. Use -WhatIf to preview the selection.

    Last access times are only accurate when NTFS last access updates are enabled.
    Check with: fsutil behavior query disablelastaccess

.PARAMETER SourcePath
    Folder to search.

.PARAMETER DaysNotAccessed
    Archive files whose last access time is older than this many days. Default: 90.

.PARAMETER NoRecurse
    Search only the top level of SourcePath instead of all subfolders.

.EXAMPLE
    .\Backup-StaleFdb.ps1 -SourcePath 'D:\Firebird\Data' -WhatIf

.EXAMPLE
    .\Backup-StaleFdb.ps1 -SourcePath 'D:\Firebird\Data' -DaysNotAccessed 180
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Container })]
    [string]$SourcePath,

    [ValidateRange(1, 36500)]
    [int]$DaysNotAccessed = 90,

    [switch]$NoRecurse
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

$sourceRoot = (Resolve-Path -LiteralPath $SourcePath).ProviderPath
$cutoff = (Get-Date).AddDays(-$DaysNotAccessed)

# Search
$scan = @{
    LiteralPath = $sourceRoot
    Filter      = '*.fdb'
    File        = $true
    Force       = $true
    Recurse     = -not $NoRecurse
}
$found = @(Get-ChildItem @scan -ErrorAction SilentlyContinue -ErrorVariable scanErrors)
foreach ($err in $scanErrors) {
    Write-Warning "Not scanned: $($err.Exception.Message)"
}

$stale = @($found | Where-Object { $_.LastAccessTime -lt $cutoff } | Sort-Object FullName)
Write-Host ("Found {0} .fdb file(s); {1} not accessed since {2:yyyy-MM-dd}." -f $found.Count, $stale.Count, $cutoff)

foreach ($file in $stale) {
    Write-Host ("  {0}  (last access {1:yyyy-MM-dd})" -f $file.FullName, $file.LastAccessTime)
}

# Zip each file next to the original, then delete the original
$archived = 0
$skipped = 0

foreach ($file in $stale) {
    $zipPath = "$($file.FullName).zip"
    if (Test-Path -LiteralPath $zipPath) {
        # Keep the existing archive; name the new one with a timestamp instead.
        $zipPath = "$($file.FullName).$(Get-Date -Format 'yyyyMMdd_HHmmss').zip"
        Write-Host "  $($file.Name).zip already exists; using $(Split-Path -Leaf $zipPath)"
    }
    $partialPath = "$zipPath.partial"

    if (Test-Path -LiteralPath $zipPath) {
        Write-Warning "Skipped $($file.FullName): $zipPath already exists."
        $skipped++
        continue
    }

    if (-not $PSCmdlet.ShouldProcess($file.FullName, "Zip to '$zipPath' and delete the original")) { continue }

    # Write under a temporary name so an interrupted run never leaves a partial <name>.fdb.zip
    try {
        Remove-Item -LiteralPath $partialPath -Force -ErrorAction SilentlyContinue

        $zip = [System.IO.Compression.ZipFile]::Open($partialPath, [System.IO.Compression.ZipArchiveMode]::Create)
        try {
            [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName, $file.Name, [System.IO.Compression.CompressionLevel]::Optimal)
        }
        finally {
            $zip.Dispose()
        }

        # Compare the archived copy with the original before anything is deleted.
        $originalHash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash

        $archive = [System.IO.Compression.ZipFile]::OpenRead($partialPath)
        try {
            if ($archive.Entries.Count -ne 1 -or $archive.Entries[0].Name -ne $file.Name) {
                throw "archive does not contain exactly '$($file.Name)'"
            }

            $sha = [System.Security.Cryptography.SHA256]::Create()
            $stream = $archive.Entries[0].Open()
            try {
                $archivedHash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '')
            }
            finally {
                $stream.Dispose()
                $sha.Dispose()
            }
        }
        finally {
            $archive.Dispose()
        }

        if ($archivedHash -ne $originalHash) {
            throw 'archive contents do not match the original (file changed while archiving?)'
        }

        Move-Item -LiteralPath $partialPath -Destination $zipPath
    }
    catch {
        Remove-Item -LiteralPath $partialPath -Force -ErrorAction SilentlyContinue
        Write-Warning "Skipped $($file.FullName): $($_.Exception.Message)"
        $skipped++
        continue
    }

    try {
        Remove-Item -LiteralPath $file.FullName -Force
        $archived++
        Write-Host "Archived $zipPath"
    }
    catch {
        Write-Warning "Zipped $($file.FullName), but the original could not be deleted: $($_.Exception.Message)"
        $skipped++
    }
}

Write-Host "Done: $archived archived, $skipped skipped."
