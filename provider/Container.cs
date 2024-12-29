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
        // protected override void GetChildNames(string path, ReturnContainers returnContainers)
        // {
        //     WriteDebug($"GetChildNames {path}");

        // }

        protected override void GetChildItems(string path, bool recurse)
        {
            WriteDebug($"GetChildItems {path}");
            var drive = Drive;
            var item = drive.RootNode.GetSchema(path);
            foreach (var child in item.Children)
            {
                var isContainer = child is SchemaInfoBase;
                WriteItemObject(child, child.FullName, isContainer);
            }
        }
    }
}
