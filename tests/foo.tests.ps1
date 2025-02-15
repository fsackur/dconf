# BeforeDiscovery {
#     $TestCases = & ($PSCommandPath -replace 'tests.ps1', 'TestCases.ps1') |
#         % {@{
#             TypeString = $_[0]
#             InputString = $_[1]
#         }}
# }
# Describe "ManagedType" {
#     It "1" {
#         [GTypes.GString]::new("foo").ManagedType | Should -Be ([string])
#     }

#     It "2" {
#         [GTypes.GType[GTypes.GString]]::Create().ManagedType | Should -Be ([string])
#     }
# }

Describe "Parsing" {
    It "parses array" {
        $Result = [GTypes.Parser]::Parse('ab', '[42, 23]')
        $Result | Should -Be @(42, 23)
    }
}
