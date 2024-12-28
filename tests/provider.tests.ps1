# BeforeAll {& (Get-Module dconf -ea Stop) {function Script:dconf {}}}
# AfterAll {& (Get-Module dconf -ea Stop) {Remove-Item function:/dconf}}

Describe "Dconf.Provider" {
    BeforeAll {
        $DconfDrive = New-PSDrive -Name dconf -PSProvider Dconf -Root /
    }

    Context "Location" {
        BeforeAll {
            Push-Location dconf:/org/
        }

        It "Changes location" {
            Get-Location | Should -Match "dconf:/"
        }
    }
}
