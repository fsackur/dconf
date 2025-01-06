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

namespace Dconf
{
    public class GSettings
    {
        public static string DefaultSchemaDir { get => "/usr/share/glib-2.0/schemas/"; }

        public GSettings() : this(DefaultSchemaDir) {}

        public GSettings(string schemaDir) => SchemaDir = schemaDir;

        public string SchemaDir { get; init; }

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
}
