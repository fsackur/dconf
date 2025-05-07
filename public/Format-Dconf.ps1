function Format-Dconf
{
    <#
        .SYNOPSIS
        Parses dconf content and sorts keys.
    #>

    [CmdletBinding(DefaultParameterSetName = "Stream")]
    param
    (
        [Parameter(ParameterSetName = "Stream", Mandatory, Position = 0, ValueFromPipeline)]
        [AllowEmptyString()]
        [string[]]$InputObject,

        [Parameter(ParameterSetName = "File", Mandatory)]
        [string]$File
    )

    end
    {
        if ($PSCmdlet.ParameterSetName -eq "File")
        {
            $InputObject = Get-Content $File -ErrorAction Stop
        }
        elseif ($MyInvocation.ExpectingInput)
        {
            $InputObject = $input
        }

        $Keys = $InputObject | ConvertTo-KeyInfo | Sort-Object FullName -Unique
        $Keys | ConvertTo-Dconf
    }
}
