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

        public abstract NodeInfo Get(string path);

        public override string ToString() => FullName;
    }

    public abstract class SchemaInfoBase : NodeInfo
    {
        protected IList<SchemaInfoBase> schemas = new List<SchemaInfoBase>();

        internal SchemaInfoBase(string fullName) : base(fullName) {}

        public virtual IReadOnlyList<NodeInfo> Children { get => schemas.AsReadOnly(); }

        public override NodeInfo Get(string path)
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

            return item.Get(string.Join('.', chunks.Skip(1)));
        }

        internal static SchemaInfoBase Build()
        {
            var paths = GSettings.GetSchemas();
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
                node.schemas.Add(child);
            }

            return node;
        }
    }

    public class SchemaInfo : SchemaInfoBase
    {
        protected IList<KeyInfo>? keys = null;

        internal SchemaInfo(string fullName) : base(fullName) {}

        public IReadOnlyList<KeyInfo> Keys
        {
            get
            {
                keys ??= GSettings.GetKeys(FullName).Select(k => new KeyInfo($"{FullName}/{k}")).ToList();
                return keys.ToList().AsReadOnly();
            }
        }

        public override IReadOnlyList<NodeInfo> Children { get => schemas.Concat<NodeInfo>(Keys).ToList().AsReadOnly(); }
    }

    public class SchemaPartInfo : SchemaInfoBase
    {
        internal SchemaPartInfo(string fullName) : base(fullName) {}
    }

    public class KeyInfo : NodeInfo
    {
        internal KeyInfo(string fullName) : base(fullName) {}

        public override KeyInfo Get(string path)
        {
            return this;
        }
    }
}
