using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Globalization;

namespace GVariant
{
    public class GPrimitive : GVariant
    {
        private readonly static Dictionary<char, Type> typeMap = new()
        {
            { 'b', typeof(Boolean) },
            { 'y', typeof(Char) },
            { 'n', typeof(Int16) },
            { 'q', typeof(UInt16) },
            { 'i', typeof(Int32) },
            { 'u', typeof(UInt32) },
            { 'x', typeof(Int64) },
            { 't', typeof(UInt64) },
            { 'h', typeof(Int32) },  // TODO: handle..?
            { 'd', typeof(Double) },
            // { 'v', typeof(Object) },  // TODO
            { 's', typeof(String) },
            { 'o', typeof(String) },
            { 'g', typeof(String) },
        };

        private readonly static Dictionary<char, GPrimitive> primitiveMap = new();

        public static GVariant<object> Parse(char gChar)
        {
            GVariant<object> prim;
            if (primitiveMap.TryGetValue(gChar, out prim!))
            {
                return prim;
            }

            if (!typeMap.TryGetValue(gChar, out Type? type))
            {
                throw new ParseException($"Not a primitive: {gChar}");
            }

            Type gType = typeof(GPrimitive<>).GetGenericTypeDefinition().MakeGenericType(new Type[] { type });
            var ctor = gType.GetConstructor(new Type[0])!;
            prim = (GPrimitive<object>)ctor.Invoke(new object[0]);

            primitiveMap.Add(gChar, prim);
            return prim;
        }
    }

    public class GPrimitive<T> : GPrimitive, GVariant<T> where T : IParsable<T>
    {
        public T Deserialize(string encoded)
        {
            if (typeof(T) == typeof(string))
            {
                encoded = GVariantUtils.Unquote(encoded);
            }
            return T.Parse(encoded, CultureInfo.InvariantCulture);
        }
    }
}
