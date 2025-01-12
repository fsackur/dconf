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
    public struct Maybe<T>
    {
        public static Maybe<T> None => default;
        public static Maybe<T> Some(T value) => new Maybe<T>(value);

        readonly bool isSome;
        readonly T value;

        Maybe(T value)
        {
            this.value = value;
            isSome = this.value is { };
        }

        public bool IsSome(out T value)
        {
            value = this.value;
            return isSome;
        }
    }

    public class GMaybe : GVariant
    {
        public GMaybe(GVariant genericArg)
        {
            GenericArg = genericArg;
            ManagedType = typeof(Maybe<>).MakeGenericType([ genericArg.ManagedType ]);
        }

        private GVariant GenericArg { get; init;}

        public Type ManagedType { get; init; }

        public object? Deserialize(string encoded)
        {
            return encoded == "@ms nothing"
                ? ManagedType.GetProperty("None")!.GetValue(ManagedType)
                : ManagedType.GetMethod("Some")!.Invoke(ManagedType, new object[] { GenericArg.Deserialize(encoded)! });
        }
    }
}
