param
(
    $Paths = (
        "/usr/share/glib-2.0/schemas/*.gschema.xml",
        "~/.local/share/gnome-shell/extensions/*/schemas/"
    ),

    [string[]]$Types
)

$Files = gci $Paths -Filter *.gschema.xml #-Exclude ca.desrt.dconf-editor.gschema.xml
foreach ($File in $Files)
{
    $x = [System.Xml.XmlDocument]::new()
    $x.Load($File)
    $root = $x.DocumentElement

    foreach ($schema in $root.SelectNodes("schema"))
    {
        $id = $schema.GetAttribute("id")
        $path = $schema.GetAttribute("path")

        foreach ($key in $schema.SelectNodes("key"))
        {
            $name = $key.GetAttribute("name")
            $typeName = $key.GetAttribute("type")
            $enumName = $key.GetAttribute("enum")
            $flagName = $key.GetAttribute("flags")
            $typeString = $typeName, $enumName, $flagName | where {$_} | select -first 1

            if ($typeName -in $Types)
            {
                [pscustomobject]@{
                    PSTypeName = "KeyFacts"
                    xmlFile = $File.FullName
                    schema = $id
                    key = $name
                    typeString = $typeString
                    value = gsettings get $id $name
                }
            }
            # if ($enumName) { "$enumName   $File" }
        }
    }
}
