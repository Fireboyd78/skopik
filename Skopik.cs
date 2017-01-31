using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Skopik
{
    internal static class Skopik
    {
        private static bool m_lookupReady = false;
        private static int[] m_lookup = new int[128];
        
        internal static readonly string[] Keywords = { "true", "false", "null" };

        internal static readonly string[] CommentLineKeys = { "//" };

        internal static readonly string CommentBlockOpenKey = "/*";
        internal static readonly string CommentBlockCloseKey = "*/";

        internal static readonly char AssignmentKey = '=';
        internal static readonly char ScopeBlockKey = ':'; // opening of a scope block

        internal static readonly char[] ControlKeys = { '{', '}', '[', ']' };
        internal static readonly char[] OperatorKeys = { '@', '"', '\'' };
        internal static readonly char[] SeparatorKeys = { ',', ';' };

        internal static readonly char[] SuffixKeys = { 'b', 'd', 'f', 'u', 'U', 'L' };

        internal static readonly string HexadecimalPrefix = "0x";

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
            SkopikDataType.Keyword,     // '@'
            SkopikDataType.String,      // ' " '
            SkopikDataType.String,      // ' ' '
        };

        internal static readonly SkopikDataType[] SeparatorLookup = {
            SkopikDataType.Array,       // ','
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
            for (int i = 0; i < ControlKeys.Length; i++)
            {
                var type = (int)(((i % 2) == 0) ? SkopikDataType.OpBlockOpen : SkopikDataType.OpBlockClose);

                // combine into one operator :)
                type |= (int)ControlLookup[(i > 1) ? 1 : 0];

                m_lookup[ControlKeys[i]] = type;
            }

            for (int i = 0; i < OperatorKeys.Length; i++)
                m_lookup[OperatorKeys[i]] = (int)OperatorLookup[i];

            for (int i = 0; i < SeparatorKeys.Length; i++)
                m_lookup[SeparatorKeys[i]] = (int)(SkopikDataType.OpBlockStmtEnd | SeparatorLookup[i]);

            for (int i = 0; i < SuffixKeys.Length; i++)
                m_lookup[SuffixKeys[i]] = (int)SuffixLookup[i];

            m_lookup[AssignmentKey] = (int)SkopikDataType.OpStmt;
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
                    if (c == '.' || c == '-')
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
                    if (isHex && ((c & ~0x67) == 0))
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
            }

            var strVal = value.Substring(strIndex);

            foreach (var c in strVal)
            {
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
                        // check for exponential float
                        if (!isHex && ((c & ~0x65) == 0))
                        {
                            if (hasExponent || (!hasDigit || (!hasDigit && !hasSeparator)))
                                throw new InvalidOperationException($"Malformed number data: '{value}'");

                            hasExponent = true;
                        }
                    }
                }
                else
                {
                    if (c == '.')
                    {
                        if (!hasDigit || hasSeparator)
                            throw new InvalidOperationException($"Malformed number data: '{value}'");

                        hasSeparator = true;
                    }
                    else if (c == '-')
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
            return IsDataType(value, SkopikDataType.OpStmt);
        }

        internal static bool IsScopeBlockOperator(string value)
        {
            return IsDataType(value, SkopikDataType.OpStmtBlock);
        }

        internal static bool IsEndStatementOperator(string value)
        {
            return IsDataType(value, SkopikDataType.OpBlockStmtEnd);
        }

        internal static bool IsArraySeparator(string value)
        {
            return IsDataType(value, SkopikDataType.OpArrayStmtEnd);
        }

        internal static bool IsScopeSeparator(string value)
        {
            return IsDataType(value, SkopikDataType.OpScopeStmtEnd);
        }

        internal static bool IsOpeningBrace(string value)
        {
            return IsDataType(value, SkopikDataType.OpBlockOpen);
        }

        internal static bool IsClosingBrace(string value)
        {
            return IsDataType(value, SkopikDataType.OpBlockClose);
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

        internal static bool IsNegativeNumber(string value)
        {
            return value.StartsWith("-");
        }
        
        internal static bool IsHexadecimalNumber(string value)
        {
            var strIndex = 0;

            if (IsNegativeNumber(value))
                ++strIndex;

            return value.Substring(strIndex).StartsWith(HexadecimalPrefix);
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
