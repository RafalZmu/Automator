param(
    [Parameter(Mandatory = $true, Position = 0)][string]$SourceFolder,
    [Parameter(Mandatory = $true, Position = 1)][string]$DaysNotAccessed,
    [Parameter(Mandatory = $true, Position = 2)][ValidateSet('true', 'false')][string]$IncludeSubfolders,
    [Parameter(Mandatory = $true, Position = 3)][ValidateSet('true', 'false')][string]$PreviewOnly
)

$ErrorActionPreference = 'Stop'
try {
    # Windows PowerShell's .NET Framework lacks Path.IsPathFullyQualified.
    # Accept drive roots and UNC server/share roots, rejecting C:folder and \folder.
    if ($SourceFolder -notmatch '^(?:[A-Za-z]:[\\/]|\\\\[^\\/]+[\\/][^\\/]+(?:[\\/]|$))') { throw 'The source folder must be a fully qualified absolute path.' }
    if (-not (Test-Path -LiteralPath $SourceFolder -PathType Container)) { throw 'The source folder does not exist.' }
    $days = 0
    if (-not [int]::TryParse($DaysNotAccessed, [ref]$days) -or $days -lt 1 -or $days -gt 36500) { throw 'Days not accessed must be a whole number from 1 to 36500.' }

    $cutoff = (Get-Date).AddDays(-$days)
    $scan = @{
        LiteralPath = $SourceFolder
        Filter      = '*.fdb'
        File        = $true
        Force       = $true
        Recurse     = ($IncludeSubfolders -eq 'true')
    }
    $found = @(Get-ChildItem @scan -ErrorAction SilentlyContinue -ErrorVariable scanErrors)
    foreach ($scanError in $scanErrors) {
        [Console]::Error.WriteLine("Not scanned: $($scanError.Exception.Message)")
    }

    $stale = @($found | Where-Object { $_.LastAccessTime -lt $cutoff } | Sort-Object FullName)
    Write-Output ('Found {0} .fdb file(s); {1} not accessed since {2:yyyy-MM-dd}.' -f $found.Count, $stale.Count, $cutoff)
    foreach ($file in $stale) {
        Write-Output ('  {0}  (last access {1:yyyy-MM-dd})' -f $file.FullName, $file.LastAccessTime)
    }

    if ($PreviewOnly -eq 'true') {
        Write-Output 'Preview only: nothing was changed. Set Preview only to false to zip and delete the files listed above.'
        return
    }

    Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
    $archived = 0
    $skipped = 0
    foreach ($file in $stale) {
        $zipPath = "$($file.FullName).zip"
        if (Test-Path -LiteralPath $zipPath) {
            # Never overwrite an existing archive; name the new one with a timestamp instead.
            $zipPath = "$($file.FullName).$(Get-Date -Format 'yyyyMMdd_HHmmss').zip"
            Write-Output "  $($file.Name).zip already exists; using $(Split-Path -Leaf $zipPath)"
        }
        if (Test-Path -LiteralPath $zipPath) {
            [Console]::Error.WriteLine("Skipped $($file.FullName): $zipPath already exists.")
            $skipped++
            continue
        }

        # Write under a temporary name so an interrupted run never leaves a partial .fdb.zip
        $partialPath = "$zipPath.partial"
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
                $entryStream = $archive.Entries[0].Open()
                try {
                    $archivedHash = [BitConverter]::ToString($sha.ComputeHash($entryStream)).Replace('-', '')
                }
                finally {
                    $entryStream.Dispose()
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
            [Console]::Error.WriteLine("Skipped $($file.FullName): $($_.Exception.Message)")
            $skipped++
            continue
        }

        try {
            Remove-Item -LiteralPath $file.FullName -Force
            $archived++
            Write-Output "Archived $zipPath"
        }
        catch {
            [Console]::Error.WriteLine("Zipped $($file.FullName), but the original could not be deleted: $($_.Exception.Message)")
            $skipped++
        }
    }

    Write-Output "Done: $archived archived, $skipped skipped."
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
