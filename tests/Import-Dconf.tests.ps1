BeforeAll {& (Get-Module dconf -ea Stop) {function Script:dconf {}}}
AfterAll {& (Get-Module dconf -ea Stop) {Remove-Item function:/dconf}}

Describe "Import-Dconf" {
    BeforeAll {
        $FileContent = "
            [org/gnome/portal/filechooser/kitty]
            last-folder-path='/foo/bar'

            [org/gnome/desktop/interface]
            color-scheme='prefer-dark'
            enable-animations=true
        " -replace '(?<=^|\n) +'

        $ExpectedWrites = @{
            "/org/gnome/portal/filechooser/kitty/last-folder-path" = "'/foo/bar'"
            "/org/gnome/desktop/interface/color-scheme" = "'prefer-dark'"
            "/org/gnome/desktop/interface/enable-animations" = "true"
        }

        Mock -ModuleName dconf Get-Content {
            if ($Path -ne "MockInputPath") {throw "unmocked command"}
            $FileContent
        }

        Mock -ModuleName dconf dconf {
            if ($args[0] -ne 'write') {throw "unmocked command"}
            if (-not $args[1]) {throw "missing dconf path"}
        }

        Mock -ModuleName dconf Export-Dconf
    }

    Context "Import from file" {
        BeforeAll {
            Import-Dconf -File "MockInputPath" -SkipBackup
        }

        It "Writes settings" {
            Should -Invoke dconf -ModuleName dconf -Scope Context -ParameterFilter {
                $args[0] | Should -BeExactly "write"
                $args[1] | Should -BeIn $ExpectedWrites.Keys
                $args[2] | Should -BeExactly $ExpectedWrites[$args[1]]
                $args.Count | Should -Be 3
                $true
            } -Times 3 -Exactly
        }

        It "Does not back up" {
            Should -Not -Invoke Export-Dconf -ModuleName dconf -Scope Context
        }
    }

    Context "Backup" {
        BeforeAll {
            Import-Dconf -File "MockInputPath" -BackupPath "MockBackupPath"
        }

        It "Backs up" {
            Should -Invoke Export-Dconf -ModuleName dconf -Scope Context -ParameterFilter {
                $Path | Should -BeExactly "/"
                $OutFile | Should -BeExactly "MockBackupPath"
                $true
            }
        }
    }

    Context "Filter" {
        BeforeAll {
            Import-Dconf -File "MockInputPath" -SkipBackup -Filter "org/gnome/desktop"
        }

        It "Writes settings" {
            Should -Invoke dconf -ModuleName dconf -Scope Context -ParameterFilter {
                $args[0] | Should -BeExactly "write"
                $args[1] | Should -BeIn $ExpectedWrites.Keys
                $args[2] | Should -BeExactly $ExpectedWrites[$args[1]]
                $args.Count | Should -Be 3
                $true
            } -Times 2 -Exactly
        }
    }
}
