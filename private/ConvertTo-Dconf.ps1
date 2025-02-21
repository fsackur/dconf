function ConvertTo-Dconf
{
    <#
        .DESCRIPTION
        Formats KeyInfo objects as Dconf settings.
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory, Position = 0, ValueFromPipeline)]
        [Dconf.KeyInfo[]]$InputObject
    )

    end
    {
        if ($MyInvocation.ExpectingInput)
        {
            $InputObject = $input
        }

        $ByPath = $InputObject | Group-Object Path
        $ByPath |
            ForEach-Object {
                "[$($_.Name)]"
                $_.Group | ForEach-Object {
                    "$($_.Key)=$($_.Value)"
                }
                ""
            } |
            Write-Output
    }
}
