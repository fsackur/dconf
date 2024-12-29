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
    public partial class DconfProvider
    {
        protected override bool IsValidPath(string path)
        {
            WriteDebug($"IsValidPath {path}");
            return true;
        }

        protected override bool ItemExists(string path)
        {
            WriteDebug($"ItemExists {path}");
            return true;
        }

        protected override void GetItem(string path)
        {
            WriteDebug($"GetItem {path}");
            var drive = Drive;
            if (Drive == null)
            {
                // TODO: more appropriate error
                WriteError(new ErrorRecord(
                    new ArgumentException("drive"),
                    "Drive",
                    ErrorCategory.InvalidArgument,
                    drive)
                );
                return;
            }

            var item = drive.RootNode.GetSchema(path);
            var isContainer = item is SchemaInfoBase;
            WriteItemObject(item, item.FullName, isContainer);
        }
    }
}
