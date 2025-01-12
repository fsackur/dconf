using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Globalization;

namespace GVariant
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

    public abstract class GVariant {}

    public interface GVariant<out T> {}
}
