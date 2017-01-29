using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Skopik
{
    internal static class StringExtensions
    {
        public static string StripQuotes(this string @this)
        {
            var start = 0;
            var length = 0;

            var isOpen = false;
            var isEscaped = false;

            for (int i = 0; i < @this.Length; i++)
            {
                var c = @this[i];
                var flags = CharUtils.GetCharFlags(c);

                if (c == '\\')
                    isEscaped = true;

                if ((flags & CharacterTypeFlags.Quote) != 0)
                {
                    if (!isOpen)
                    {
                        if (!isEscaped)
                        {
                            isOpen = true;
                            start = (i + 1);
                            continue;
                        }
                    }
                    else
                    {
                        if (isEscaped)
                        {
                            isEscaped = false;
                        }
                        else
                        {
                            return @this.Substring(start, length);
                        }
                    }
                }
                length++;
            }

            return @this;
        }

        public static string[] SplitTokens(this string @this)
        {
            var values = new List<String>();

            var start = 0;
            var length = 0;

            for (int i = 0; i < @this.Length; i++)
            {
                var c = @this[i];
                var flags = CharUtils.GetCharFlags(c);

                if ((flags & CharacterTypeFlags.Null) != 0)
                    break;

                if ((flags & CharacterTypeFlags.TabOrWhitespace) != 0)
                {
                    if (length > 0)
                        values.Add(@this.Substring(start, length));

                    start = (i + 1); // "ABC|  DEF" -> "ABC | DEF" -> "ABC |DEF"
                    length = 0;
                }
                else
                {
                    length++;
                }
            }

            return values.ToArray();
        }

        public static string[] SplitTokensNew(this string @this)
        {
            var values = new List<String>();

            var start = 0;
            var length = 0;

            var stringOpen = false;
            var stringEscaped = false;
            
            for (int i = 0; i < @this.Length; i++)
            {
                var c = @this[i];
                var flags = CharUtils.GetCharFlags(c);

                // break on null
                if ((flags & CharacterTypeFlags.Null) != 0)
                    break;

                if ((flags & CharacterTypeFlags.TabOrWhitespace) != 0)
                {
                    // process tabs/whitespace outside of strings
                    if (!stringOpen)
                    {
                        if (length > 0)
                            values.Add(@this.Substring(start, length));

                        start = (i + 1); // "ABC|  DEF" -> "ABC | DEF" -> "ABC |DEF"
                        length = 0;

                        continue;
                    }
                }
                
                if ((flags & CharacterTypeFlags.ExtendedOperators) != 0)
                {
                    if (!stringOpen)
                    {
                        values.Add(c.ToString());

                        start = (i + 1);
                        length = 0;

                        continue;
                    }
                }

                // increase string length
                ++length;
                
                if ((flags & CharacterTypeFlags.Quote) != 0)
                {
                    if (stringOpen)
                    {
                        if (stringEscaped)
                        {
                            stringEscaped = false;
                        }
                        else
                        {
                            // complete the string (include last quote)
                            if (length > 0)
                                values.Add(@this.Substring(start, length + 1));

                            start = (i + 1); // "ABC|" -> "ABC"|
                            length = 0;

                            stringOpen = false;
                        }
                    }
                    else
                    {
                        start = i; // |"ABC"
                        length = 0;

                        stringOpen = true;
                    }
                }
                else if (stringEscaped)
                {
                    // not an escape sequence
                    stringEscaped = false;
                }

                if (stringOpen && (c == '\\'))
                    stringEscaped = true;
            }

            // final add
            if (length > 0)
                values.Add(@this.Substring(start, length));

            return values.ToArray();
        }
    }
}
