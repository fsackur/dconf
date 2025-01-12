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
        public GDict(IEnumerable<GVariant> genericArgs)
        {
            if (genericArgs.Count() != 2)
            {
                throw new ParseException($"Expected exactly 2 generic args for {this.GetType()}");
            }

            var genericTypes = genericArgs
                .Select(g => g.ManagedType)
                .ToArray();

            ManagedType = typeof(Dictionary<,>).MakeGenericType(genericTypes);
        }

        public Type ManagedType { get; init; }

        public object? Deserialize(string encoded) => throw new NotImplementedException();
    }
}
