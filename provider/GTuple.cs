using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

namespace Dconf
{
    public class GTuple : GVariant
    {
        public GTuple(IEnumerable<GVariant> genericArgs)
        {
            GenericArgs = genericArgs;

            var genericTypes = genericArgs
                .Select(g => g.ManagedType)
                .ToArray();

            ManagedType = typeof(Tuple)
                .GetMethods()
                .Where(m => m.Name == "Create" && m.GetParameters().Count() == genericTypes.Count())
                .First()
                .ReturnType
                .GetGenericTypeDefinition()
                .MakeGenericType(genericTypes);
        }

        public Type ManagedType { get; init; }

        public IEnumerable<GVariant> GenericArgs { get; init; }

        public object? Deserialize(string encoded)
        {
            encoded = GVariantUtils.StripTuple(encoded);
            var items = GVariantUtils
                .SplitOnComma(encoded)
                .Zip(GenericArgs, (item, gv) => gv.Deserialize(item))
                .ToArray();

            var ctor = ManagedType.GetConstructor(ManagedType.GetGenericArguments());
            return ctor!.Invoke(items);
        }
    }
}
