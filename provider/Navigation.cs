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
        protected override bool IsItemContainer(string path)
        {
            WriteDebug($"IsItemContainer {path}");
            var decomposed = DecomposePath(path);
            return decomposed.leaf.Count() == 0;
        }
    }
}
