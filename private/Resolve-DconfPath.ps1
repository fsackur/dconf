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
        if ($Text -match '^\[(?<Path>.+)\]\s*$')
        {
            $_Path = $Path, $Matches.Path -join '/' -replace '/{2,}', '/'
            "[$_Path]"
        }
        else
        {
            $Text
        }
    }

    end {""}
}
