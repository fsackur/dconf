function Export-Dconf
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory, Position = 0)]
        [string]$Path
    )

    $Path = $Path -replace '^/?', '/' -replace '(?<=[^/])$', '/'
    dconf dump $Path | Resolve-DconfPath -Path $Path
}

Register-ArgumentCompleter -CommandName Export-Dconf -ParameterName Path -ScriptBlock {
    param ($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)

    $Paths = Get-DconfPath
    (@($Paths) -like "$wordToComplete*"), (@($Paths) -like "*$wordToComplete*") | Write-Output
}
