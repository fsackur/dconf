using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

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

    public class GVariantParser
    {
        private class Sentinel {}

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
            _ => typeof(Sentinel)
        };

        private static (Type, IEnumerator<char>) Consume(IEnumerator<char> charEnum, char? marker = null)
        {
            if (!charEnum.MoveNext())
            {
                return (typeof(Sentinel), charEnum);
            }

            char c = charEnum.Current;
            Type t = FromChar(c);
            if (t != typeof(Sentinel))
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
                Type[] types = [ t ];
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
                    if (t == typeof(Sentinel)) { break; }
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
                return (typeof(Sentinel), charEnum);
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
            Debug.Assert(type != typeof(Sentinel), "We should not have sentinel values here");
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

        public static Type BuildEnum(string name, IEnumerable<KeyValuePair<string, int>> members, bool isFlags = false)
        {
            EnumBuilder eb = Module.DefineEnum(name, TypeAttributes.Public, typeof(int));
            foreach (var kvp in members)
            {
                eb.DefineLiteral(kvp.Key, kvp.Value);
            }

            if (isFlags)
            {
                Type flagType = typeof(FlagsAttribute);
                var flagCtor = flagType.GetConstructor(new Type[0]);
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
