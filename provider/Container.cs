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
        protected override bool HasChildItems(string path)
        {
            WriteDebug($"HasChildItems {path}");
            var schema = Get(path) as Schema;
            return schema != null && schema.Children.Count() > 0;
        }

        protected override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            WriteDebug($"GetChildNames {path}");
            var item = Get(path);

            var schema = item as Schema;
            if (schema == null) { return; }

            foreach (var child in schema.Children)
            {
                var isContainer = child is Schema;
                WriteItemObject(child.Name, path, isContainer);
            }
        }

        protected override void GetChildItems(string path, bool recurse)
        {
            WriteDebug($"GetChildItems {path}");
            var item = Get(path);
            if (item == null) { return; }

            var schema = item as Schema;
            if (schema == null)
            {
                WriteItemObject(item, item.FullName, false);
                return;
            }

            foreach (var child in schema.Children)
            {
                var isContainer = child is Schema;
                WriteItemObject(child, child.FullName, isContainer);
            }

            if (!recurse) { return; }

            foreach (var child in schema.Children.Where(child => child is Schema))
            {
                GetChildItems(child.FullName, recurse);
            }
        }
    }
}
