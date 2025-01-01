BeforeAll {
    $Script:MockSchemas = @{
        "org.gnome.mutter.wayland.keybindings /org/gnome/mutter/wayland/keybindings/" = @{
            "restore-shortcuts" = "@as []"
            "switch-to-session-1" = "['<Primary><Alt>F1']"
        }
        "org.gnome.nautilus /org/gnome/nautilus/" = @{
        }
        "org.gnome.mutter.keybindings /org/gnome/mutter/keybindings/" = @{
            "cancel-input-capture" = "@as []"
            "rotate-monitor" = "['XF86RotateWindows']"
            "switch-monitor" = "['<Super>p', 'XF86Display']"
        }
        "org.gnome.mutter.wayland /org/gnome/mutter/wayland/" = @{
            "xwayland-allow-byte-swapped-clients" = "false"
            "xwayland-allow-grabs" = "false"
        }
        "org.gnome.nautilus.compression /org/gnome/nautilus/compression/" = @{
            "default-compression-format" = "'zip'"
        }
        "org.gnome.mutter /org/gnome/mutter/" = @{
            "attach-modal-dialogs" = "false"
            "auto-maximize" = "true"
            "center-new-windows" = "false"
        }
    }

    class MockGSettings : Dconf.GSettings
    {
        [string[]] ListSchemas([bool]$includePaths)
        {
            $Schemas = $Script:MockSchemas.Keys
            if (-not $includePaths)
            {
                $Schemas = $Schemas -replace ' .*'
            }
            return $Schemas
        }

        [string[]] ListKeys([string]$path)
        {
            $SchemaAndPath = $Script:MockSchemas.Keys -match ".* /?$path/?$"
            return $Script:MockSchemas[$SchemaAndPath].Keys
        }

        [string[]] Get([string] $schema, [string] $key)
        {
            $SchemaAndPath = $Script:MockSchemas.Keys -match "^$schema .*"
            return $Script:MockSchemas[$SchemaAndPath][$key]
        }
    }
}

Describe "Dconf.Provider" {
    BeforeAll {
        $GSettings = [MockGSettings]::new()
        $DconfDrive = New-PSDrive -Name dconf -PSProvider Dconf -Root / -GSettings $GSettings
    }
    AfterAll {
        Remove-PSDrive $DconfDrive
    }

    Context "Location" {
        BeforeAll {
            $Expected = "dconf:/org"
            Push-Location $Expected
        }
        AfterAll {Pop-Location}

        It "Changes location" {
            Get-Location | Should -Be $Expected
        }

        It "Gets item" {
            Get-Item "gnome" | Should -Match "gnome"
        }

        It "Gets item by wildcard" {
            Get-Item "gn*me" | Should -Match "gnome"
            Get-Item "gn*me/mutt*" | Should -Match "mutter"
        }

        It "Gets child items" {
            Get-ChildItem "gnome" | Where-Object Name -eq "org.gnome.mutter" | Should -HaveCount 1
        }

        It "Gets child items by wildcard" {
            Get-ChildItem "gnome/mutter/a*" | Should -HaveCount 2
        }
    }
}
