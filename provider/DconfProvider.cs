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
    internal class DriveInfo : PSDriveInfo
    {
        public DriveInfo(PSDriveInfo driveInfo) : base(driveInfo) { }
    }

    internal enum DconfBinary
    {
        dconf,
        gsettings
    }

    [CmdletProvider("Dconf", ProviderCapabilities.None)]
    public class DconfProvider : NavigationCmdletProvider
    {
        /// <remarks>
        /// If we have schemas `org.gnome.mutter` and `org.gnome.shell`, that does not
        /// make `org.gnome` a valid schema. But we still wish to navigate through it.
        /// This is a tree to enable navigation through partial segments of schemas.
        /// </remarks>
        internal class SchemaPath
        {
            Dictionary<string, SchemaPath> children = new();

            // Whether this node is a valid schema
            internal bool isSchema;

            internal string basePath;

            private SchemaPath(string basePath) => this.basePath = basePath;

            internal static SchemaPath Build(string[] paths, string basePath = "")
            {
                var node = new SchemaPath(basePath);
                var groups = paths
                    .Select(p => p.Split('.', 2))
                    .GroupBy(
                        arr => arr[0],
                        arr => arr.Length > 1 ? arr[1] : null
                    );

                foreach (var group in groups)
                {
                    var newBasePath = string.Join(basePath, group.Key);
                    var childPaths = group.Where(p => p is not null).ToArray();
                    var child = Build(childPaths!, newBasePath);
                    child.isSchema = childPaths.Length < group.Count();
                    node.children[group.Key] = child;
                }
                return node;
            }

            internal SchemaPath? Get(string childName)
            {
                SchemaPath? sp;
                if (children.TryGetValue(childName, out sp))
                {
                    return sp;
                }
                return null;
            }

            internal IEnumerable<SchemaPath> List() => children.Values.ToList();
        }

        internal record DecomposedPath
        {
            public required IEnumerable<string> container;
            public required SchemaPath? schemaPath;
            public required IEnumerable<string> leaf;
        }


        protected override PSDriveInfo NewDrive(PSDriveInfo drive)
        {
            return new DriveInfo(drive);
        }

        protected override PSDriveInfo RemoveDrive(PSDriveInfo drive)
        {
            if (drive == null)
            {
                WriteError(new ErrorRecord(
                    new ArgumentNullException("drive"),
                    "NullDrive",
                    ErrorCategory.InvalidArgument,
                    drive)
                );
            }
            var dconfDrive = drive as DriveInfo;
#pragma warning disable CS8603 // Possible null reference return.
            return dconfDrive;
#pragma warning restore CS8603 // Possible null reference return.
        }

        private string[] Invoke(DconfBinary binary, string[] args)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = binary.ToString(),
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
            return output.Split("\n");
        }

        protected string[] InvokeDconf(string[] args) => Invoke(DconfBinary.dconf, args);

        protected string[] InvokeGsettings(string[] args) => Invoke(DconfBinary.gsettings, args);

        protected string[] List(string path)
        {
            return InvokeDconf(["list", path]);
        }

        protected static string Trim(string path)
        {
            if (path.StartsWith('/'))
                path = path.Substring(1);
            if (path.EndsWith('/'))
                path = path.Substring(0, path.Length - 1);
            return path;
        }

        protected static string ToGsettingsPath(string path) => Trim(path).Replace('/', '.');

        protected static string[] ToChunks(string path) => Trim(path).Split('/');

        protected static string GetBase(string path) => string.Join('.', ToChunks(path)[0..^2]);

        private string[]? schemas = null;

        protected string[] Schemas {
            get
            {
                schemas ??= InvokeGsettings(["list-schemas"]);
                return schemas;
            }
        }

        private SchemaPath? schemaPaths = null;

        private SchemaPath SchemaPaths
        {
            get
            {
                schemaPaths ??= SchemaPath.Build(Schemas);
                return schemaPaths;
            }
        }

        private DecomposedPath DecomposePath(string path)
        {
            var leafChunks = ToChunks(path);
            List<string> containerChunks = new();
            SchemaPath? sp = SchemaPaths;

            while (leafChunks.Length > 0)
            {
                sp = sp.Get(leafChunks[0]);
                if (sp == null)
                    break;
                containerChunks.Add(leafChunks[0]);
                leafChunks = leafChunks[1..^1];
            }
            return new DecomposedPath()
            {
                container = containerChunks,
                schemaPath = sp,
                leaf = leafChunks,
            };
        }

        private string[] GetSchemaKeys(string path) => InvokeGsettings(["list-keys", path]);

        protected override bool IsValidPath(string path)
        {
            return true;
        }

        protected override bool ItemExists(string path)
        {
            WriteDebug($"ItemExists {path}");
            var decomposed = DecomposePath(path);
            return decomposed.leaf.Count() <= 1;
        }

        protected override bool IsItemContainer(string path)
        {
            WriteDebug($"IsItemContainer {path}");
            var decomposed = DecomposePath(path);
            return decomposed.leaf.Count() == 0;
        }

        protected override void GetItem(string path)
        {
            WriteDebug($"GetItem {path}");
            string command;
            bool isContainer;
            (command, isContainer) = path.EndsWith("/") ? ("dump", true) : ("read", false);

            WriteItemObject(InvokeDconf([command, path]), path, isContainer);
        }

        // protected override bool HasChildItems( string path )
        // {
        //     return false;
        // }

        protected override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            WriteDebug($"GetChildNames {path}");
            if (!path.EndsWith("/"))
            {
                path = $"{path}/";
            }
            List(path);
        }

        protected override void GetChildItems(string path, bool recurse)
        {
            WriteDebug($"GetChildItems {path}");

            var decomposed = DecomposePath(path);
            if (decomposed.schemaPath == null)
            {
                if (decomposed.leaf.Count() > 1)
                {
                    WriteError(new ErrorRecord(
                        new ItemNotFoundException($"Cannot find path '{path}' because it does not exist."),
                        "PathNotFound",
                        ErrorCategory.ObjectNotFound,
                        path)
                    );
                }
                else
                {
                    WriteItemObject(path, path, false);
                }
                return;
            }

            var sp = decomposed.schemaPath;
            var children = sp.List();
            foreach (var child in children)
            {
                WriteItemObject(child.basePath, child.basePath, true);
            }

            if (sp.isSchema)
            {
                foreach (var key in GetSchemaKeys(path))
                {
                    WriteItemObject($"{path}/{key}", $"{path}/{key}", false);
                }
            }

            if (recurse)
            {
                foreach (var child in children)
                {
                    GetChildItems(child.basePath, recurse);
                }
            }
        }

        // protected override void NewItem(string path, string type, object newItemValue)
        // {
        // }

        // protected override bool IsItemContainer(string path)
        // {}
        // {
        //     if (PathIsDrive(path))
        //     {
        //         return true;
        //     }

        //     string[] pathChunks = ChunkPath(path);
        //     string tableName;
        //     int rowNumber;

        //     PathType type = GetNamesFromPath(path, out tableName, out rowNumber);

        //     if (type == PathType.Table)
        //     {
        //         foreach (DatabaseTableInfo ti in GetTables())
        //         {
        //             if (string.Equals(ti.Name, tableName, StringComparison.OrdinalIgnoreCase))
        //             {
        //                 return true;
        //             }
        //         } // foreach (DatabaseTableInfo...
        //     } // if (pathChunks...

        //     return false;
        // } // IsItemContainer

    }
}
