try
{
    $null = Get-Command dconf -ErrorAction Stop
}
catch
{
    $_.ErrorDetails = "Command not found: dconf. Please ensure it is on the path."
    throw $_
}
