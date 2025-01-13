using namespace System.Collections.Generic;

BeforeDiscovery {
    $TestCases = & ($PSCommandPath -replace 'tests.ps1', 'TestCases.ps1') |
        ? {$_.TypeString -notmatch 'v' -and $_.TypeString -match '\('}
}

Describe "Dconf.GVariantParser" {
    Context "Parsing" {
        It "Parses a type string: '<TypeString>'" -ForEach $TestCases {
            $Result = [Dconf.GVariantParser]::Parse($TypeString)
            $Result.ManagedType | Should -Be $ManagedType
        }
    }

    Context "Deserialization" {
        It "Deserializes a value string: '<InputString>'" -ForEach (
            $TestCases | Where-Object {$_.ContainsKey("Expected")}
        ) {
            $Parser = [Dconf.GVariantParser]::Parse($TypeString)
            $Result = $Parser.Deserialize($InputString)

            if ($ManagedType.IsAssignableTo([Collections.IDictionary]))
            {
                $Result.Keys | Sort-Object | Should -Be ($Expected.Keys | Sort-Object)
                $Expected.Keys | ForEach-Object {
                    $Result[$_] | Should -Be $Expected[$_]
                }
            }
            else
            {
                $Result | Should -Be $Expected
            }
        }
    }
}

# Describe "Dconf.GEnumBuilder" {
#     It "Builds an enum" {
#         [string[]]$Members = "Foo", "Bar"
#         $Result = [Dconf.GEnum]::Build(
#             "FooEnum",
#             $Members
#         ).ManagedType

#         $Result.Name | Should -Be "FooEnum"
#         $Result.IsAssignableTo([Enum]) | Should -BeTrue
#         [string[]][Enum]::GetValues($Result) | Should -BeExactly $Members
#         [int]$Result::Foo | Should -Be 0
#         [int]$Result::Bar | Should -Be 1
#         "Foo, Bar" -as $Result | Should -Not -Match "Foo, Bar"
#         $Result.GetCustomAttributes($true) | Should -BeNullOrEmpty
#     }

#     It "Builds a flag enum" {
#         [Dictionary[string, int]]$Members = [Dictionary[string, int]]::new()
#         $Members.Add("Foo", 1)
#         $Members.Add("Bar", 8)

#         $Result = [Dconf.GEnum]::Build(
#             "BarEnum",
#             $Members,
#             $true
#         ).ManagedType

#         $Result.Name | Should -Be "BarEnum"
#         $Result.IsAssignableTo([Enum]) | Should -BeTrue
#         [string[]][Enum]::GetValues($Result) | Sort-Object | Should -BeExactly ($Members.Keys | Sort-Object)
#         [int]$Result::Foo | Should -Be 1
#         [int]$Result::Bar | Should -Be 8
#         "Foo, Bar" -as $Result -as [int] | Should -Be 9
#         $Result.GetCustomAttributes($true) | Should -Match "Flags"
#     }
# }
