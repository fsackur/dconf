
namespace Dconf
{
    public partial class DconfProvider
    {
        protected override bool IsItemContainer(string path)
        {
            return Get(path) is Schema;
        }
    }
}
