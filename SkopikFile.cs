using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Skopik
{
    public class SkopikFile
    {
        private SkopikScopeType _globalScope;

        public string FileName { get; }

        public SkopikScopeType GlobalScope
        {
            get
            {
                if (_globalScope == null)
                    _globalScope = new SkopikScopeType() {
                        Name = "<global>"
                    };

                return _globalScope;
            }
        }
        
        public void Parse()
        {
            using (var ms = new MemoryStream(File.ReadAllBytes(FileName)))
            using (var skop = new SkopikReader(ms))
            {
                skop.ReadNestedScope(GlobalScope, $"<scope::('{Path.GetFileNameWithoutExtension(FileName)}')>");
            }
        }
        
        public SkopikFile(string fileName)
        {
            if (!File.Exists(fileName))
                throw new InvalidOperationException("Skopik file not found!");

            FileName = fileName;
        }
    }
}
