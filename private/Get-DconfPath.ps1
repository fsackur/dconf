function Get-DconfPath
{
    <#
        .DESCRIPTION
        Get all dconf paths that exist in the system.
    #>

    [CmdletBinding()]
    param
    (
        [switch]$Flush
    )

    if ($Flush -or -not $Script:DconfPaths)
    {
        $Dump = dconf dump /
        if (-not $?)
        {
            throw ($Dump | Out-String).Trim()
        }

        $KeyPaths = $Dump |
            Select-String '^\[(?<path>.*)\]$' |
            ForEach-Object Matches |
            ForEach-Object Groups |
            Where-Object Name -eq "path" |
            ForEach-Object Value

        # dconf dump omits paths that do not contain keys. For completions, we want those paths.
        # Assumption: dump output is ordered heirarchically
        $Last = ""
        $Script:DconfPaths = $KeyPaths | ForEach-Object {
            $Current = $_
            while ($Last -and $Current -notmatch "^$Last/.*")
            {
                $Last = $Last -replace '[^/]+?$' -replace '/$'
            }

            while ($Current -ne $Last)
            {
                $NextSegment = $Current -replace $Last -replace '^/' -replace '/.*'
                $Last = $Last, $NextSegment -join '/' -replace '^/'
                $Last
            }
        }
    }
    $Script:DconfPaths
}
