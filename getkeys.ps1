param
(
    $Type = [Dconf.GEnum],
    [switch]$Parse
)

$GetValue = {gsettings get $_.Schema $_.Name}
$ParseValue = {$Value = & $GetValue; $_.Type.Deserialize($Value)}
$Prop = if ($Parse) {$ParseValue} else {$GetValue}

gci dconf:/org -Recurse |
    ? Type -is $Type |
    ft Schema, Name, Type, @{n="value"; e=$Prop}, SchemaFile
