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

            return isFlags ? new GFlagsEnum(type) : new GEnum(type);
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

        public GEnum(Type managedType) => ManagedType = managedType;

        public Type ManagedType { get; init; }

        public virtual object? Deserialize(string encoded)
        {
            string name = GVariantUtils.Unquote(encoded);
            return Enum.TryParse(ManagedType, name, out object? result)
                ? result
                : throw new ParseException($"{name} is not a valid case for {ManagedType}");
        }
    }

    public class GFlagsEnum : GEnum
    {
        public GFlagsEnum(Type managedType) : base(managedType) {}

        public override object? Deserialize(string encoded)
        {
            encoded = Regex.Replace(encoded, @"^@as\s+", "");
            string[] encodedArray = GVariantUtils.SplitEncodedArray(encoded);

            var names = encodedArray.Select(GVariantUtils.Unquote);
            foreach (string n in names)
            {
                if (!Enum.TryParse(ManagedType, n, out _))
                {
                    throw new ParseException($"{n} is not a valid case for {ManagedType}");
                }
            }
            return Enum.Parse(ManagedType, string.Join(',', names));
        }
    }
}
