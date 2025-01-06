using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace Dconf
{
    public partial class DconfProvider
    {
        private bool IsTabCompleting()
        {
            // workaround for https://github.com/PowerShell/PowerShell/issues/24744
            var stack = Environment.StackTrace;
            var completerMatch = Regex.Match(stack, @"\bSystem\.Management\.Automation\.CommandCompletion\.CompleteInput\(");
            return completerMatch.Success;
        }

        private NodeInfo? Get(string path) => Drive.RootNode.Get(path);

        protected override bool IsValidPath(string path)
        {
            return true;
        }

        protected override bool ItemExists(string path)
        {
            if (Get(path) is not NodeInfo item) { return false; }
            if (item is KeyInfo && path.EndsWith('/')) { return false; }
            return true;
        }

        protected override void GetItem(string path)
        {
            var item = Get(path);
            var isContainer = item is Schema;

            if (!isContainer && IsTabCompleting())
            {
                return;
            }

            WriteItemObject(item, path, isContainer);
        }

        protected override string[] ExpandPath(string path)
        {
            var chunks = Utils.ToChunks(path);
            return ExpandPath(Drive.RootNode, chunks).ToArray();
        }

        private IList<string> ExpandPath(NodeInfo item, string[] chunks)
        {
            List<string> matches = [];
            if (chunks.Length == 0) { return matches; }

            if (item is not Schema schema) { return matches; }

            var chunk = chunks[0];
            chunks = chunks[1..^0];
            WildcardPattern pattern = new(chunk);

            foreach (var child in schema.Children)
            {
                if (child.PathFragment == chunk || pattern.IsMatch(child.PathFragment))
                {
                    if (chunks.Length == 0)
                    {
                        matches.Add(child.Path);
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
