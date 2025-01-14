using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

namespace GTypes
{
    using PTypeFunc = Func<(GType<GVariant>, IEnumerator<char>)>;
    using PFunc = Func<(GVariant, IEnumerator<char>)>;

    internal class ParseException : Exception
    {
        internal ParseException(string msg) : base(msg) {}
    }

    public class Parser
    {
        #region Sentinels

        private class GVoid : GType<GVariant> {}

        private static readonly GType<GVariant> Void = new GVoid();

        private class GNone : GVariant
        {
            internal GNone() : base(null!) {}

            public override object Value
            {
                get => throw new ParseException($"Cannot get value of a sentinel: {this.GetType()}");
                init {}
            }

            public override Type ManagedType { get => throw new ParseException($"Cannot get managed type of a sentinel: {this.GetType()}"); }
        }

        private static readonly GVariant None = new GNone();

        #endregion Sentinels

        private static (GType<GVariant>, IEnumerator<char>) ConsumeType(IEnumerator<char> chars)
        {
            if (!chars.MoveNext())
            {
                return (Void, chars);
            }

            char c = chars.Current;
            Func<Type, PTypeFunc> make = T => () => (GType.Create(T), chars);

            PTypeFunc consume = c switch
            {
                'b' => make(typeof(GBool)),
                'y' => make(typeof(GChar)),
                'n' => make(typeof(GInt16)),
                'q' => make(typeof(GUInt16)),
                'i' => make(typeof(GInt32)),
                'u' => make(typeof(GUInt32)),
                'x' => make(typeof(GInt64)),
                't' => make(typeof(GUInt64)),
                'h' => make(typeof(GInt32)),  // TODO: handle..?
                'd' => make(typeof(GDouble)),
                'v' => make(typeof(GVariant<object>)),  // TODO: object..?
                's' => make(typeof(GString)),
                'o' => make(typeof(GString)),  // TODO: further processing
                'g' => make(typeof(GString)),  // TODO: further processing

                'a' => () =>
                {
                    GType<GVariant> gType;
                    (gType, chars) = ConsumeType(chars);
                    Console.WriteLine($"{gType} {gType.GVariant}");
                    Type arrayType = gType.GVariant.MakeArrayType();
                    // typeof(IEnumerable<>).MakeGenericType(gType.GetType());
                    Type T = typeof(GArray<>).MakeGenericType(arrayType);
                    return (GType.Create(T), chars);
                },

                _ => throw new ParseException($"Not a type: {c}")
            };

            return consume();
        }

        public static GType<GVariant> ParseType(string typeString)
        {
            if (string.IsNullOrEmpty(typeString))
            {
                return Void;
            }

            var (gType, charEnum) = ConsumeType(typeString.GetEnumerator());

            if (charEnum.MoveNext()) { throw new ParseException("We should have consumed all chars"); }

            return gType;
        }

        private static GVariant Pop(GType<GVariant> gType, IEnumerable<char> chars) => Pop(gType, new(chars.ToArray()));

        private static GVariant Pop(GType<GVariant> gType, string encoded)
        {
            Type type = gType.GetType().GetGenericArguments()[0];
            var ctor = type.GetConstructor(new Type[] { typeof(string) });
            return (GVariant)ctor!.Invoke(new object[] { encoded });
        }

        private static (GVariant, IEnumerator<char>) Consume(
            GType<GVariant> gType,
            IEnumerator<char> chars,
            List<char> buffer,
            char endMarker = (char)0
        )
        {
            bool isQuoted = endMarker == '\'' || endMarker == '"';
            bool bufferIsEmpty = buffer.Count() == 0;

            if (!chars.MoveNext())
            {
                return bufferIsEmpty
                    ? (None, chars)
                    : (Pop(gType, buffer), chars);
            }
            char c = chars.Current;

            PFunc pop = () => (Pop(gType, buffer), chars);
            PFunc recurse = () => Consume(gType, chars, buffer, endMarker);
            PFunc pushAndRecurse = () => { buffer.Add(c); return recurse(); };

            if (c == endMarker) { return pop(); }
            if (isQuoted) { return pushAndRecurse(); }

            PFunc consume = c switch
            {
                ' ' when bufferIsEmpty => recurse,
                ' ' => pop,
                ',' => pop,

                '\'' or '"' => () =>
                {
                    endMarker = c;
                    return recurse();
                },

                '@' when bufferIsEmpty => () =>
                {
                    var origType = gType;

                    (gType, chars) = ConsumeType(chars);
                    Console.WriteLine($"{gType}");
                    buffer = new();
                    endMarker = c;

                    GVariant value;
                    (value, chars) = recurse();
                    Console.WriteLine($"{value}");

                    value = origType is GChar
                        ? new GChar((char)value.Value)
                        : Pop(origType, value.Value.ToString()!);
                    return (value, chars);
                },

                _ => pushAndRecurse
            };

            return consume();
        }

        public static GVariant Parse(string typeString, string encoded) => Parse(ParseType(typeString), encoded);

        public static GVariant Parse(GType<GVariant> gType, string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
            {
                return None;
            }

            var (v, charEnum) = Consume(gType, encoded.GetEnumerator(), new());
            if (charEnum.MoveNext()) { throw new ParseException("We should have consumed all chars"); }
            return v;
        }
    }
}
