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

        static void DumpData(SkopikData skopData, int indentLevel = 0)
        {
            var idx = 0;

            foreach (var kv in skopData)
            {
                var obj = kv.Value;

                if (obj != null)
                {
                    var str = $"{obj.DataType}";

                    if (obj is ISkopikBlock)
                    {
                        if (obj.IsTuple)
                            str = $"{((ISkopikTuple)obj).TupleType}";

                        var block = (ISkopikBlock)obj;

                        str = $"{str}[{block.Count}]";

                        if (!block.IsAnonymous)
                            str = $"{str}({block.Name})";
                    }
                    else if (obj is ISkopikValue)
                    {
                        str = $"{str}('{(ISkopikValue)obj}')";
                    }

                    if (skopData.IsScope)
                        str = $"{kv.Key} : {str}";

                    str = $"({idx++}) {str}";

                    if (indentLevel > 0)
                    {
                        var indent = "";

                        for (int i = 0; i < indentLevel; i++)
                            indent += " ";

                        str = $"{indent}{str}";
                    }

                    WriteLog(str);

                    if (obj is ISkopikBlock)
                    {
                        var inner = new SkopikData((ISkopikBlock)obj);
                        DumpData(inner, indentLevel + 2);
                    }
                }
                else
                {
                    WriteLog("ERROR!!!");
                }
            }
        }

        static void Main(string[] args)
        {
            var timer = new Stopwatch(); 
            var testFilename = (args.Length >= 1) ? args[0] : Path.Combine(Environment.CurrentDirectory, "test.skop");

            if (!File.Exists(testFilename))
            {
                WriteLog($"Error -- file '{testFilename}' does not exist!");
                return;
            }

            // how many times to parse it
            var nLoops = (args.Length == 2) ? int.Parse(args[1]) : 1;

            var test1Elapsed = 0L;
            
            WriteLog($"Running {nLoops:N0} tests on '{Path.GetFullPath(testFilename)}'...");

            var name = Path.GetFileNameWithoutExtension(testFilename);
            var buffer = File.ReadAllBytes(testFilename);
            
            timer.Start();
            for (int v = 0; v < nLoops; v++)
            {
                if (v == 1)
                {
                    test1Elapsed = timer.ElapsedMilliseconds;
                    WriteLog($"Startup impact: {test1Elapsed}ms");

                    timer.Restart();
                }

                var skopData = SkopikData.Load(buffer, name);

                // only do this for tests run one time
                if (nLoops == 1)
                    DumpData(skopData);
            }
            timer.Stop();
            
            WriteLog("\nResults (+/- startup):");

            var resultNames = new[] {
                $"(+):",
                $"(-):",
            };

            for (int i = 0; i < 2; i++)
            {
                var resultName = resultNames[i];

                var elapsedMilliseconds = timer.ElapsedMilliseconds;
                var elapsedSeconds = (elapsedMilliseconds / 1000d);

                if (i == 0)
                    elapsedMilliseconds += test1Elapsed;

                var averageTime = ((double)elapsedMilliseconds / nLoops);

                WriteLog($"  {resultName} {elapsedMilliseconds}ms, average: {averageTime:F4}ms");
            }
        }
    }
}
