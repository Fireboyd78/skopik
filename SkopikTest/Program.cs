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

        static void RunTest(SkopikData skopData)
        {
            var data = skopData.GlobalScope;

            ISkopikObject[] objects = {
                data["test_bool"],
                data["test_int"],
                data["test_uint"],
                data["test_long"],
                data["test_ulong"],
                data["test_binary"],
                data["test_double"],
                data["test_float"],
                data["test_string"],
                data["test_inline"],
                data["<array::('Test array')>"],
                data["<scope::('Test scope')>"],
                data["<tuple::('Test tuple')>"],
            };
            
            for (int i = 0; i < objects.Length; i++)
            {
                var obj = objects[i];
                var typeName = "ERROR!!!";

                if (obj != null)
                {
                    var type = obj.GetType();

                    typeName = type.Name;

                    if (type.IsGenericType)
                    {
                        var args = type.GetGenericArguments().Select((e) => e.Name);

                        typeName = $"{typeName.Split('`')[0]}<{String.Join(",", args)}>";
                    }

                    if (typeName.StartsWith("Skopik"))
                        typeName = typeName.Substring(6);
                }

                WriteLog($"[{i}]: '{typeName}'");
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
            var nLoops = (args.Length == 2) ? int.Parse(args[1]) : 5000;

            var test1Elapsed = 0L;
            
            WriteLog($"Running {nLoops:N0} tests on '{Path.GetFullPath(testFilename)}'...");

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
                
                var skopData = new SkopikData(buffer);

                // only do this for tests run one time
                if (nLoops == 1)
                    RunTest(skopData);
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
