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
        private NodeInfo Get(string path) => Drive.RootNode.Get(path);

        protected override bool IsValidPath(string path)
        {
            WriteDebug($"IsValidPath {path}");
            return true;
        }

        protected override bool ItemExists(string path)
        {
            WriteDebug($"ItemExists {path}");
            return Get(path) != null;
        }

        protected override void GetItem(string path)
        {
            WriteDebug($"GetItem {path}");
            var item = Get(path);
            var isContainer = item is Schema;
            WriteItemObject(item, path, isContainer);
        }
    }
}
