namespace BrightstarDB.PerformanceBenchmarks
{
    public class ExampleBenchmark : BenchmarkBase
    {
        public override void Setup()
        {
            Report.LogOperationCompleted("create-data", "create all the initial data", 1, 10);
        }

        public override void RunMix()
        {
            Report.LogOperationCompleted("do-queries", "query the loaded data", 25, 250);
            Report.LogOperationSkipped("skipped", "skip this operation");
            Report.LogOperationException("some-failure", "this operation will fail", "Here are the full details about the error:\n\tStuff happened.");
        }

        public override void CleanUp()
        {
            
        }
    }
}
