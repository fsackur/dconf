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
    }

    public interface GType<out T> where T : GVariant
    {
        public static GType<T> Create() => new GTypeImpl<T>();

        public Type ManagedType { get => typeof(T).BaseType!.GetGenericArguments()[0]; }
    }

    public class GTypeImpl<T> : GType<T> where T : GVariant {}

    public abstract class GVariant
    {
        public abstract object Value { get; init; }

        public abstract Type ManagedType { get; }
    }
}
