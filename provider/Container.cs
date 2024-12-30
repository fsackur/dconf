using System.Linq;
using System.Management.Automation;

namespace Dconf
{
    public partial class DconfProvider
    {
        protected override bool HasChildItems(string path)
        {
            if (Get(path) is Schema schema)
            {
                return schema.Children.Count > 0;
            }
            return false;
        }

        protected override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            if (Get(path) is not Schema schema) { return;}

            foreach (var child in schema.Children)
            {
                var isContainer = child is Schema;
                WriteItemObject(child.Name, path, isContainer);
            }
        }

        protected override void GetChildItems(string path, bool recurse)
        {
            if (Get(path) is not NodeInfo item) { return; }

            if (item is not Schema schema)
            {
                WriteItemObject(item, item.Path, false);
                return;
            }

            foreach (var child in schema.Children)
            {
                var isContainer = child is Schema;
                WriteItemObject(child, child.Path, isContainer);
            }

            if (!recurse) { return; }

            foreach (var child in schema.Children.Where(child => child is Schema))
            {
                GetChildItems(child.Path, recurse);
            }
        }
    }
}
