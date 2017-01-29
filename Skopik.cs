using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Skopik
{
    internal static class Skopik
    {
        internal static readonly string[] Keywords = { "true", "false", "null" };

        internal static readonly string[] CommentLineKeys = { "//" };

        internal static readonly string CommentBlockOpenKey = "/*";
        internal static readonly string CommentBlockCloseKey = "*/";

        internal static readonly char AssignmentKey = '=';
        internal static readonly char ScopeBlockKey = ':'; // opening of a scope block

        internal static readonly char[] ControlKeys = { '{', '}', '[', ']' };
        internal static readonly char[] OperatorKeys = { '@', '"', '\'' };
        internal static readonly char[] SeparatorKeys = { ',', ';' };

        internal static readonly string[] PrefixKeys = { "0x" };
        internal static readonly string[] SuffixKeys = { "b", "d", "f", "u", "U", "L", "uL" };

        internal static readonly SkopikDataType[] WordLookup = {
            SkopikDataType.Boolean,     // 'true'
            SkopikDataType.Boolean,     // 'false'
            SkopikDataType.Null,        // 'null'
        };

        internal static readonly SkopikDataType[] ControlLookup = {
            SkopikDataType.Scope,       // '{}'
            SkopikDataType.Array,       // '[]'
        };

        internal static readonly SkopikDataType[] OperatorLookup = {
            SkopikDataType.Reserved,    // '@'
            SkopikDataType.String,      // ' " '
            SkopikDataType.String,      // ' ' '
        };

        internal static readonly SkopikDataType[] SuffixLookup = {
            SkopikDataType.Integer32,   // 'b'
            SkopikDataType.Double,      // 'd'
            SkopikDataType.Float,       // 'f'
            SkopikDataType.UInteger32,  // 'u'
            SkopikDataType.UInteger32,  // 'U'
            SkopikDataType.Integer64,   // 'L'
            SkopikDataType.UInteger64   // 'uL'
        };

        internal static string StripDataTypeSuffix(string value)
        {
            var val = new StringBuilder(value.Length);

            foreach (var c in value)
            {
                // eww
                if (!CharUtils.HasCharFlags(c, CharacterTypeFlags.Digit | CharacterTypeFlags.Control) && SuffixKeys.Contains(c.ToString()))
                    continue;

                val.Append(c);
            }

            return val.ToString();
        }

        internal static SkopikDataType GetDataTypeFromSuffix(string value)
        {
            for (int i = 0; i < SuffixKeys.Length; i++)
            {
                if (value.EndsWith(SuffixKeys[i]))
                    return SuffixLookup[i];
            }

            return SkopikDataType.Invalid;
        }

        internal static SkopikDataType GetOperatorDataType(string value)
        {
            for (int i = 0; i < OperatorKeys.Length; i++)
            {
                var op = OperatorKeys[i];

                if (value[0] == op)
                    return OperatorLookup[i];
            }

            return SkopikDataType.Invalid;
        }

        internal static SkopikDataType GetControlDataType(string value)
        {
            for (int i = 0; i < ControlKeys.Length; i += 2)
            {
                var openTag = ControlKeys[i];
                var closeTag = ControlKeys[i + 1];

                if (value[0] == openTag || value[0] == closeTag)
                    return ControlLookup[(i > 1) ? 1 : 0]; // oh god the horror
            }

            return SkopikDataType.Invalid;
        }

        internal static SkopikDataType GetWordDataType(string value)
        {
            for (int i = 0; i < Keywords.Length; i++)
            {
                var k = Keywords[i];

                if (value.StartsWith(k))
                    return WordLookup[i];
            }

            return SkopikDataType.Invalid;
        }

        internal static SkopikDataType GetNumberDataType(string value)
        {
            var numberType = GetDataTypeFromSuffix(value);

            if (numberType == SkopikDataType.Invalid)
            {
                var isNegative = false;
                var hasExponent = false;
                var hasDigit = false;
                var hasSeparator = false; // floats

                foreach (var c in value)
                {
                    if (CharUtils.HasCharFlags(c, CharacterTypeFlags.Digit))
                    {
                        hasDigit = true;
                        continue;
                    }
                    if ((c == '.') || (c == ','))
                    {
                        if (!hasDigit || hasSeparator)
                            Console.WriteLine($"-- Malformed number data: '{value}'");

                        hasSeparator = true;
                        continue;
                    }
                    if ((c == 'e') || (c == 'E'))
                    {
                        // check for exponential float
                        if (hasExponent || (!hasDigit || (!hasDigit && !hasSeparator)))
                            Console.WriteLine($"-- Malformed number data: '{value}'");

                        hasExponent = true;
                        continue;
                    }
                    if (c == '-')
                    {
                        if (hasExponent)
                        {
                            if (!hasDigit || (!hasDigit && !hasSeparator))
                                Console.WriteLine($"-- Malformed number data: '{value}'");

                            // exponential float
                            // just continue normally
                            continue;
                        }
                        else
                        {
                            if (isNegative || hasDigit || hasSeparator)
                                Console.WriteLine($"-- Malformed number data: '{value}'");

                            // negative number
                            isNegative = true;
                            continue;
                        }
                    }
                }

                if (hasDigit)
                {
                    // NOTE: Doubles are implicitly assumed for decimal values (e.g. 1.0 is a double, NOT a float)
                    if (hasSeparator)
                    {
                        if (hasExponent)
                            Console.WriteLine($"-- Successfully detected an exponential decimal: '{value}'");

                        numberType = SkopikDataType.Double;
                    }
                    else
                    {
                        // TODO: Return UInt32 for non-negative numbers?
                        numberType = SkopikDataType.Integer32;
                    }
                }
            }

            // can still be invalid
            return numberType;
        }

        internal static SkopikDataType GetDataType(string value)
        {
            // check operators first
            var dataType = GetOperatorDataType(value);

            // check controls next
            if (dataType == SkopikDataType.Invalid)
            {
                dataType = GetControlDataType(value);

                // check word types next
                if (dataType == SkopikDataType.Invalid)
                    dataType = GetWordDataType(value);

                // check number types
                if (dataType == SkopikDataType.Invalid)
                    dataType = GetNumberDataType(value);
            }

            // may be invalid
            return dataType;
        }

        internal static bool IsAssignmentOperator(string value)
        {
            return (value[0] == AssignmentKey);
        }

        internal static bool IsScopeBlockOperator(string value)
        {
            return (value[0] == ScopeBlockKey);
        }

        internal static bool IsSeparator(string value, bool inArray)
        {
            return (value[0] == ((inArray) ? SeparatorKeys[0] : SeparatorKeys[1]));
        }

        internal static bool IsOpeningBrace(string value)
        {
            for (int i = 0; i < ControlKeys.Length; i += 2)
            {
                var k = ControlKeys[i];

                if (value[0] == k)
                    return true;
            }

            return false;
        }

        internal static bool IsClosingBrace(string value)
        {
            for (int i = 0; i < ControlKeys.Length; i += 2)
            {
                var k = ControlKeys[i + 1];

                if (value[0] == k)
                    return true;
            }

            return false;
        }

        internal static bool IsCommentLine(string value)
        {
            for (int i = 0; i < CommentLineKeys.Length; i++)
            {
                var k = CommentLineKeys[i];

                if (value.StartsWith(k))
                    return true;
            }

            return false;
        }

        internal static bool IsCommentBlock(string value, bool isOpen)
        {
            if (value.StartsWith((!isOpen) ? CommentBlockOpenKey : CommentBlockOpenKey) || value.EndsWith(CommentBlockCloseKey))
                return true;

            return false;
        }

        internal static bool IsNumberValue(SkopikDataType dataType)
        {
            switch (dataType)
            {
            case SkopikDataType.Integer32:
            case SkopikDataType.Integer64:
            case SkopikDataType.UInteger64:
            case SkopikDataType.UInteger32:
            case SkopikDataType.Float:
            case SkopikDataType.Double:
                return true;
            }

            return false;
        }
    }
}
