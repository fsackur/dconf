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
    internal class ParsingFailed : Exception {
        internal ParsingFailed(string msg) : base(msg) {}
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
        public abstract object? Deserialize(string encoded);
    }

    public class GPrimitive : GVariant
    {
        private readonly static Dictionary<char, GPrimitive> _primitives;

        static GPrimitive()
        {
            Regex unquotePattern = new("""^(['\"])?(?<unquoted>.*)\1$""");
            Func<string, string> unquote = s => unquotePattern.Replace(s, "${unquoted}");

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

        public static Type ParseTypeChar(char gChar)
        {
            return _primitives.TryGetValue(gChar, out GPrimitive? prim)
                ? prim.ManagedType
                : throw new ParsingFailed($"Not a primitive: {gChar}");
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

    public class GVariantParser
    {
        private class None { }

        private class CloseBracket {}

        private static (Type, IEnumerator<char>) Consume(IEnumerator<char> charEnum, char? marker = null)
        {
            if (!charEnum.MoveNext())
            {
                return (typeof(None), charEnum);
            }

            char c = charEnum.Current;
            Func<IEnumerator<char>, (Type, IEnumerator<char>)> consume = c switch
            {
                'a' => charEnum =>
                {
                    Type t;
                    (t, charEnum) = Consume(charEnum);
                    t = t.MakeArrayType();
                    return (t, charEnum);
                },

                'm' => charEnum =>
                {
                    Type t;
                    (t, charEnum) = Consume(charEnum);
                    Type[] genericArgs = [t];
                    t = typeof(Maybe<>).MakeGenericType(genericArgs);
                    return (t, charEnum);
                },

                '}' or ')' => charEnum => (typeof(CloseBracket), charEnum),

                '(' or '{' => charEnum =>
                {
                    char newMarker = c switch { '(' => ')', '{' => '}',
                        _ => throw new ParsingFailed("Brackets mysteriously switched")};

                    List<Type> genericArgs = new();
                    while (true)
                    {
                        Type genT;
                        (genT, charEnum) = Consume(charEnum, newMarker);
                        if (genT == typeof(None)) { throw new ParsingFailed($"Expecting {newMarker}"); }
                        if (genT == typeof(CloseBracket)) { break; }
                        genericArgs.Add(genT);
                    }

                    Type t = c switch
                    {
                        '(' when genericArgs.Count == 0 => typeof(Tuple),
                        '(' => typeof(Tuple)
                            .GetMethods()
                            .Where(m => m.Name == "Create" && m.GetParameters().Count() == genericArgs.Count())
                            .First()
                            .ReturnType
                            .GetGenericTypeDefinition()
                            .MakeGenericType(genericArgs.ToArray()),
                        _ => typeof(Dictionary<,>)
                            .MakeGenericType(genericArgs.ToArray()),
                    };
                    return (t, charEnum);
                },

                _ => charEnum => (GPrimitive.ParseTypeChar(c), charEnum)
            };

            return consume(charEnum);
        }

        public static Type ParseTypeString(string typeString)
        {
            if (string.IsNullOrEmpty(typeString))
            {
                return typeof(void);
            }

            var (type, charEnum) = Consume(typeString.GetEnumerator());
            if (type == typeof(None)) { throw new ParsingFailed("We should not have sentinel values here"); }
            if (charEnum.MoveNext()) { throw new ParsingFailed("We should have consumed all chars"); }
            return type;
        }
    }

    public class GEnumBuilder
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

        public static Type BuildEnum(string name, IEnumerable<KeyValuePair<string, int>> members, bool isFlags = false)
        {
            if (GetExistingEnum(name, members, isFlags) is Type type)
            {
                return type;
            }

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

            return eb.CreateType();
        }

        public static Type BuildEnum(string name, IEnumerable<string> members, bool isFlags = false)
        {
            Dictionary<string, int> memberDict = new(members.Count());
            var i = 0;
            foreach (string member in members)
            {
                memberDict.Add(member, i);
                i++;
            }
            return BuildEnum(name, memberDict, isFlags);
        }
    }
}
