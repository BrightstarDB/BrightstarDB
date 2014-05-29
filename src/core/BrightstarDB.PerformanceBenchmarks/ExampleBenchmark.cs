using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB.PerformanceBenchmarks
{
    public class ExampleBenchmark : BenchmarkBase
    {
        public ExampleBenchmark(string filename, string connectionString) : base(filename, connectionString)
        {
        }

        public override void Setup()
        {
            LogOperation("create-data", "create all the initial data", "0", "");
        }

        public override void RunMix()
        {
            LogOperation("do-queries", "query initial data set", "5", "");
            LogOperation("do-updates", "update data set", "25", "");
            LogOperation("do-queries-after-updates", "query updated data set", "15", "");
        }
    }
}
