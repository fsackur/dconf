using System;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Reflection;

namespace Dconf
{
    internal class DriveInfo : PSDriveInfo
    {
        private Schema? rootNode = null;
        internal Schema RootNode { get { rootNode ??= Schema.Build(); return rootNode; } }
        public DriveInfo(PSDriveInfo driveInfo) : base(driveInfo) { }
    }

    [CmdletProvider("Dconf", ProviderCapabilities.ExpandWildcards)]
    public partial class DconfProvider : NavigationCmdletProvider
    {
        private DriveInfo Drive { get => (DriveInfo) this.PSDriveInfo; }

        protected override PSDriveInfo NewDrive(PSDriveInfo drive)
        {
            return new DriveInfo(drive);
        }

        protected override PSDriveInfo RemoveDrive(PSDriveInfo drive)
        {
            if (drive == null)
            {
                WriteError(new ErrorRecord(
                    new ArgumentNullException(nameof(drive)),
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
