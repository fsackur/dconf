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
    public class GArray<T> : GVariant<T> where T : IEnumerable<GVariant>
    {
        public GArray(T value) : base(value) {}
    }
}
