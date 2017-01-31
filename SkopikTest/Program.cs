using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Skopik;

namespace SkopikTest
{
    class Program
    {
        static void WriteLog(string message)
        {
            Console.WriteLine(message);
            Debug.WriteLine(message);
        }

        static void Main(string[] args)
        {
            var timer = new Stopwatch();
            var testFilename = (args.Length == 1) ? args[0] : Path.Combine(Environment.CurrentDirectory, "test.skop");

            WriteLog($"Running tests on file: {testFilename}");

            if (!File.Exists(testFilename))
            {
                WriteLog("Test file not found, what did you do?!");
                return;
            }

            // how many times to parse it
            var nLoops = (args.Length == 2) ? int.Parse(args[1]) : 5000;

            timer.Start();
            for (int v = 0; v < nLoops; v++)
            {
                var skopFile = new SkopikFile(testFilename);
                skopFile.Parse();
            }
            timer.Stop();

            var totalTime = timer.ElapsedMilliseconds;
            var averageTime = (double)totalTime / nLoops;

            WriteLog($"Parsed {nLoops:N0} files in {timer.ElapsedMilliseconds}ms (avg: {averageTime:F4}ms).");
        }
    }
}
