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
    public class GArray<T> : GVariant, GVariant<T>
    {
        public T Deserialize(string encoded) => throw new NotImplementedException();
    }
}
