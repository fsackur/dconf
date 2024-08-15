BeforeAll {& (Get-Module dconf -ea Stop) {function Script:dconf {}}}
AfterAll {& (Get-Module dconf -ea Stop) {Remove-Item function:/dconf}}

Describe "Import-Dconf" {
    BeforeAll {
        Mock -ModuleName dconf dconf {
            if ($args[0] -ne 'write') {throw "unmocked command"}
            if (-not $args[1]) {throw "missing dconf path"}
        }

        "[/]`nlast-folder-path='/foo/bar'" | Import-Dconf -Path "/org/gnome/portal/filechooser/kitty/"
    }

    It "Writes settings" {
        Should -Invoke dconf -ModuleName dconf -Scope Describe -ParameterFilter {
            $args[0] | Should -BeExactly "write"
            $args[1] | Should -BeExactly "/org/gnome/portal/filechooser/kitty/last-folder-path"
            $args[2] | Should -BeExactly "'/foo/bar'"
            $args.Count | Should -Be 3
            $true
        }
    }
}
