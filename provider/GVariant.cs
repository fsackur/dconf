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
    internal static class GVariantUtils
    {
        private static readonly Regex unquotePattern = new("""^(['\"])?(?<unquoted>.*)\1$""");
        private static readonly Regex arrayPattern = new("""^(\[)(?<contents>.*)\1$""");
        internal static string Unquote(string s) => unquotePattern.Replace(s, "${unquoted}");
        internal static string[] SplitEncodedArray(string s)
        {
            Regex commaPattern = new(@",\s*");
            var contents = arrayPattern.Replace(s, "${contents}");
            return string.IsNullOrEmpty(contents)
                ? new string[] { }
                : commaPattern.Split(contents);
        }
    }

    internal class ParseException : Exception {
        internal ParseException(string msg) : base(msg) {}
    }

    public interface GVariant
    {
        public Type ManagedType { get; }

        public object? Deserialize(string encoded);
    }

    public class GVariantParser
    {
        private class GSentinel : GVariant
        {
            public Type ManagedType { get => throw new InvalidOperationException($"Cannot work with {nameof(GNone)}"); }

            public object? Deserialize(string encoded) => throw new InvalidOperationException($"Cannot work with {nameof(GNone)}");
        }

        private class GNone : GSentinel {}

        private class GCloseBracket : GSentinel {}

        private static readonly GVariant None = new GNone();

        private static readonly GCloseBracket CloseRoundBracket = new GCloseBracket();

        private static readonly GCloseBracket CloseCurlyBracket = new GCloseBracket();

        private static (IEnumerable<GVariant>, IEnumerator<char>) ConsumeUntil(IEnumerator<char> charEnum, GCloseBracket marker)
        {
            List<GVariant> result = new();
            while (true)
            {
                GVariant v;
                (v, charEnum) = Consume(charEnum);
                if (v == None) { throw new ParseException($"Expecting {marker}"); }
                if (v == marker) { break; }
                result.Add(v);
            }
            return (result, charEnum);
        }

        private static (GVariant, IEnumerator<char>) Consume(IEnumerator<char> charEnum)
        {
            if (!charEnum.MoveNext())
            {
                return (None, charEnum);
            }

            char c = charEnum.Current;
            Func<IEnumerator<char>, (GVariant, IEnumerator<char>)> consume = c switch
            {
                'a' => charEnum =>
                {
                    GVariant v;
                    (v, charEnum) = Consume(charEnum);
                    return (new GArray(v), charEnum);
                },

                'm' => charEnum =>
                {
                    GVariant v;
                    (v, charEnum) = Consume(charEnum);
                    return (new GMaybe(v), charEnum);
                },

                ')' => charEnum => (CloseRoundBracket, charEnum),

                '(' => charEnum =>
                {
                    IEnumerable<GVariant> genericArgs;
                    (genericArgs, charEnum) = ConsumeUntil(charEnum, CloseRoundBracket);
                    GVariant tuple = genericArgs.Count() == 0
                        ? new GEmptyTuple()
                        : new GTuple(genericArgs);
                    return (tuple, charEnum);
                },

                '}' => charEnum => (CloseCurlyBracket, charEnum),

                '{' => charEnum =>
                {
                    IEnumerable<GVariant> genericArgs;
                    (genericArgs, charEnum) = ConsumeUntil(charEnum, CloseCurlyBracket);
                    return (new GDict(genericArgs), charEnum);
                },

                _ => charEnum => (GPrimitive.Parse(c), charEnum)
            };

            return consume(charEnum);
        }

        public static GVariant Parse(string typeString)
        {
            if (string.IsNullOrEmpty(typeString))
            {
                return None;
            }

            var (v, charEnum) = Consume(typeString.GetEnumerator());
            if (v is GSentinel) { throw new ParseException("We should not have sentinel values here"); }
            if (charEnum.MoveNext()) { throw new ParseException("We should have consumed all chars"); }
            return v;
        }
    }
}
