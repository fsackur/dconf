using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Globalization;

namespace GTypes
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

        internal static GType<GVariant> MakeGType(Type T)
        {
            Type TResult = typeof(GType<>).MakeGenericType(T);
            var Create = TResult.GetMethod("Create")!;
            return (GType<GVariant>)Create.Invoke(null, new object[0])!;
        }
    }

    public interface GType<out T> where T : GVariant
    {
        public static GType<T> Create() => new GTypeImpl<T>();

        public Type ManagedType
        {
            get
            {
                // every GVariant is either generic or descends from a generic type
                Type? t = typeof(T);
                while (t != null && !t.IsGenericType) { t = t.BaseType; }

                return t is not null
                    ? t.GetGenericArguments().First()
                    : throw new ParseException($"Type {typeof(T)} is not generic and has no generic base.");
            }
        }
    }

    public class GTypeImpl<T> : GType<T> where T : GVariant {}

    public abstract class GVariant
    {
        public GVariant() => Value = null!;

        public GVariant(object value) => Value = value;

        public virtual object Value { get; init; }

        public abstract Type ManagedType { get; }
    }

    public class GVariant<T> : GVariant
    {
        public GVariant(T value) : base(value!) => Value = value;

        public new virtual T Value { get; init; }

        public override Type ManagedType { get => typeof(T); }
    }
}
