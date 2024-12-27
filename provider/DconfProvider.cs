using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Provider;

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

        private string[]? schemas = null;

        protected string[] Schemas {
            get
            {
                schemas ??= InvokeGsettings(["list-schemas"]);
                return schemas;
            }
        }

        protected override bool IsValidPath(string path)
        {
            return true;
        }

        protected override bool ItemExists(string path)
        {
            path = ToGsettingsPath(path);
            if (Schemas.Contains(path))
            {
                return true;
            }
            var chunks = ToChunks(path);
            path = string.Join('.', chunks.SkipLast(1));
            if (Schemas.Contains(path))
            {
                return InvokeGsettings(["list-keys"]).Contains(chunks[^1]);
            }
            return false;
        }

        protected override bool IsItemContainer(string path)
        {
            path = ToGsettingsPath(path);
            return Schemas.Contains(path);
        }

        protected override void GetItem(string path)
        {
            WriteDebug(path);
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
            if (!path.EndsWith("/"))
            {
                path = $"{path}/";
            }
            List(path);
        }

        protected override void GetChildItems(string path, bool recurse)
        {

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
