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
    public class GEmptyTuple : GVariant
    {
        public Type ManagedType { get => typeof(Tuple); }

        public object? Deserialize(string encoded) => throw new NotImplementedException();
    }

    public class GTuple : GVariant
    {
        public GTuple(IEnumerable<GVariant> genericArgs)
        {
            if (genericArgs.Count() < 1)
            {
                throw new ParseException($"Expected at least 1 generic arg for {this.GetType()}");
            }

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

        public object? Deserialize(string encoded) => throw new NotImplementedException();
    }
}
