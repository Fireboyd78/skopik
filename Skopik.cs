using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Skopik
{
    internal static class Skopik
    {
        private static bool m_lookupReady = false;
        private static int[] m_lookup = new int[128];

        internal static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
        
        internal static readonly string[] Keywords = { "true", "false", "null" };

        internal static readonly string CommentLineKey = "//";
        internal static readonly string CommentBlockOpenKey = "/*";
        internal static readonly string CommentBlockCloseKey = "*/";

        internal static readonly char AssignmentKey = '=';
        internal static readonly char ScopeBlockKey = ':'; // opening of a scope block

        internal static readonly char[] BlockKeys = {
            '{', '}',
            '[', ']',
            '(', ')',
        };

        internal static readonly char[] OperatorKeys = { '@', '"', '\'' };
        internal static readonly char[] DelimiterKeys = { ',', ';' };

        internal static readonly char[] SuffixKeys = { 'b', 'd', 'f', 'u', 'U', 'L' };
        internal static readonly char[] ExponentialKeys = { 'e', 'E' };

        internal static readonly char NegativePrefixKey = '-';
        internal static readonly char DecimalPointKey = '.';
        
        internal static readonly string HexadecimalPrefix = "0x";

        internal static readonly SkopikDataType[] WordLookup = {
            SkopikDataType.Boolean,     // 'true'
            SkopikDataType.Boolean,     // 'false'
            SkopikDataType.Null,        // 'null'
        };

        internal static readonly SkopikDataType[] BlockLookup = {
            SkopikDataType.Scope,       // '{}'
            SkopikDataType.Array,       // '[]'
            SkopikDataType.Tuple,       // '()'
        };

        internal static readonly SkopikDataType[] OperatorLookup = {
            SkopikDataType.Keyword,     // '@'
            SkopikDataType.String,      // ' " '
            SkopikDataType.String,      // ' ' '
        };

        internal static readonly SkopikDataType[] DelimiterLookup = {
            SkopikDataType.Array | SkopikDataType.Tuple,
                                        // ','
            SkopikDataType.Scope,       // ';'
        };

        internal static readonly SkopikDataType[] SuffixLookup = {
            SkopikDataType.Binary,      // 'b'
            SkopikDataType.Double,      // 'd'
            SkopikDataType.Float,       // 'f'
            SkopikDataType.Unsigned,    // 'u'
            SkopikDataType.Unsigned,    // 'U'
            SkopikDataType.Long,        // 'L'
        };
        
        internal static void MapLookupTypes()
        {
            for (int i = 0; i < BlockKeys.Length; i += 2)
            {
                var opBlockType = BlockLookup[i >> 1];
                
                // set open/close controls
                m_lookup[BlockKeys[i + 0]] = (int)(opBlockType | SkopikDataType.OpBlockOpen);
                m_lookup[BlockKeys[i + 1]] = (int)(opBlockType | SkopikDataType.OpBlockClose);
            }

            for (int i = 0; i < OperatorKeys.Length; i++)
                m_lookup[OperatorKeys[i]] = (int)OperatorLookup[i];

            for (int i = 0; i < DelimiterKeys.Length; i++)
                m_lookup[DelimiterKeys[i]] = (int)(DelimiterLookup[i] | SkopikDataType.OpBlockDelim);

            for (int i = 0; i < SuffixKeys.Length; i++)
                m_lookup[SuffixKeys[i]] = (int)SuffixLookup[i];

            m_lookup[AssignmentKey] = (int)SkopikDataType.OpStmtAssignmt;
            m_lookup[ScopeBlockKey] = (int)SkopikDataType.OpStmtBlock;
        }

        // TODO: give this a datatype to work with
        internal static string SanitizeNumber(string value, bool isHex)
        {
            var length = 0;
            
            foreach (var c in value)
            {
                var flags = CharUtils.GetCharFlags(c);

                // don't allow decimals/negative hexadecimal numbers
                // (negative hex numbers need to be processed at another point)
                if (!isHex)
                {
                    if (c == DecimalPointKey || c == NegativePrefixKey)
                    {
                        ++length;
                        continue;
                    }
                }
                if ((flags & CharacterTypeFlags.Digit) != 0)
                {
                    ++length;
                    continue;
                }
                if ((flags & CharacterTypeFlags.Letter) != 0)
                {
                    // ABCDEF or abcdef?
                    // (adding 1 prevents 'g' and 'G' false positives)
                    if (isHex && (((c + 1) & ~0x67) == 0))
                    {
                        ++length;
                        continue;
                    }

                    var type = GetDataType(c);

                    if (type != SkopikDataType.None)
                    {
                        // first suffix character, we can safely break
                        break;
                    }
                }

                // can't sanitize the string
                // reset the length and stop
                length = 0;
                break;
            }
            return (length > 0 ) ? value.Substring(0, length) : String.Empty;
        }
        
        internal static SkopikDataType GetDataType(char value)
        {
            if (!m_lookupReady)
            {
                MapLookupTypes();
                m_lookupReady = true;
            }

            return (SkopikDataType)m_lookup[value];
        }

        internal static SkopikDataType GetDataType(string value)
        {
            return GetDataType(value[0]);
        }
        
        internal static bool IsDataType(char value, SkopikDataType dataType)
        {
            var type = (int)GetDataType(value);
            var checkType = (int)dataType;

            return ((type & checkType) == checkType);
        }

        internal static bool IsDataType(string value, SkopikDataType dataType)
        {
            return IsDataType(value[0], dataType);
        }
        
        internal static SkopikDataType GetWordDataType(string value)
        {
            for (int i = 0; i < Keywords.Length; i++)
            {
                var k = Keywords[i];

                if (value.Length != k.Length)
                    continue;

                if (value.Equals(k, StringComparison.InvariantCultureIgnoreCase))
                    return WordLookup[i];
            }

            return SkopikDataType.None;
        }

        internal static SkopikDataType GetNumberDataType(string value)
        {
            if (value.Length < 1)
                return SkopikDataType.None;

            var strIndex = 0;

            var isNegative = false;
            var isHex = false;

            var hasExponent = false;
            var hasDigit = false;
            var hasSeparator = false; // floats

            var numberType = SkopikDataType.None;
            var suffixType = SkopikDataType.None;

            if (IsHexadecimalNumber(value))
            {
                strIndex += 2;
                isHex = true;

                if (strIndex == value.Length)
                    throw new InvalidOperationException($"Malformed hexadecimal number data: '{value}'");
            }
            
            for (int i = strIndex; i < value.Length; i++)
            {
                var c = value[i];
                var flags = CharUtils.GetCharFlags(c);

                if ((flags & CharacterTypeFlags.Digit) != 0)
                {
                    hasDigit = true;
                }
                else if ((flags & CharacterTypeFlags.Letter) != 0)
                {
                    suffixType |= GetDataType(c);

                    if (suffixType == SkopikDataType.None)
                    {
                        var eIdx = ((flags & CharacterTypeFlags.Lowercase) != 0) ? 0 : 1;

                        // check for exponential float
                        if (!isHex && (c == ExponentialKeys[eIdx]))
                        {
                            if (hasExponent || (!hasDigit || (!hasDigit && !hasSeparator)))
                                throw new InvalidOperationException($"Malformed number data: '{value}'");

                            hasExponent = true;
                        }
                    }
                }
                else
                {
                    if (c == DecimalPointKey)
                    {
                        if (!hasDigit || hasSeparator)
                            throw new InvalidOperationException($"Malformed number data: '{value}'");

                        hasSeparator = true;
                    }
                    else if (c == NegativePrefixKey)
                    {
                        if (hasExponent)
                        {
                            if (!hasDigit || (!hasDigit && !hasSeparator))
                                throw new InvalidOperationException($"Malformed number data: '{value}'");

                            // exponential float
                            // just continue normally
                        }
                        else
                        {
                            if (isNegative || hasDigit || hasSeparator)
                                throw new InvalidOperationException($"Malformed number data: '{value}'");

                            // negative number
                            isNegative = true;
                        }
                    }
                }
            }

            // figure out the number type
            if (suffixType == SkopikDataType.None)
            {
                // setup the default value if we couldn't figure it out above
                if (hasSeparator)
                {
                    if (hasExponent)
                        Debug.WriteLine($"Successfully detected an exponential decimal: '{value}'");

                    numberType |= SkopikDataType.Double;
                }
                else
                {
                    numberType |= SkopikDataType.Integer32;
                }
            }
            else
            {
                if ((suffixType & (SkopikDataType.Float | SkopikDataType.Double)) != 0)
                    numberType |= (suffixType & (SkopikDataType.Float | SkopikDataType.Double));

                if ((suffixType & SkopikDataType.NumberFlagMask) != 0)
                {
                    numberType |= SkopikDataType.Integer;

                    // binary data has no sign
                    if ((suffixType & SkopikDataType.Binary) == 0)
                    {
                        // signed?
                        if ((suffixType & SkopikDataType.Unsigned) == 0)
                            numberType |= SkopikDataType.Signed;
                    }
                    
                    numberType |= (suffixType & (SkopikDataType.NumberFlagMask));
                }
            }
            
            // can still be invalid!
            return numberType;
        }

        internal static SkopikDataType GetAnyDataType(string value)
        {
            var dataType = SkopikDataType.None;

            if (value.Length != 1)
            {
                // word data?
                dataType = GetWordDataType(value);
            }

            // control flow data?
            if (dataType == SkopikDataType.None)
                dataType = GetDataType(value[0]);

            // number data?
            if (dataType == SkopikDataType.None)
                dataType = GetNumberDataType(value);

            // may still be invalid
            return dataType;
        }

        internal static bool IsAssignmentOperator(string value)
        {
            return IsDataType(value, SkopikDataType.OpStmtAssignmt);
        }

        internal static bool IsScopeBlockOperator(string value)
        {
            return IsDataType(value, SkopikDataType.OpStmtBlock);
        }

        internal static bool IsEndStatementOperator(string value)
        {
            return IsDataType(value, SkopikDataType.OpBlockDelim);
        }

        internal static bool IsArraySeparator(string value)
        {
            return IsDataType(value, SkopikDataType.OpArrayDelim);
        }

        internal static bool IsScopeSeparator(string value)
        {
            return IsDataType(value, SkopikDataType.OpScopeDelim);
        }

        internal static bool IsDelimiter(string value, SkopikDataType subType = SkopikDataType.None)
        {
            return IsDataType(value, subType | SkopikDataType.OpBlockDelim);
        }

        internal static bool IsOpeningBrace(string value, SkopikDataType subType = SkopikDataType.None)
        {
            return IsDataType(value, subType | SkopikDataType.OpBlockOpen);
        }

        internal static bool IsClosingBrace(string value, SkopikDataType subType = SkopikDataType.None)
        {
            return IsDataType(value, subType | SkopikDataType.OpBlockClose);
        }
        
        internal static bool IsNegativeNumber(string value)
        {
            return ((value.Length > 1) && (value[0] == NegativePrefixKey));
        }
        
        internal static bool IsHexadecimalNumber(string value)
        {
            if (value.Length < HexadecimalPrefix.Length)
                return false;

            var strIndex = 0;

            if (IsNegativeNumber(value))
                ++strIndex;

            for (int i = 0; i < HexadecimalPrefix.Length; i++)
            {
                if (value[strIndex + i] != HexadecimalPrefix[i])
                    return false;
            }

            return true;
        }

        internal static bool IsNumberValue(SkopikDataType dataType)
        {
            return ((dataType & SkopikDataType.Integer) != 0);
        }

        internal static bool IsDecimalNumberValue(SkopikDataType dataType)
        {
            return ((dataType & (SkopikDataType.Float | SkopikDataType.Double)) != 0);
        }
    }
}
