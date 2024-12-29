using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Dconf
{
    public partial class DconfProvider
    {
        private NodeInfo? Get(string path) => Drive.RootNode.Get(path);

        protected override bool IsValidPath(string path)
        {
            return true;
        }

        protected override bool ItemExists(string path)
        {
            return Get(path) is not null;
        }

        protected override void GetItem(string path)
        {
            var item = Get(path);
            var isContainer = item is Schema;
            WriteItemObject(item, path, isContainer);
        }

        protected override string[] ExpandPath(string path)
        {
            var chunks = GSettings.ToChunks(path);
            return ExpandPath(Drive.RootNode, chunks).ToArray();
        }

        private IList<string> ExpandPath(NodeInfo item, string[] chunks)
        {
            List<string> matches = new();
            if (chunks.Length == 0) { return matches; }

            if (item is not Schema schema) { return matches; }

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
