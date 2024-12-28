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
        protected override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            WriteDebug($"GetChildNames {path}");
            if (!path.EndsWith("/"))
            {
                path = $"{path}/";
            }
            List(path);
        }

        protected override void GetChildItems(string path, bool recurse)
        {
            WriteDebug($"GetChildItems {path}");

            var decomposed = DecomposePath(path);
            if (decomposed.schemaPath == null)
            {
                if (decomposed.leaf.Count() > 1)
                {
                    WriteError(new ErrorRecord(
                        new ItemNotFoundException($"Cannot find path '{path}' because it does not exist."),
                        "PathNotFound",
                        ErrorCategory.ObjectNotFound,
                        path)
                    );
                }
                else
                {
                    WriteItemObject(path, path, false);
                }
                return;
            }

            var sp = decomposed.schemaPath;
            var children = sp.List();
            foreach (var child in children)
            {
                WriteItemObject(child.basePath, child.basePath, true);
            }

            if (sp.isSchema)
            {
                foreach (var key in GetSchemaKeys(path))
                {
                    WriteItemObject($"{path}/{key}", $"{path}/{key}", false);
                }
            }

            if (recurse)
            {
                foreach (var child in children)
                {
                    GetChildItems(child.basePath, recurse);
                }
            }
        }
    }
}
