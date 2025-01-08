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

    public class GPrimitive
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
                primMembers.Select<GPrimitive>(args => new GPrimitive(
                                gChar: args.Item1,
                                type: args.Item2,
                                deserializer: args.Item3))
                           .Select(prim => new KeyValuePair<char, GPrimitive>(prim.GChar, prim))
            );
        }

        public GPrimitive(char gChar, Type type, Func<string, object> deserializer)
        {
            GChar = gChar;
            ManagedType = type;
            Deserialize = deserializer;
        }

        public char GChar { get; init; }

        public Type ManagedType { get; init; }

        public Func<string, object> Deserialize { get; init; }
    }

    public class GVariantParser
    {
        private class None { }

        private static Type FromChar(char c) => c switch
        {
            // https://docs.gtk.org/glib/gvariant-format-strings.html
            'b' => typeof(Boolean),
            'y' => typeof(Char),
            'n' => typeof(Int16),
            'q' => typeof(UInt16),
            'i' => typeof(Int32),
            'u' => typeof(UInt32),
            'x' => typeof(Int64),
            't' => typeof(UInt64),
            'h' => typeof(Int32),  // handle..?
            'd' => typeof(Double),
            'v' => typeof(Object),  // pointer to variant..?
            's' or 'o' or 'g' => typeof(String),
            _ => typeof(None)
        };

        private static object DeserialisePrimitive(char typeChar, string value)
        {

        }

        internal class ParsingFailed : Exception {}

        private static (Type, IEnumerator<char>) Consume(IEnumerator<char> charEnum, char? marker = null)
        {
            if (!charEnum.MoveNext())
            {
                return (typeof(None), charEnum);
            }

            char c = charEnum.Current;
            Type t = FromChar(c);
            if (t != typeof(None))
            {
                return (t, charEnum);
            }

            if (c == 'a')
            {
                (t, charEnum) = Consume(charEnum);
                t = t.MakeArrayType();
                return (t, charEnum);
            }

            if (c == 'm')
            {
                (t, charEnum) = Consume(charEnum);
                Type[] types = [t];
                t = typeof(Maybe<>).MakeGenericType(types);
                return (t, charEnum);
            }

            if (c is '(' || c is '{')
            {
                char newMarker = c switch { '(' => ')', _ => '}' };
                List<Type> types = new();
                while (true)
                {
                    (t, charEnum) = Consume(charEnum, newMarker);
                    if (t == typeof(None)) {
                        throw new ParsingFailed()
                    }
                    types.Add(t);
                }

                t = c switch
                {
                    '(' when types.Count == 0 => typeof(Tuple),
                    '(' => typeof(Tuple)
                        .GetMethods()
                        .Where(m => m.Name == "Create" && m.GetParameters().Count() == types.Count())
                        .First()
                        .ReturnType
                        .GetGenericTypeDefinition()
                        .MakeGenericType(types.ToArray()),
                    _ => typeof(Dictionary<,>)
                        .MakeGenericType(types.ToArray()),
                };
                return (t, charEnum);
            }

            if (c == marker)
            {
                return (typeof(None), charEnum);
            }

            throw new InvalidOperationException($"Failed to parse '{c}' as a GVariant type.");
        }

        public static Type Parse(string typeString)
        {
            if (string.IsNullOrEmpty(typeString))
            {
                return typeof(void);
            }

            var (type, charEnum) = Consume(typeString.GetEnumerator());
            Debug.Assert(type != typeof(None), "We should not have sentinel values here");
            Debug.Assert(!charEnum.MoveNext(), "We should have consumed all chars");
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
