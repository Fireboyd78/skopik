using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Skopik
{
    public class SkopikData
    {
        public SkopikScope GlobalScope { get; set; }
        
        public SkopikData()
        {
        }

        public SkopikData(byte[] buffer)
        {
            using (var ms = new MemoryStream(buffer, 0, buffer.Length, false))
            using (var skop = new SkopikReader(ms))
            {
                GlobalScope = skop.ReadScope();
                GlobalScope.Name = "<global>";
            }
        }
    }
}
