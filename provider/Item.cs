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

        protected override string[] ExpandPath(string path)
        {
            WriteDebug($"ExpandPath {path}");
            var chunks = GSettings.ToChunks(path);
            var matches = ExpandPath(Drive.RootNode, chunks);
            return matches.ToArray();
        }

        private IList<string> ExpandPath(NodeInfo item, string[] chunks)
        {
            List<string> matches = new();
            if (chunks.Length == 0) { return matches; }

            var schema = item as Schema;
            if (schema == null) { return matches; }

            var chunk = chunks[0];
            chunks = chunks[1..^0];
            WildcardPattern pattern = new(chunk);

            foreach (var child in schema.Children)
            {
                if (child.Name == chunk || pattern.IsMatch(child.Name))
                {
                    if (chunks.Length == 0)
                    {
                        matches.Add(child.FullName);
                    }
                    else
                    {
                        matches.AddRange(ExpandPath(child, chunks));
                    }
                }
            }
            return matches;
        }
    }
}
