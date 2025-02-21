function Set-Dconf
{
    <#
        .DESCRIPTION
        Imports dconf settings.

        .EXAMPLE
        dconf dump / > dump.txt
        Get-Content dump.txt | Set-Dconf
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Position = 0)]
        [string]$Path = '/',

        [Parameter(Mandatory, ValueFromPipeline, Position = 1)]
        [AllowEmptyString()]
        [string[]]$InputObject,

        [string[]]$Filter
    )

    end
    {
        $Path = $Path -replace '^/?', '/' -replace '(?<=[^/])$', '/'
        $_Path = $Path

        if ($Filter)
        {
            $Filter = @($Filter) -replace '^/?', '/' -replace '(?<=[^/])$', '/'
        }

        if ($MyInvocation.ExpectingInput)
        {
            $InputObject = $input
        }

        # Can't get past error: "Key file contains line [some_group] which is not a key-value pair, group, or comment"
        # So we use dconf write instead of dconf load
        $Lines = ($InputObject | Out-String).Trim() -split '\r?\n'
        foreach ($Line in $Lines)
        {
            if ([string]::IsNullOrWhiteSpace($Line) -or $Line.StartsWith('#'))
            {
                continue
            }
            elseif ($Line -match '^\[(?<Path>.+)\]\s*$')
            {
                $_Path = $(
                    $MatchedPath = $Matches.Path
                    if ($MatchedPath -eq '/')
                    {
                        $Path -replace '/$'
                    }
                    elseif ($MatchedPath -match '^/.')
                    {
                        $MatchedPath
                    }
                    else
                    {
                        $Path, $MatchedPath -join '/' -replace '/{2,}', '/'
                    }
                )

                $ShouldSkip = $Filter -and -not ($Filter | Where-Object {$_Path -ilike "$_*"})
                if ($ShouldSkip) {Write-Verbose "Skipping $_Path"}
                continue
            }

            if ($ShouldSkip) {continue}

            $Key, $Value = $Line -split '=', 2
            $FullKey = $_Path, $Key -join '/' -replace '/{2,}', '/'

            if ($Value -eq $Script:DCONF_RESET_SENTINEL)
            {
                dconf reset $FullKey
            }
            else
            {
                dconf write $FullKey "$Value"
            }

            if (-not $?)
            {
                Write-Error "Failed to write '$Value' to '$FullKey'"
            }
        }
    }
}
