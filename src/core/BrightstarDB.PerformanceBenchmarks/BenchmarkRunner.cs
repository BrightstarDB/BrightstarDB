using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace BrightstarDB.PerformanceBenchmarks
{
    class BenchmarkRunner
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

            // test scale
            var scale = int.Parse(args[2]);

            // todo: use introspection to detect benchmarks
            ICollection<BenchmarkBase> benchmarks = new Collection<BenchmarkBase>();
            benchmarks.Add(new ExampleBenchmark(reportFolder + "\\example-benchmark-results-" + Guid.NewGuid() + ".xml", connectionString));
            benchmarks.Add(new TaxonomyDocumentBenchmark(reportFolder + "\\taxonomy-benchmark-results-" + Guid.NewGuid() + ".xml", connectionString));
            foreach (var benchmark in benchmarks)
            {
                benchmark.Setup();
                benchmark.RunMix();
                benchmark.CleanUp();
            }
        }

        static void Usage()
        {
            Console.WriteLine(@"Usage: BenchmarkRunner.exe [storeFolderPath] [reportPath] [benchmark scale (1-5)]");
        }

        private void RunBenchmark(BenchmarkBase benchmark)
        {
            
        }
    }
}
