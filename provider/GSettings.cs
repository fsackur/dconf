using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Xml;

namespace Dconf
{
    public class GSettings
    {
        public static string DefaultSchemaDir { get => "/usr/share/glib-2.0/schemas/"; }

        public static string DefaultUserExtensionSchemaDir
        {
            get => Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                ".local/share/gnome-shell/extensions/*/schemas"
            );
        }

        public GSettings() : this(DefaultSchemaDir) {}

        public GSettings(string schemaDir) => SchemaDir = schemaDir;

        // public GSettings(GSchemaXmlParser parser) : this() => Parser = parser;

        public string SchemaDir { get; init; }

        // TODO: resolve paths
        // private GSchemaXmlParser? parser = null;
        // public GSchemaXmlParser Parser { get => { parser ??= new([ SchemaDir ]); return parser; } }

        internal string[] Invoke(string[] args)
        {
            args = [ "--schemadir", SchemaDir, ..args ];

            ProcessStartInfo startInfo = new()
            {
                FileName = "gsettings",
                Arguments = string.Join((" "), args),
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            var proc = Process.Start(startInfo);
            ArgumentNullException.ThrowIfNull(proc);

            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                throw new RuntimeException(error);
            }
            return output.TrimEnd().Split("\n");
        }

        public string[] ListSchemas(bool includePaths)
        {
            string[] args = ["list-schemas"];
            if (includePaths) { args = [.. args, "--print-paths"]; }
            return Invoke(args);
        }

        public string[] ListKeys(string path) => Invoke(["list-keys", Utils.ToSchemaName(path)]);

        public string[] Get(string schema, string key) => Invoke(["get", schema, key]);

        public string[] Describe(string schema, string key) => Invoke(["describe", schema, key]);
    }

    public class GSchemaXmlParser
    {
        public static IEnumerable<Schema> Parse(string path)
        {
            List<Schema> schemas = new();
            XmlDocument x = new();
            x.Load(path);
            XmlElement root = x.DocumentElement;

            foreach (XmlNode xmlSchema in root.SelectNodes("schema"))
            {
                string id = xmlSchema.GetAttribute("id");
                string path = xmlSchema.GetAttribute("path");

                List<KeyInfo> keys = new();
                foreach (XmlNode xmlKey in xmlSchema.SelectNodes("key"))
                {
                    string name = xmlKey.GetAttribute("name");
                    string typeName = xmlKey.GetAttribute("type");
                    if (typeName is string.Empty)
                    {
                        string enumName = xmlKey.GetAttribute("enum");
                        var values = root.SelectNodes($"enum[@id='{enumName}']/value/@nick");
                    }

                    KeyInfo key = new(
                        name: name,
                        path: $"{path}{name}",
                        type: type,
                        default: xmlKey.SelectSingleNode("default").InnerText,
                        summary: xmlKey.SelectSingleNode("summary").InnerText,
                        description: xmlKey.SelectSingleNode("description").InnerText,
                    )
                    string name = xmlKey.name;
                }
                // SchemaInfo schema = new()
                // schemas.Add(new Schema(schema.))
            }
            return schemas;
        }

        public GSchemaXmlParser(IEnumerable<string> schemaFiles) => SchemaFiles = schemaFiles;

        public IEnumerable<string> SchemaFiles { get; init; }

        public IEnumerable<XmlDocument> Xml
        {
            get => SchemaFiles.Select(path => {
                XmlDocument x = new();
                x.Load(path);
                return x;
            });
        }

        // public IEnumerable<Schema> Schemas
        // {
        //     get => Xml.Select(x => {

        //     });
        // }
    }
}
