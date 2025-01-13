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
        // TODO: needs a full parser, or we'll get caught out by strings containing commas

        private static readonly Regex unquotePattern = new("""^(['\"])?(?<unquoted>.*)\1$""");
        private static readonly Regex arrayPattern = new("""^\[(?<contents>.*)\]$""");
        private static readonly Regex dictPattern = new("""^\{(?<contents>.*)\}$""");
        private static readonly Regex tuplePattern = new("""^\((?<contents>.*)\)$""");
        private static readonly Regex commaPattern = new(@",\s*");
        private static readonly Regex colonPattern = new(@":\s*");
        internal static string Unquote(string s) => unquotePattern.Replace(s, "${unquoted}");

        internal static string StripArray(string s) => arrayPattern.Replace(s, "${contents}");
        internal static string StripDict(string s) => dictPattern.Replace(s, "${contents}");
        internal static string StripTuple(string s) => tuplePattern.Replace(s, "${contents}");
        internal static string[] SplitOnComma(string s)
        {
            return string.IsNullOrWhiteSpace(s)
                ? new string[0]
                : commaPattern.Split(s);
        }

        internal static (string, string) SplitKeyValuePair(string s)
        {
            var items = colonPattern.Split(s, 2);
            return items.Length == 2
                ? (items[0], items[1])
                : throw new ParseException($"Expected a colon-spearated key-value pair, but got '{s}");
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
                    if (genericArgs.Count() < 1)
                    {
                        throw new ParseException($"Expected at least 1 generic arg for {typeof(GTuple)}");
                    }
                    return (new GTuple(genericArgs), charEnum);
                },

                '}' => charEnum => (CloseCurlyBracket, charEnum),

                '{' => charEnum =>
                {
                    IEnumerable<GVariant> genericArgs;
                    (genericArgs, charEnum) = ConsumeUntil(charEnum, CloseCurlyBracket);
                    var genArgs = genericArgs.ToArray();
                    if (genArgs.Length != 2)
                    {
                        throw new ParseException($"Expected exactly 2 generic args for {typeof(GDict)}.");
                    }
                    return (new GDict(genArgs[0], genArgs[1]), charEnum);
                },

                _ => charEnum => (GPrimitive.Parse(c), charEnum)
            };

            return consume(charEnum);
        }

        public static GVariant ParseType(string typeString)
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

        private static string Pop(IEnumerable<char> chars) => new(chars);

        private static (Token, IEnumerator<char>) Consume(
            IEnumerator<char> charEnum,
            List<char> buffer,
            char? endMarker = null
        )
        {
            if (!charEnum.MoveNext())
            {
                return (null, charEnum);
            }

            bool isQuoted = endMarker == '\'' || endMarker == '"';
            bool bufferIsEmpty = buffer.Count() == 0;
            char c = charEnum.Current;

            Func<IEnumerator<char>, (Token, IEnumerator<char>)> consume;
            consume = c switch
            {
                endMarker => charEnum => (PopBuffer(buffer), charEnum),

                ' ' when !isQuoted && bufferIsEmpty => charEnum => Consume(charEnum, buffer, endMarker),

                ' ' when !isQuoted => charEnum => (PopBuffer(buffer), charEnum),

                '\'' or '"' => charEnum => Consume(charEnum, new(), endMarker: c),

                '@' when !isQuoted && bufferIsEmpty => charEnum =>
                {
                    Token hint;
                    (hint, charEnum) = Consume(charEnum, buffer, endMarker: c);
                    Token value;
                    (value, charEnum) = Consume(charEnum, new(), endMarker: c);
                    return (new HintedToken(value.Value, ParseType(hint.Value)), charEnum);
                },

                ',' when !isQuoted => (PopBuffer(buffer), charEnum),

                _ => charEnum =>
                {
                    buffer.Add(c);
                    return Consume(charEnum, buffer, endMarker);
                }
            };

            return consume(charEnum);
        }

        public static GVariant Parse(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
            {
                return None;
            }

            var (v, charEnum) = Consume(encoded.GetEnumerator());
            if (charEnum.MoveNext()) { throw new ParseException("We should have consumed all chars"); }
            return v;
        }
    }
}
