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
    /// <remarks>
    /// If we have schemas `org.gnome.mutter` and `org.gnome.shell`, that does not
    /// make `org.gnome` a valid schema. But we still wish to navigate through it.
    /// This is a tree to enable navigation through partial segments of schemas.
    /// </remarks>
    public abstract class NodeInfo
    {
        internal NodeInfo(string fullName)
        {
            FullName = fullName;
            Name = GSettings.GetName(fullName);
        }

        public string FullName { get; init; }
        public string Name { get; init; }
        public override string ToString() => FullName;
    }

    public abstract class SchemaInfoBase : NodeInfo
    {
        private IList<SchemaInfoBase> children = new List<SchemaInfoBase>();

        internal SchemaInfoBase(string fullName) : base(fullName) {}

        public virtual IReadOnlyList<SchemaInfoBase> Children { get => children.AsReadOnly(); }

        internal static SchemaInfoBase Build(IEnumerable<string> paths)
        {
            var segments = paths.Select(p => p.Split('.'));
            return Build("/", segments, 0);
        }

        private static SchemaInfoBase Build(string fullName, IEnumerable<string[]> splitPaths, int depth)
        {
            SchemaInfoBase? node = null;
            List<string[]> childPaths = new();
            foreach (var splitPath in splitPaths)
            {
                if (splitPath.Length == depth)
                {
                    node = new SchemaInfo(fullName);
                }
                else
                {
                    childPaths.Add(splitPath);
                }
            }
            node ??= new SchemaPartInfo(fullName);

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

        public SchemaInfoBase GetSchema(string path)
        {
            var chunks = GSettings.ToChunks(path);
            var chunk = chunks.FirstOrDefault();

            if (chunk == null && Name == string.Empty)
            {
                return this;
            }

            var item = Children.Where(i => i.Name == chunk).First();
            if (chunks.Length == 1)
            {
                return item;
            }

            return item.GetSchema(string.Join('.', chunks.Skip(1)));
        }
    }

    public class SchemaInfo : SchemaInfoBase
    {
        private IList<KeyInfo> keys = new List<KeyInfo>();

        internal SchemaInfo(string fullName) : base(fullName) {}
    }

    public class SchemaPartInfo : SchemaInfoBase
    {
        internal SchemaPartInfo(string fullName) : base(fullName) {}
    }

    public class KeyInfo : NodeInfo
    {
        internal KeyInfo(string fullName) : base(fullName) {}
    }
}
