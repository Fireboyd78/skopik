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
                            // wrap in '<value>'
                            return $"'{@this.Substring(start, length)}'";
                        }
                    }
                }
                length++;
            }

            return @this;
        }
            
        
    }
}
