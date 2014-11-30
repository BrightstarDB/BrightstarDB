using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace BrightstarDB.PerformanceBenchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Usage();
                return;
            }

            // base directory for stores
            var storeFolder = args[0];
            var connectionString = string.Format("type=embedded;storesDirectory={0}", storeFolder);

            // base directory for reports
            var reportFolder = args[1];
            if (!Directory.Exists(reportFolder))
            {
                Directory.CreateDirectory(reportFolder);
            }
            var runName = Environment.MachineName + "_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

            // test scale
            var scale = int.Parse(args[2]);

            foreach (var benchmark in GetBenchmarks())
            {
                try
                {
                    benchmark.Initialize(connectionString, scale);
                    benchmark.Setup();
                    benchmark.RunMix();
                    benchmark.CleanUp();
                    WriteReport(reportFolder, runName, benchmark);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Unhandled exception while running benchmark {0}. Details follow.\n{1}",
                        benchmark.GetType().FullName, ex);
                }
            }
        }

        private static IEnumerable<BenchmarkBase> GetBenchmarks()
        {
            var ret = new List<BenchmarkBase>();
            foreach (var t in typeof(BenchmarkBase).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(BenchmarkBase)) && !t.IsAbstract))
            {
                var ctor = t.GetConstructor(new Type[] { });
                if (ctor != null)
                {
                    try
                    {
                        var instance = Activator.CreateInstance(t) as BenchmarkBase;
                        if (instance != null)
                        {
                            ret.Add(instance);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Failed to create instance of benchmark class {0}. Cause: {1}", t.FullName, ex);
                    }
                }
                else
                {
                    Console.Error.WriteLine("Could not find a no-args constructor for benchmark class {0}", t.FullName);
                }
            }
            return ret;
        }

        private static void WriteReport(string reportFolder, string runName, BenchmarkBase benchmark)
        {
            var ser = new XmlSerializer(typeof(BenchmarkReport));
            var reportFileName = Path.Combine(reportFolder, runName + "_" + benchmark.GetType().Name + ".xml");
            var writer = new StreamWriter(reportFileName, false, Encoding.UTF8);
            ser.Serialize(writer, benchmark.Report);
            writer.Close();
        }

        static void Usage()
        {
            Console.WriteLine(@"Usage: BenchmarkRunner.exe [storeFolderPath] [reportPath] [benchmark scale (1-5)]");
        }
    }
}
