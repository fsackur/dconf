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
