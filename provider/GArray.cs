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
    public class GArray : GVariant
    {
        public GArray(GVariant genericArg)
        {
            GenericArg = genericArg;
            ManagedType = genericArg.ManagedType.MakeArrayType();
        }

        public Type ManagedType { get; init; }

        public GVariant GenericArg { get; init; }

        public object? Deserialize(string encoded)
        {
            return GVariantUtils
                .SplitEncodedArray(encoded)
                .Select(e => GenericArg.Deserialize(e))
                .ToArray();
        }
    }
}
