param
(
    $Paths = (
        "/usr/share/glib-2.0/schemas/*.gschema.xml",
        "~/.local/share/gnome-shell/extensions/*/schemas/"
    ),

    [char[]]$Types
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
            if ($typeName.Length -eq 1 -and $typeName -in $Types) { $File }
        }
    }
}
