using namespace System.Collections.Generic;

BeforeDiscovery {
    $TestData = @{
        "()" = [Tuple]
        "y" = [char]
        "x" = [Int64]
        "v" = [object]
        "u" = [UInt32]
        "t" = [UInt64]
        "s" = [string]
        "q" = [UInt16]
        "o" = [string]  # represents schema path
        "n" = [Int16]
        "ms" = [Dconf.Maybe[string]]
        "mb" = [Dconf.Maybe[bool]]
        "i" = [Int32]
        "h" = [Int32]  # handle???
        "g" = [string]  # e.g. iias  ???
        "d" = [double]
        "b" = [bool]
        "ay" = [char[]]
        "av" = [object[]]
        "as" = [string[]]
        "ao" = [string[]]
        "ai" = [Int32[]]
        "ad" = [double[]]
        "aay" = [char[][]]
        "aas" = [string[][]]
        "a{sv}" = [Dictionary[string, object][]]
        "aa{sv}" = [Dictionary[string, object][][]]
        "a{ss}" = [Dictionary[string, string][]]
        "a(us)" = [Tuple[uint, string][]]
        "a(sss)" = [Tuple[string, string, string][]]
        "a(ss)" = [Tuple[string, string][]]
        "a(dddd)" = [Tuple[double, double, double, double][]]
        "a((aussasasu)u)" = [Tuple[
            Tuple[uint[], string, string, string[], string[], uint],
            uint
        ][]]
        "a((auss)u)" = [Tuple[
            Tuple[uint[], string, string],
            uint
        ][]]
        "{ss}" = [Dictionary[string, string]]
        "(ssm(dd))" = [Tuple[string, string, Dconf.Maybe[Tuple[double, double]]]]
        "(sb)" = [Tuple[string, bool]]
        "(iib)" = [Tuple[int, int, bool]]
        "(ii)" = [Tuple[int, int]]
        "(dd)" = [Tuple[double, double]]
        "(bdddd)" = [Tuple[bool, double, double, double, double]]
        "(bbsmv)" = [Tuple[bool, bool, string, Dconf.Maybe[object]]]
    }

    $TestCases = $TestData.GetEnumerator() | ForEach-Object {
        @{
            TypeString = $_.Key
            Expected = $_.Value
        }
    }
}

Describe "Dconf.GVariantParser" {
    Context "Parsing" {
        It "Parses a type string: '<TypeString>'" -ForEach $TestCases {
            $Result = [Dconf.GVariantParser]::Parse($TypeString)
            $Result.ManagedType | Should -Be $Expected
        }
    }
}

Describe "Dconf.GEnumBuilder" {
    It "Builds an enum" {
        [string[]]$Members = "Foo", "Bar"
        $Result = [Dconf.GEnum]::Build(
            "FooEnum",
            $Members
        ).ManagedType

        $Result.Name | Should -Be "FooEnum"
        $Result.IsAssignableTo([Enum]) | Should -BeTrue
        [string[]][Enum]::GetValues($Result) | Should -BeExactly $Members
        [int]$Result::Foo | Should -Be 0
        [int]$Result::Bar | Should -Be 1
        "Foo, Bar" -as $Result | Should -Not -Match "Foo, Bar"
        $Result.GetCustomAttributes($true) | Should -BeNullOrEmpty
    }

    It "Builds a flag enum" {
        [Dictionary[string, int]]$Members = [Dictionary[string, int]]::new()
        $Members.Add("Foo", 1)
        $Members.Add("Bar", 8)

        $Result = [Dconf.GEnum]::Build(
            "BarEnum",
            $Members,
            $true
        ).ManagedType

        $Result.Name | Should -Be "BarEnum"
        $Result.IsAssignableTo([Enum]) | Should -BeTrue
        [string[]][Enum]::GetValues($Result) | Sort-Object | Should -BeExactly ($Members.Keys | Sort-Object)
        [int]$Result::Foo | Should -Be 1
        [int]$Result::Bar | Should -Be 8
        "Foo, Bar" -as $Result -as [int] | Should -Be 9
        $Result.GetCustomAttributes($true) | Should -Match "Flags"
    }
}
