function Import-Dconf
{
    <#
        .SYNOPSIS
        Imports dconf settings from file.

        .DESCRIPTION
        Imports dconf settings from file. Backs up all settings before importing.

        .PARAMETER File
        Path to file containing dconf settings. The file can be generated with `Export-Dconf` or
        with `dconf dump /`. Content should be key-value pairs, section headers in square brackets.

        Note that when using `dconf dump`, the section headers will be relative to the path passed
        to `dconf dump`. If this was not `/` (the root path), then the import will succeed, but
        incorrect paths and keys will be created. Export-Dconf prevents this by resolving paths to
        absolute paths.

        .PARAMETER Filter
        Limit the dconf paths to import. Only keys that are children of the dconf paths will be
        imported.

        .PARAMETER SkipBackup
        Do not back up dconf settings before import.

        .PARAMETER BackupPath
        Path to backup file. By default, this will be `/tmp/dconf.xxxxxxxxxxxxxxxxxx`.

        .EXAMPLE
        Import-Dconf -File ./dconf.dump

        Imports all settings from `dconf.dump`.

        .EXAMPLE
        Import-Dconf -File ./dconf.dump -Filter org/gnome/shell/extensions

        Imports settings under `/org/gnome/shell/extensions/` from `dconf.dump`.
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory, Position = 0)]
        [string]$File,

        [string[]]$Filter,

        [switch]$SkipBackup,

        [string]$BackupPath = (Join-Path ([IO.Path]::GetTempPath()) "dconf.$([datetime]::UtcNow.Ticks)")
    )

    end
    {
        if (-not $SkipBackup)
        {
            Export-Dconf / -OutFile $BackupPath
            "Backed up dconf settings to $BackupPath" | Write-Verbose
        }

        $Content = Get-Content $File -ErrorAction Stop
        $Content | Set-Dconf -Filter $Filter
    }
}
