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
    public abstract class GPrimitive<T> : GVariant where T: IParsable<T>
    {
        public GPrimitive(T value) => Value = value;

        public GPrimitive(string encoded)
        {
            Value = T.Parse(encoded, CultureInfo.InvariantCulture);
        }

        public override object Value { get; init; }

        public override Type ManagedType { get => typeof(T); }
    }

    public class GBool : GPrimitive<bool>
    {
        public GBool(bool value) : base(value) {}
        public GBool(string value) : base(value) {}
    }

    public class GChar : GPrimitive<char>
    {
        public GChar(char value) : base(value) {}
        public GChar(string value) : base(value) {}
    }

    public class GInt16 : GPrimitive<Int16>
    {
        public GInt16(Int16 value) : base(value) {}
        public GInt16(string value) : base(value) {}
    }

    public class GUInt16 : GPrimitive<UInt16>
    {
        public GUInt16(UInt16 value) : base(value) {}
        public GUInt16(string value) : base(value) {}
    }

    public class GInt32 : GPrimitive<Int32>
    {
        public GInt32(Int32 value) : base(value) {}
        public GInt32(string value) : base(value) {}
    }

    public class GUInt32 : GPrimitive<UInt32>
    {
        public GUInt32(UInt32 value) : base(value) {}
        public GUInt32(string value) : base(value) {}
    }

    public class GInt64 : GPrimitive<Int64>
    {
        public GInt64(Int64 value) : base(value) {}
        public GInt64(string value) : base(value) {}
    }

    public class GUInt64 : GPrimitive<UInt64>
    {
        public GUInt64(UInt64 value) : base(value) {}
        public GUInt64(string value) : base(value) {}
    }

    public class GDouble : GPrimitive<Double>
    {
        public GDouble(Double value) : base(value) {}
        public GDouble(string value) : base(value) {}
    }

    public class GString : GPrimitive<string>
    {
        public GString(string value) : base(GVariantUtils.Unquote(value)) {}
    }
}
