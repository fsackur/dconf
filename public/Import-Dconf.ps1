function Import-Dconf
{
    [CmdletBinding()]
    param
    (
        [Parameter(Position = 0)]
        [string]$Path = '/',

        [Parameter(Mandatory, ValueFromPipeline, Position = 1)]
        [AllowEmptyString()]
        [string[]]$InputObject
    )

    end
    {
        $Path = $Path -replace '^/?', '/' -replace '(?<=[^/])$', '/'
        $_Path = $Path

        if ($MyInvocation.ExpectingInput)
        {
            $InputObject = $input
        }

        # Can't get past error: "Key file contains line [some_group] which is not a key-value pair, group, or comment"
        # So we use dconf write instead of dconf load
        $Lines = ($InputObject | Out-String).Trim() -split '\r?\n'
        foreach ($Line in $Lines)
        {
            if ([string]::IsNullOrWhiteSpace($Line))
            {
                continue
            }
            elseif ($Line -match '^\[(?<Path>.+)\]\s*$')
            {
                $__Path = $Matches.Path
                $_Path = if ($__Path -eq '/')
                {
                    $Path -replace '/$'
                }
                elseif ($__Path -match '^/.')
                {
                    $__Path
                }
                else
                {
                    $Path, $__Path -join '/' -replace '/{2,}', '/'
                }
            }
            else
            {
                $Key, $Value = $Line -split '=', 2
                $FullKey = $_Path, $Key -join '/' -replace '/{2,}', '/'
                dconf write $FullKey $Value
            }
        }
    }
}
