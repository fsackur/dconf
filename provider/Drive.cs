using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Collections.Generic;
using System.Reflection;

namespace Dconf
{
    internal class DriveInfo : PSDriveInfo
    {
        private Schema? rootNode = null;
        internal Schema RootNode { get { rootNode ??= Schema.BuildTree(GSettings); return rootNode; } }
        public DriveInfo(PSDriveInfo driveInfo, GSettings gsettings) : base(driveInfo)
        {
            GSettings = gsettings;
        }

        public GSettings GSettings { get; init; }
    }

    [CmdletProvider("Dconf", ProviderCapabilities.ExpandWildcards)]
    public partial class DconfProvider : NavigationCmdletProvider, IContentCmdletProvider
    {
        internal class NewDriveDynamicParams
        {
            [Parameter(DontShow = true, ParameterSetName = "TestFixture")]  // for testing
            public GSettings? GSettings { get; set; }

            [Parameter(ParameterSetName = "__AllParameterSets")]
            public string[]? SchemaDir { get; set; }
        }

        private DriveInfo Drive { get => (DriveInfo) this.PSDriveInfo; }

        protected override PSDriveInfo NewDrive(PSDriveInfo drive)
        {
            GSettings? gsettings = null;
            List<string> schemaDirs = new() { GSettings.DefaultSchemaDir, GSettings.DefaultUserExtensionSchemaDir };

            if (DynamicParameters is NewDriveDynamicParams dp)
            {
                if (dp.GSettings is GSettings)
                {
                    gsettings = dp.GSettings;
                }

                if (dp.SchemaDir is string[] sDirs)
                {
                    foreach (var sDir in sDirs)
                    {
                        var psDirs = SessionState.InvokeProvider.Item.Get(sDir);
                        foreach (var psobject in psDirs)
                        {
                            if (psobject.BaseObject is DirectoryInfo dirInfo)
                            {
                                schemaDirs.Add(dirInfo.FullName);
                            }
                        }
                    }
                }
            }

            List<string> schemaFiles = new();
            var psFiles = SessionState.InvokeCommand.InvokeScript($"Get-ChildItem {string.Join(',', schemaDirs)} -File -Filter '*.gschema.xml'");
            foreach (var psobject in psFiles)
            {
                if (psobject.BaseObject is FileInfo fileInfo)
                {
                    schemaFiles.Add(fileInfo.FullName);
                }
            }

            gsettings ??= new();
            return new DriveInfo(drive, gsettings);
        }

        protected override object NewDriveDynamicParameters() => new NewDriveDynamicParams();

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
