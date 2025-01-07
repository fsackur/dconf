using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;

namespace Dconf
{
    public class GSchemaXmlParser
    {
        public static IEnumerable<Schema> Parse(string filePath)
        {
            List<Schema> schemas = new();
            XmlDocument x = new();
            x.Load(filePath);
            XmlElement root = x.DocumentElement!;

            foreach (XmlElement xmlSchema in root.SelectNodes("schema")!)
            {
                string schemaName = xmlSchema.GetAttribute("id");
                string schemaPath = xmlSchema.GetAttribute("path");

                List<KeyInfo> keys = new();
                foreach (XmlElement xmlKey in xmlSchema.SelectNodes("key")!)
                {
                    string name = xmlKey.GetAttribute("name");
                    string typeString = xmlKey.GetAttribute("type");
                    Type type;
                    if (typeString != string.Empty)
                    {
                        type = GVariantParser.Parse(typeString);
                    }
                    else
                    {
                        string enumName = xmlKey.GetAttribute("enum");
                        string xPath;
                        bool isFlags = false;
                        if (enumName != string.Empty)
                        {
                            xPath = $"enum[@id='{enumName}']/value";
                        }
                        else
                        {
                            enumName = xmlKey.GetAttribute("flags");
                            xPath = $"flags[@id='{enumName}']/value";
                            isFlags = true;
                        }

                        if (enumName == string.Empty)
                        {
                            throw new InvalidOperationException($"Key does not define type, enum, or flags: '{schemaPath}/{name}'");
                        }

                        Dictionary<string, int> members = new();
                        foreach (XmlElement element in root.SelectNodes(xPath)!)
                        {
                            var nick = element.GetAttribute("nick");
                            var value = int.Parse(element.GetAttribute("value"));
                            members.Add(nick, value);
                        }
                        type = GEnumBuilder.BuildEnum(enumName, members, isFlags);
                    }

                    KeyInfo key = new(
                        schema: schemaName,
                        name: name,
                        path: $"{schemaPath}{name}",
                        schemaFile: filePath,
                        type: type,
                        _default: xmlKey.SelectSingleNode("default")?.InnerText,
                        summary: xmlKey.SelectSingleNode("summary")?.InnerText,
                        description: xmlKey.SelectSingleNode("description")?.InnerText
                    );
                    keys.Add(key);
                }

                SchemaInfo schema = new(
                    name: schemaName,
                    path: schemaPath,
                    schemaFile: filePath,
                    keys: keys
                );
                schemas.Add(schema);
            }
            return schemas;
        }

        public GSchemaXmlParser(IEnumerable<string> schemaFiles) => SchemaFiles = schemaFiles;

        public IEnumerable<string> SchemaFiles { get; init; }

        public IEnumerable<Schema> GetSchemas()
        {
            return SchemaFiles
                .Select(file => Parse(file))
                .SelectMany(schema => schema);
        }
    }
}
