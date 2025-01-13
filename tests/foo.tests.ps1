Describe "ManagedType" {
    It "1" {
        [GTypes.GString]::new("foo").ManagedType | Should -Be ([string])
    }

    It "2" {
        [GTypes.GType[GTypes.GString]]::Create().ManagedType | Should -Be ([string])
    }
}
