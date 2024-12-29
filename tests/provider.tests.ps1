# BeforeAll {& (Get-Module dconf -ea Stop) {function Script:dconf {}}}
# AfterAll {& (Get-Module dconf -ea Stop) {Remove-Item function:/dconf}}
$Global:DebugPreference = "Continue"

Describe "Dconf.Provider" {
    BeforeAll {
        $DconfDrive = New-PSDrive -Name dconf -PSProvider Dconf -Root /
    }
    AfterAll {
        Remove-PSDrive $DconfDrive
    }

    Context "Location" {
        BeforeAll {
            $Expected = "dconf:/org/"
            Push-Location $Expected
        }
        AfterAll {Pop-Location}

        It "Changes location" {
            Get-Location | Should -Be $Expected
        }

        It "Gets item" {
            Get-Item "gnome" | Should -Match "gnome"
        }
    }
}
