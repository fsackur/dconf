try
{
    $null = Get-Command dconf -ErrorAction Stop
}
catch
{
    $_.ErrorDetails = "Command not found: dconf. Please ensure it is on the path."
    throw $_
}

$Folders = "$PSScriptRoot/private", "$PSScriptRoot/public" | Resolve-Path -ea Ignore
$Folders |
    Get-ChildItem -File -Recurse -Filter *.ps1 |
    ForEach-Object {. $_}

$PathCompleter = {
    param ($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)

    $HasLeadingSlash = $wordToComplete -match '^/'
    $wordToComplete = $wordToComplete -replace '^/'
    $Paths = @(Get-DconfPath) -ilike "*$wordToComplete*"
    $DirectChildren = @($Paths) -imatch "^$wordToComplete([^/]*)$"
    $Paths = $DirectChildren, $Paths | Write-Output | Select-Object -Unique
    if ($HasLeadingSlash)
    {
        $Paths = @($Paths) -replace '^/?', '/'
    }
    $Paths
}
Register-ArgumentCompleter -CommandName Set-Dconf, Export-Dconf -ParameterName Path -ScriptBlock $PathCompleter
Register-ArgumentCompleter -CommandName Import-Dconf -ParameterName Filter -ScriptBlock $PathCompleter
