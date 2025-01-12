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
    public class GDict : GVariant
    {
        public GDict(GVariant genericKeyArg, GVariant genericValueArg)
        {
            GenericKeyArg = genericKeyArg;
            GenericValueArg = genericValueArg;
            Type[] genericTypes = [ genericKeyArg.ManagedType, genericValueArg.ManagedType ];
            ManagedType = typeof(Dictionary<,>).MakeGenericType(genericTypes);
        }

        public Type ManagedType { get; init; }

        public GVariant GenericKeyArg { get; init; }

        public GVariant GenericValueArg { get; init; }

        public object? Deserialize(string encoded)
        {
            encoded = GVariantUtils.StripDict(encoded);
            var kvps = GVariantUtils.SplitOnComma(encoded).Select(GVariantUtils.SplitKeyValuePair);
            var ctor = ManagedType.GetConstructor(new Type[] { typeof(int) });
            var result = (IDictionary)ctor!.Invoke(new object[] { kvps.Count() });
            foreach (var (key, value) in kvps)
            {
                object parsedKey = GenericKeyArg.Deserialize(key)!;
                object parsedValue = GenericValueArg.Deserialize(value)!;
                result.Add(parsedKey, parsedValue);
            }
            return result;
        }
    }
}
