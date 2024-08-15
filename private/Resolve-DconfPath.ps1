function Resolve-DconfPath
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory, Position = 0)]
        [string]$Path,

        [Parameter(ValueFromPipeline, Mandatory, Position = 1)]
        [AllowEmptyString()]
        [string]$Text
    )

    process
    {
        $Lines = $Text -split '\r?\n'
        foreach ($Line in $Lines)
        {
            if ($Line -match '^\[(?<Path>.+)\]\s*$')
            {
                $_Path = $Path, $Matches.Path -join '/' -replace '/{2,}', '/' -replace '/$'
                "[$_Path]"
            }
            else
            {
                $Line
            }
        }
    }

    end {""}
}
