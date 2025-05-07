function Compare-Dconf
{
    <#
        .SYNOPSIS
        Compares two sets of dconf settings.

        .DESCRIPTION
        Shows differences between two sets of dconf settings.

        By default, this command captures a snapshot of all settings, then waits for you to make
        changes before capturing a second snapshot.

        .PARAMETER ReferenceObject
        The "before" settings.

        If this argument is not supplied, the command will dump the current settings as a base for
        comparison.

        .PARAMETER DifferenceObject
        The "after" settings.

        If this argument is not supplied, the command will pause while you make dconf changes,
        then capture the new settings for comparison.

        .EXAMPLE
        Compare-Dconf

        Captured dconf snapshot
        Waiting to capture snapshot (press enter after making changes):

        [org/gnome/desktop/app-folders]
        folder-children=['Utilities', 'YaST', 'Pardus']

        Captures the dconf changes made while the command was running. In this example, the user
        has modified the /org/gnome/desktop/app-folders/folder-children key.

        .EXAMPLE
        Export-Dconf > ./before

        <make dconf changes...>

        Export-Dconf > ./after

        Compare-Dconf (Get-Content ./before) (Get-Content ./after)

        [org/gnome/settings-daemon/plugins/media-keys/custom-keybindings/custom0]
        binding='<Super>k'
        command='kitty'
        name='Kitty'

        In this example, the user set a keybinding for the kitty terminal between capturing the
        "before" and "after" snapshots.
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Position = 0)]
        [string[]]$ReferenceObject,

        [Parameter(Position = 1)]
        [string[]]$DifferenceObject
    )

    $Ref = if ($null -eq $ReferenceObject)
    {
        Export-Dconf | ConvertTo-KeyInfo
        Write-Host "Captured dconf snapshot"
    }
    else
    {
        $ReferenceObject | ConvertTo-KeyInfo
    }

    $Diff = if ($null -eq $DifferenceObject)
    {
        $null = Read-Host "Waiting to capture snapshot (press enter after making changes)"
        Export-Dconf | ConvertTo-KeyInfo
    }
    else
    {
        $DifferenceObject | ConvertTo-KeyInfo
    }

    $DiffsByPath = Compare-Object $Ref $Diff -Property FullName, Value | Group-Object FullName
    $Changed = $DiffsByPath |
        ForEach-Object {
            if ($_.Count -gt 1)
            {
                $_.Group | Where-Object SideIndicator -eq '=>'
            }
            elseif ($_.Group[0].SideIndicator -eq '=>')
            {
                $_.Group[0]
            }
            else
            {
                $Removed = $_.Group[0]
                $Removed.Value = $Script:DCONF_RESET_SENTINEL
                $Removed
            }
        } |
        Write-Output |
        ForEach-Object {
            $Path, $Key = $_.FullName -split '/(?=[^/]+/?$)', 2
            [Dconf.KeyInfo]::new($Path, $Key, $_.Value)
        }

    $Changed | ConvertTo-Dconf
}
