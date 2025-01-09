param
(
    $Paths = (
        "/usr/share/glib-2.0/schemas/*.gschema.xml",
        "~/.local/share/gnome-shell/extensions/*/schemas/"
    ),

    [string[]]$Types
)

$Files = gci $Paths -Filter *.gschema.xml
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
            if ($typeName -in $Types) { "$name $File" }
            # if ($enumName) { "$enumName   $File" }
        }
    }
}
