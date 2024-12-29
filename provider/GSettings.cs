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
    public static class GSettings
    {
        internal static string[] Invoke(string[] args)
        {
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

        public static string[] ToChunks(string path)
        {
            var chunks = path.Split(new char[] { '/', '.'}, StringSplitOptions.RemoveEmptyEntries);
            return chunks.Length > 0 ? chunks : [ string.Empty ];
        }

        public static string Normalize(string path) => string.Join('.', ToChunks(path));

        public static string GetParent(string path) => string.Join('.', ToChunks(path).SkipLast(1));

        public static string GetName(string path) => ToChunks(path).Last();

        public static string[] GetSchemas() => Invoke(["list-schemas"]);

        public static string[] GetKeys(string path) => Invoke(["list-keys", Normalize(path)]);
    }
}
