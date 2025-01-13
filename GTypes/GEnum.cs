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
    public abstract class GEnum : GVariant
    {
        internal GEnum(object value) : base(value) {}

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

        public static GType<GEnum> Build(string name, IEnumerable<KeyValuePair<string, int>> members, bool isFlags = false)
        {
            if (GetExistingEnum(name, members, isFlags) is not Type managedType)
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

                managedType = eb.CreateType();
            }

            Type genDef = isFlags ? typeof(GFlagsEnum<>) : typeof(GEnum<>);
            Type T = genDef.MakeGenericType(new Type[] { managedType });
            return (GType<GEnum>)GVariantUtils.MakeGType(T);
            // var ctor = genType.GetConstructor(new Type[0])!;
            // return (GEnum)ctor.Invoke(new object[0]);
        }

        public static GType<GEnum> Build(string name, IEnumerable<string> members)
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
    }

    public class GEnum<T> : GEnum where T : Enum
    {
        public static T Parse(string encoded, bool ignoreCase = false)
        {
            string name = GVariantUtils.Unquote(encoded);
            return Enum.TryParse(typeof(T), name, ignoreCase, out object? result)
                ? (T)result
                : throw new ParseException($"{name} is not a valid case for {typeof(T)}");
        }

        public GEnum(T value) : base(value) => Value = value;

        public GEnum(string encoded, bool ignoreCase = false) : this(Parse(encoded, ignoreCase)) {}

        public override object Value { get; init; }

        public override Type ManagedType { get => typeof(T); }
    }

    public class GFlagsEnum<T> : GEnum<T> where T : Enum
    {
        public new static T Parse(string encoded, bool ignoreCase = false)
        {
            encoded = Regex.Replace(encoded, @"^@a?s\s+", "");
            string[] encodedArray = GVariantUtils.SplitEncodedArray(encoded);

            var names = encodedArray.Select(GVariantUtils.Unquote);
            foreach (string n in names)
            {
                if (!Enum.TryParse(typeof(T), n, ignoreCase, out _))
                {
                    throw new ParseException($"{n} is not a valid case for {typeof(T)}");
                }
            }
            return (T)Enum.Parse(typeof(T), string.Join(',', names), ignoreCase);
        }

        public GFlagsEnum(T value) : base(value) {}

        public GFlagsEnum(string encoded, bool ignoreCase = false) : base(Parse(encoded, ignoreCase)) {}
    }
}
