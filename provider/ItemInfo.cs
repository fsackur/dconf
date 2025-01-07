using System;
using System.Collections.Generic;
using System.Linq;

namespace Dconf
{
    /// <remarks>
    /// If we have schemas `org.gnome.mutter` and `org.gnome.shell`, that does not
    /// make `org.gnome` a valid schema. But we still wish to navigate through it.
    /// This is a tree to enable navigation through partial segments of schemas.
    /// </remarks>
    public abstract class NodeInfo
    {
        internal NodeInfo(string name, string path)
        {
            Name = name;
            Path = Utils.Normalize(path);
            PathFragment = Utils.ToChunks(path).LastOrDefault() ?? string.Empty;
        }

        public string Name { get; init; }

        public string Path { get; init; }

        public string PathFragment { get; init; }

        internal string[]? chunks = null;
        internal string[] Chunks {
            get
            {
                chunks ??= Utils.ToChunks(Path);
                return chunks;
            }
        }

        public abstract NodeInfo? Get(string path);

        public override string ToString() => Path;
    }

    public abstract partial class Schema : NodeInfo
    {
        protected IList<Schema> schemas = [];

        internal Schema(string name, string path) : base(name, path) { }

        public virtual IReadOnlyList<NodeInfo> Children { get => schemas.AsReadOnly(); }

        public override NodeInfo? Get(string path)
        {
            path = Utils.Normalize(path);
            if (path == Path) { return this; }

            string relPath;
            if (path.StartsWith(Path))
            {
                relPath = Utils.Normalize(path.Substring(Path.Length));
            }
            else { return null; }

            var chunk = Utils.ToChunks(relPath).First();
            if (Children.Where(i => i.PathFragment == chunk).FirstOrDefault() is NodeInfo item)
            {
                return item.Get(path);
            }
            return null;
        }
    }

    public class SchemaInfo : Schema
    {
        protected IEnumerable<KeyInfo>? keys = null;

        internal SchemaInfo(string name, string path) : base(name, path) { }

        internal SchemaInfo(string name, string path, IEnumerable<KeyInfo> keys) : this(name, path) => this.keys = keys;

        public IReadOnlyList<KeyInfo> Keys
        {
            get => keys.ToList().AsReadOnly();
            // get
            // {
            //     keys ??= gsettings
            //         .ListKeys(Name)
            //         .Select(k => new KeyInfo(Name, k, $"{Path}/{k}"))
            //         .ToList();
            //     return keys.ToList().AsReadOnly();
            // }
        }

        public override IReadOnlyList<NodeInfo> Children { get => schemas.Concat<NodeInfo>(Keys).ToList().AsReadOnly(); }
    }

    public class SchemaPartInfo : Schema
    {
        internal SchemaPartInfo(string path) : base("", path) { }
    }

    public class KeyInfo : NodeInfo
    {
        private string? description;

        internal KeyInfo(string schema, string name, string path) : base(name, path) => Schema = schema;

        public string Schema { get; init; }

        public string? Description { get; init; }

        public override KeyInfo? Get(string path) => Utils.Normalize(path) == Path ? this : null;
    }


    public partial class Schema
    {
        internal static Schema BuildTree(GSettings gsettings)
        {
            var nameAndPaths = gsettings.ListSchemas(true);
            var schemas = nameAndPaths.Select(
                napStr => {
                    var nap = napStr.Split(' ', 2);
                    var name = nap[0];
                    var path = nap[1];
                    return new SchemaInfo(name, path);
                }
            );
            SchemaPartInfo root = new("");
            BuildTree(root, schemas, 0);
            return root;
        }

        private static void BuildTree(Schema parent, IEnumerable<Schema> schemas, int depth)
        {
            var groups = schemas.GroupBy(s => s.Chunks[depth]);
            var newDepth = depth + 1;
            foreach (var group in groups)
            {
                Schema? container = null;
                List<Schema> children = [];
                foreach (var child in group)
                {
                    if (child.Chunks.Length == newDepth)
                    {
                        container = child;
                    }
                    else
                    {
                        children.Add(child);
                    }
                }
                container ??= new SchemaPartInfo($"{parent.Path}/{group.Key}");
                parent.schemas.Add(container);
                BuildTree(container, children, newDepth);
            }
        }
    }
}
