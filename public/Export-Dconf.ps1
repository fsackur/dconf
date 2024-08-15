function Export-Dconf
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory, Position = 0)]
        [string[]]$Path
    )

    foreach ($_Path in $Path)
    {
        $_Path = $_Path -replace '^/?', '/' -replace '(?<=[^/])$', '/'
        dconf dump $_Path | Resolve-DconfPath -Path $_Path
    }
}

Register-ArgumentCompleter -CommandName Export-Dconf -ParameterName Path -ScriptBlock {
    param ($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)

    $Paths = Get-DconfPath
    (@($Paths) -like "$wordToComplete*"), (@($Paths) -like "*$wordToComplete*") | Write-Output
}
