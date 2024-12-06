BeforeAll {& (Get-Module dconf -ea Stop) {function Script:dconf {}}}
AfterAll {& (Get-Module dconf -ea Stop) {Remove-Item function:/dconf}}

Describe "Export-Dconf" {
    BeforeAll {
        $ExpectedKey = "UNMOCKED"
        $MockDump = "UNMOCKED"

        $MockScriptblock = {
            if ("UNMOCKED" -in $ExpectedKey, $ExpectedDump) {throw "mock variables not set"}
            if ($args[0] -ne 'dump') {throw "unmocked command"}
            if (-not $args[1]) {throw "missing dconf path"}
            if ($args[1] -eq $ExpectedKey)
            {
                return $MockDump
            }
            else {throw "unmocked key"}
        }
    }

    Context "Exports all settings" {
        BeforeAll {
            $ExpectedKey = "/"
            $MockDump = "
                [org/gnome/portal/filechooser/kitty]
                last-folder-path='/foo/bar'

                [org/gnome/desktop/interface]
                color-scheme='prefer-dark'
                enable-animations=true
            ".Trim() -split "\n" -replace '^\s+'

            Mock -ModuleName dconf dconf $MockScriptblock

            $Output = Export-Dconf
            $Output = $Output.TrimEnd() -split "\r?\n"
        }

        It "Calls dconf" {
            Should -Invoke dconf -ModuleName dconf -Scope Context -Times 1 -Exactly
        }

        It "Canonicalises dconf keys" {
            $Output[0] | Should -BeExactly '[org/gnome/portal/filechooser/kitty]'
            $Output[3] | Should -BeExactly '[org/gnome/desktop/interface]'
        }

        It "Outputs settings" {
            $Output[1] | Should -BeExactly $MockDump[1]
            $Output[4] | Should -BeExactly $MockDump[4]
            $Output[5] | Should -BeExactly $MockDump[5]
            $Output.Count | Should -Be 6
        }
    }

    Context "Exports key" {
        BeforeAll {
            $ExpectedKey = "/org/gnome/desktop/"
            $MockDump = "
                [interface]
                color-scheme='prefer-dark'
                enable-animations=true
            ".Trim() -split "\n" -replace '^\s+'

            Mock -ModuleName dconf dconf $MockScriptblock

            $Output = Export-Dconf -Path "org/gnome/desktop"
            $Output = $Output.TrimEnd() -split "\r?\n"
        }

        It "Calls dconf" {
            Should -Invoke dconf -ModuleName dconf -Scope Context -Times 1 -Exactly
        }

        It "Canonicalises dconf keys" {
            $Output[0] | Should -BeExactly '[org/gnome/desktop/interface]'
        }

        It "Outputs settings" {
            $Output[1] | Should -BeExactly $MockDump[1]
            $Output[2] | Should -BeExactly $MockDump[2]
            $Output.Count | Should -Be 3
        }
    }

    Context "Writes file" {
        BeforeAll {
            $ExpectedKey = "/"
            $MockDump = "
                [org/gnome/portal/filechooser/kitty]
                last-folder-path='/foo/bar'

                [org/gnome/desktop/interface]
                color-scheme='prefer-dark'
                enable-animations=true
            ".Trim() -split "\n" -replace '^\s+'

            Mock -ModuleName dconf dconf $MockScriptblock

            $MockPath = "TestDrive:/dconf"
            $Output = Export-Dconf -OutFile $MockPath
            $Content = Get-Content $MockPath
        }

        It "Does not write to stdout" {
            $Output | Should -BeNullOrEmpty
        }

        It "Writes file" {
            $Content | Should -BeExactly $MockDump
        }
    }
}
