function ConvertTo-KeyInfo
{
    <#
        .DESCRIPTION
        Creates KeyInfo objects representing Dconf settings.
    #>

    [CmdletBinding()]
    [OutputType([Dconf.KeyInfo[]])]
    param
    (
        [Parameter(Mandatory, Position = 0, ValueFromPipeline)]
        [AllowEmptyString()]
        [string[]]$InputObject
    )

    end
    {
        if ($MyInvocation.ExpectingInput)
        {
            $InputObject = $input
        }

        $InputObject = $InputObject | Out-String | ForEach-Object Trim | Select-Object -Unique

        $InputObject -split "\n" |
            Where-Object {-not [string]::IsNullOrWhiteSpace($_)} |
            ForEach-Object {
                if ($_ -match '^\[(?<Path>.*)\]\s*$')
                {
                    $Path = $Matches.Path
                }
                else
                {
                    $Key, $Value = $_ -split '=', 2
                    [Dconf.KeyInfo]::new($Path, $Key, $Value)
                }
            }
    }
}
