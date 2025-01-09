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
        private static Regex unquotePattern = new("""^(['\"])?(?<unquoted>.*)\1$""");
        private static Regex arrayPattern = new("""^(\[)(?<contents>.*)\1$""");
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

    public struct Maybe<T>
    {
        public static Maybe<T> None => default;
        public static Maybe<T> Some(T value) => new Maybe<T>(value);

        readonly bool isSome;
        readonly T value;

        Maybe(T value)
        {
            this.value = value;
            isSome = this.value is { };
        }

        public bool IsSome(out T value)
        {
            value = this.value;
            return isSome;
        }
    }

    public interface GVariant
    {
        public Type ManagedType { get; }

        public object? Deserialize(string encoded);
    }

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

    public class GMaybe : GVariant
    {
        public GMaybe(GVariant genericArg)
        {
            ManagedType = typeof(Maybe<>).MakeGenericType([ genericArg.ManagedType ]);
        }

        public Type ManagedType { get; init; }

        public object? Deserialize(string encoded) => throw new NotImplementedException();
    }

    public class GArray : GVariant
    {
        public GArray(GVariant genericArg)
        {
            ManagedType = genericArg.ManagedType.MakeArrayType();
        }

        public Type ManagedType { get; init; }

        public object? Deserialize(string encoded) => throw new NotImplementedException();
    }

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

    public class GEnum : GVariant
    {
        private static ModuleBuilder? module;

        protected static ModuleBuilder Module
        {
            get
            {
                if (module is not ModuleBuilder)
                {
                    string name = "Dconf.Dynamic";
                    var assemblyName = new AssemblyName(name);
                    var ab = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                    module = ab.DefineDynamicModule(assemblyName.Name!);
                }
                return module;
            }
        }

        public static bool IsFlags(GVariant v) => IsFlags(v.ManagedType);

        public static bool IsFlags(Type t) => t.GetCustomAttributes(false).Any(a => a is FlagsAttribute);

        private static Type? GetExistingEnum(string name, IEnumerable<KeyValuePair<string, int>> members, bool isFlags = false)
        {
            if (Module.GetType(name) is not Type type)
            {
                return null;
            }

            var memberDict = new Dictionary<string, int>(members);
            var enumValues = Enum.GetValues(type);

            bool definitionsMatch = true;
            foreach (var enumValue in enumValues)
            {
                bool valueMatches =
                    memberDict.TryGetValue(enumValue.ToString()!, out int paramValue) &&
                    (int)enumValue == paramValue;

                if (!valueMatches) { definitionsMatch = false; break; }
            }

            definitionsMatch =
                definitionsMatch &&
                enumValues.Length == members.Count() &&
                isFlags == type.GetCustomAttributes(false).Any(a => a is FlagsAttribute);

            if (definitionsMatch)
            {
                return type;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot create enum '{name}'; type already exists and does not match provided definition."
                );
            }
        }

        public static GEnum Build(string name, IEnumerable<KeyValuePair<string, int>> members, bool isFlags = false)
        {
            if (GetExistingEnum(name, members, isFlags) is not Type type)
            {
                EnumBuilder eb = Module.DefineEnum(name, TypeAttributes.Public, typeof(int));
                foreach (var kvp in members)
                {
                    eb.DefineLiteral(kvp.Key, kvp.Value);
                }

                if (isFlags)
                {
                    var flagCtor = typeof(FlagsAttribute).GetConstructor(new Type[0]);
                    eb.SetCustomAttribute(flagCtor!, new byte[0]);
                }

                type = eb.CreateType();
            }

            return new GEnum(type);
        }

        public static GEnum Build(string name, IEnumerable<string> members)
        {
            Dictionary<string, int> memberDict = new(members.Count());
            var i = 0;
            foreach (string member in members)
            {
                memberDict.Add(member, i);
                i++;
            }
            return Build(name, memberDict, false);
        }

        protected GEnum(Type managedType)
        {
            ManagedType = managedType;
        }

        public Type ManagedType { get; init; }

        public object? Deserialize(string encoded)
        {
            string[] encodedArray = IsFlags(this)
                ? GVariantUtils.SplitEncodedArray(Regex.Replace(encoded, @"^@as\s+", ""))
                : [ encoded ];

            var unquote = GVariantUtils.Unquote;
            string names = string.Join(',', encodedArray.Select(unquote));

            return Enum.TryParse(ManagedType, names, out object? result)
                ? result
                : throw new ParseException($"{names} is not a valid case for {ManagedType}");
        }
    }
}
