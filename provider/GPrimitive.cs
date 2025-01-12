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
    public class GPrimitive : GVariant
    {
        private readonly static Dictionary<char, GPrimitive> _primitives;

        static GPrimitive()
        {
            var unquote = GVariantUtils.Unquote;

            List<Tuple<
                        char,
                        Type,
                        Func<string, object>>> primMembers =
            new() {
                // https://docs.gtk.org/glib/gvariant-format-strings.html
                new('b', typeof(Boolean), s => Boolean.Parse(s)),
                new('y', typeof(Char), s => Char.Parse(s)),
                new('n', typeof(Int16), s => Int16.Parse(s)),
                new('q', typeof(UInt16), s => UInt16.Parse(s)),
                new('i', typeof(Int32), s => Int32.Parse(s)),
                new('u', typeof(UInt32), s => UInt32.Parse(s)),
                new('x', typeof(Int64), s => Int64.Parse(s)),
                new('t', typeof(UInt64), s => UInt64.Parse(s)),
                new('h', typeof(Int32),  s => Int32.Parse(s)),  // TODO: handle..?
                new('d', typeof(Double), s => Double.Parse(s)),
                new('v', typeof(Object), s => unquote(s)),  // TODO: variant..?
                new('s', typeof(String), s => unquote(s)),
                new('o', typeof(String), s => unquote(s)),
                new('g', typeof(String), s => unquote(s))
            };

            _primitives = new(
                primMembers.Select(args => new GPrimitive(
                                gChar: args.Item1,
                                type: args.Item2,
                                deserializer: args.Item3))
                           .Select(prim => new KeyValuePair<char, GPrimitive>(prim.GChar, prim))
            );
        }

        public static GVariant Parse(char gChar)
        {
            return _primitives.TryGetValue(gChar, out GPrimitive? prim)
                ? prim
                : throw new ParseException($"Not a primitive: {gChar}");
        }

        public static object? ToManagedType(char gChar, string encodedValue)
        {
            return _primitives.TryGetValue(gChar, out GPrimitive? prim)
                ? prim.Deserialize(encodedValue)
                : null;
        }

        private Func<string, object?> deserializer;

        public GPrimitive(char gChar, Type type, Func<string, object?> deserializer)
        {
            GChar = gChar;
            ManagedType = type;
            this.deserializer = deserializer;
        }

        public char GChar { get; init; }

        public Type ManagedType { get; init; }

        public object? Deserialize(string encoded) => deserializer(encoded);
    }
}
