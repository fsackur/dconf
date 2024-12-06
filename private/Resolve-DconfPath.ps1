function Resolve-DconfPath
{
    <#
        .DESCRIPTION
        Replaces relative paths with full paths in output from dconf dump.
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory, Position = 0)]
        [Alias('Path')]
        [string]$BasePath,

        [Parameter(ValueFromPipeline, Mandatory, Position = 1)]
        [AllowEmptyString()]
        [string]$Text
    )

    process
    {
        $Lines = $Text -split '\r?\n'
        foreach ($Line in $Lines)
        {
            # This path is relative to the path passed to dconf dump
            if ($Line -match '^\[(?<Path>.+)\]\s*$')
            {
                $Path = $BasePath, $Matches.Path -join '/' -replace '/{2,}', '/' -replace '^/' -replace '/$'
                "[$Path]"
            }
            else
            {
                $Line
            }
        }
    }

    end {""}
}
