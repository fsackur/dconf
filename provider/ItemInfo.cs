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
    public enum NodeType
    {
        SchemaPart,
        Schema,
        Key
    }

    /// <remarks>
    /// If we have schemas `org.gnome.mutter` and `org.gnome.shell`, that does not
    /// make `org.gnome` a valid schema. But we still wish to navigate through it.
    /// This is a tree to enable navigation through partial segments of schemas.
    /// </remarks>
    public class NodeInfo
    {
        private IList<NodeInfo> children = new List<NodeInfo>();
        private NodeInfo(string fullName, NodeType type)
        {
            FullName = fullName;
            Type = type;
        }

        public IReadOnlyList<NodeInfo> Children { get => children.AsReadOnly(); }
        public string FullName { get; init; }
        public NodeType Type { get; init; }
        public override string ToString() => FullName;

        internal static NodeInfo Build(IEnumerable<string> paths)
        {
            var segments = paths.Select(p => p.Split('.'));
            return Build("/", segments, 0);
        }

        private static NodeInfo Build(string fullName, IEnumerable<string[]> splitPaths, int depth)
        {
            NodeInfo? node = null;
            List<string[]> childPaths = new();
            foreach (var splitPath in splitPaths)
            {
                if (splitPath.Length == depth)
                {
                    node = new NodeInfo(fullName, NodeType.Schema);
                }
                else
                {
                    childPaths.Add(splitPath);
                }
            }
            node ??= new NodeInfo(fullName, NodeType.SchemaPart);

            var fragments = splitPaths.Where(p => p.Count() > depth);

            var groups = childPaths.GroupBy(p => p[depth]);
            var newDepth = depth + 1;

            foreach (var group in groups)
            {
                var newName = string.Join('.', group.First()[0..newDepth]);
                var child = Build(newName, group, newDepth);
                node.children.Add(child);
            }

            return node;
        }
    }
}
