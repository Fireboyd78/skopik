using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Skopik;

namespace SkopikTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var timer = new Stopwatch();

            timer.Start();
            for (int v = 0; v < 5000; v++)
            {
                var skopFile = new SkopikFile(@"C:\Dev\Research\Skop\test.skop");
                skopFile.Parse();
            }
            timer.Stop();
            
            Debug.WriteLine($"5,000 SKOP files took {timer.ElapsedMilliseconds}ms");
        }
    }
}
