function Get-DconfPath
{
    [CmdletBinding()]
    param
    (
        [switch]$Refresh
    )

    if ($Refresh -or -not $Script:DconfPaths)
    {
        $Dump = dconf dump /
        $Script:DconfPaths = $Dump |
            Select-String '^\[(?<path>.*)\]$' |
            ForEach-Object Matches |
            ForEach-Object Groups |
            Where-Object Name -eq "path" |
            ForEach-Object Value
    }
    $Script:DconfPaths
}
