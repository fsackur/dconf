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
            return true;
        }

        protected override bool ItemExists(string path)
        {
            WriteDebug($"ItemExists {path}");
            var decomposed = DecomposePath(path);
            return decomposed.leaf.Count() <= 1;
        }

        protected override void GetItem(string path)
        {
            WriteDebug($"GetItem {path}");
            string command;
            bool isContainer;
            (command, isContainer) = path.EndsWith("/") ? ("dump", true) : ("read", false);

            WriteItemObject(InvokeDconf([command, path]), path, isContainer);
        }
    }
}
