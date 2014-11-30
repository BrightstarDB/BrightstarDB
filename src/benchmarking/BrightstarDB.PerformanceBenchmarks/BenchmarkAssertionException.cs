using System;

namespace BrightstarDB.PerformanceBenchmarks
{
    public class BenchmarkAssertionException : Exception
    {
        public BenchmarkAssertionException(string message): base(message)
        {
        }
    }
}