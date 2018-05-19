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
        public static readonly string DefaultName = "<global>";

        public SkopikScope GlobalScope { get; set; }

        public static SkopikData Load(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException("Skopik file was not found.");

            var name = Path.GetFileNameWithoutExtension(filename);
            var buffer = File.ReadAllBytes(filename);

            return new SkopikData(name, buffer);
        }

        public SkopikData()
            : this(DefaultName) { }

        public SkopikData(byte[] buffer)
            : this(DefaultName, buffer) { }

        public SkopikData(string name)
        {
            GlobalScope = new SkopikScope(name);
        }
        
        public SkopikData(string name, byte[] buffer)
        {
            using (var ms = new MemoryStream(buffer, 0, buffer.Length, false))
            using (var skop = new SkopikReader(ms))
            {
                GlobalScope = skop.ReadScope(name);
            }
        }

        public SkopikData(string name, Stream stream)
        {
            using (var skop = new SkopikReader(stream))
            {
                GlobalScope = skop.ReadScope(name);
            }
        }
    }
}
