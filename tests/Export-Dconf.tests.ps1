Describe "Export-Dconf" {
    BeforeAll {
        Mock -ModuleName dconf dconf {
            if ($args[0] -ne 'dump') {throw "unmocked command"}
            if (-not $args[1]) {throw "missing dconf path"}
            if ($args[1] -eq "/org/gnome/portal/filechooser/kitty/")
            {
                return "[/]`nlast-folder-path='/foo/bar'"
            }
        }

        $Output = Export-Dconf -Path "/org/gnome/portal/filechooser/kitty/"
        $Lines = ($Output -join "`n").Trim() -split '\r?\n'
    }

    It "Calls dconf" {
        Should -Invoke dconf -ModuleName dconf -Scope Describe
    }

    It "Canonicalises dconf keys" {
        $Lines[0] | Should -BeExactly '[/org/gnome/portal/filechooser/kitty]'
    }

    It "Outputs settings" {
        $Lines[1] | Should -BeExactly "last-folder-path='/foo/bar'"
    }
}
