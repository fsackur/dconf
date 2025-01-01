using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Reflection;

namespace Dconf
{
    public class KeyReader : IContentReader
    {
        private GSettings gsettings;
        private KeyInfo key;
        private bool isOpen;
        internal KeyReader(GSettings gsettings, KeyInfo key)
        {
            this.gsettings = gsettings;
            this.key = key;
            isOpen = true;
        }

        public IList Read(long readCount)
        {
            if (!isOpen || readCount == 0L)
            {
                return new string[0];
            }

            var value = gsettings.Get(key.Schema, key.Name);
            isOpen = false;
            return value;
        }

        public void Seek(long offset, SeekOrigin origin) {}

        public void Close() => isOpen = false;

        public void Dispose() => Close();
    }

    public partial class DconfProvider
    {
        public IContentReader? GetContentReader(string path)
        {
            var item = Get(path);
            if (item is not KeyInfo key)
            {
                WriteError(new ErrorRecord(
                    new InvalidOperationException($"Unable to read '{path}' because it is not a key."),
                    "NotAKey",
                    ErrorCategory.InvalidOperation,
                    path)
                );
                return null;
            }

            return new KeyReader(Drive.GSettings, key);
        }

        public object? GetContentReaderDynamicParameters(string path) => null;

        public IContentWriter? GetContentWriter(string path) => null;

        public object? GetContentWriterDynamicParameters(string path) => null;

        public void ClearContent(string path) {}

        public object? ClearContentDynamicParameters(string path) => null;
    }
}
