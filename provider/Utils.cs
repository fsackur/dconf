using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dconf
{
    public static class Utils
    {
        public static string[] ToChunks(string path)
        {
            var chunks = path.Split(new char[] { '/', '.' }, StringSplitOptions.RemoveEmptyEntries);
            return chunks.Length > 0 ? chunks : [string.Empty];
        }

        public static string Normalize(string path) => string.Join('/', ToChunks(path));

        public static string ToSchemaName(string path) => string.Join('.', ToChunks(path));

        public static string GetParent(string path) => string.Join('.', ToChunks(path).SkipLast(1));

        public static string GetName(string path) => ToChunks(path).Last();
    }
}
