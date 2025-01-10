using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

namespace GVariant
{
    public class Parser
    {
        #region Sentinels
        private class GSentinel : GVariant {}
        private class GNone : GSentinel {}
        private class GCloseBracket : GSentinel {}
        private static readonly GVariant None = new GNone();
        private static readonly GCloseBracket CloseRoundBracket = new GCloseBracket();
        private static readonly GCloseBracket CloseCurlyBracket = new GCloseBracket();
        #endregion Sentinels

        private static (IEnumerable<GVariant>, IEnumerator<char>) ConsumeUntil(IEnumerator<char> charEnum, GCloseBracket marker)
        {
            List<GVariant> result = new();
            while (true)
            {
                GVariant v;
                (v, charEnum) = Consume(charEnum);
                if (v == None) { throw new ParseException($"Expecting {marker}"); }
                if (v == marker) { break; }
                result.Add(v);
            }
            return (result, charEnum);
        }

        private static (GVariant, IEnumerator<char>) Consume(IEnumerator<char> charEnum)
        {
            if (!charEnum.MoveNext())
            {
                return (None, charEnum);
            }

            char c = charEnum.Current;
            Func<IEnumerator<char>, (GVariant, IEnumerator<char>)> consume = c switch
            {
                _ => charEnum => (GPrimitive.Parse(c), charEnum)
            };

            return consume(charEnum);
        }

        public static GVariant Parse(string typeString)
        {
            if (string.IsNullOrEmpty(typeString))
            {
                return None;
            }

            var (v, charEnum) = Consume(typeString.GetEnumerator());
            if (v is GSentinel) { throw new ParseException("We should not have sentinel values here"); }
            if (charEnum.MoveNext()) { throw new ParseException("We should have consumed all chars"); }
            return v;
        }
    }
}
