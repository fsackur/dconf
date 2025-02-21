function ConvertTo-PSObject
{
    <#
        .DESCRIPTION
        Creates KeyInfo objects representing Dconf settings.
    #>

    [CmdletBinding()]
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

        $InputObject = $InputObject | Out-String | ForEach-Object Trim

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
