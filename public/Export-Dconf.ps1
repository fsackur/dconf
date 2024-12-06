function Export-Dconf
{
    <#
        .SYNOPSIS
        Exports dconf settings.

        .DESCRIPTION
        Exports dconf settings within a given dconf path to stdout or to file. Wraps `dconf dump`
        and converts dconf paths to absolute paths.

        .PARAMETER Path
        The dconf path to export.

        .PARAMETER OutFile
        The file to export to. By default, this command writes to stdout.

        .EXAMPLE
        Export-Dconf org/gnome/shell/extensions ./dconf.dump

        Exports settings under `/org/gnome/shell/extensions/` to `dconf.dump`.
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Position = 0)]
        [string[]]$Path = "/",

        [Parameter(Position = 1)]
        [string]$OutFile
    )

    $Content = @()

    foreach ($_Path in $Path)
    {
        $_Path = $_Path -replace '^/?', '/' -replace '(?<=[^/])$', '/'
        $Content += dconf dump $_Path | Resolve-DconfPath -Path $_Path
    }

    $Content = ($Content | Out-String).Trim()
    if ($OutFile)
    {
        $Content > $OutFile
    }
    elseif ($Content)
    {
        $Content
    }
}
