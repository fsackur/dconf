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
using System.Reflection;

namespace Dconf
{
    internal class DriveInfo : PSDriveInfo
    {
        private SchemaInfoBase? rootNode = null;
        internal SchemaInfoBase RootNode { get { rootNode ??= SchemaInfoBase.Build(); return rootNode; } }
        public DriveInfo(PSDriveInfo driveInfo) : base(driveInfo) { }
    }

    [CmdletProvider("Dconf", ProviderCapabilities.None)]
    public partial class DconfProvider : NavigationCmdletProvider
    {
        private DriveInfo Drive
        {
            get
            {
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var context = typeof(CmdletProvider).GetProperty("Context", flags)?.GetValue(this)!;
                var drive = context.GetType().GetProperty("Drive", flags)?.GetValue(context)!;
                WriteDebug($"Accessed DriveInfo: {drive}");
                return (DriveInfo)drive;
            }
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
    }
}
