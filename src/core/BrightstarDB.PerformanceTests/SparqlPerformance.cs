using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BrightstarDB.Client;
using BrightstarDB.Profiling;
using BrightstarDB.Storage;
using NUnit.Framework;

namespace BrightstarDB.PerformanceTests
{
    [TestFixture(true)]
    [TestFixture(false)]
    public class SparqlPerformance
    {
        private readonly bool _useVirtualNodes;

        public SparqlPerformance(bool useVirtualNodes)
        {
            _useVirtualNodes = useVirtualNodes;
        }

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Configuration.EnableVirtualizedQueries = _useVirtualNodes;
            Configuration.EnableQueryCache = false; // We want to measure performance without caching
            EnsureStore();
        }

        [Test]
        [Sequential]
        public void TestSimpleJoinQuery()
        {
            var sparqlQuery =
                    @"PREFIX bsbm: <http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/> 
                    SELECT ?review WHERE { 
                        ?review bsbm:reviewFor ?product . 
                        ?product bsbm:productFeature <http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature2330> .
                    } LIMIT 3";
            // Warm-up
            var client = BrightstarService.GetClient("type=embedded;storesDirectory=stores");
            for (int i = 0; i < 5; i++)
            {
                client.ExecuteQuery("SparqlPerformance", sparqlQuery);
            }

            // Profile
            var profiler = new BrightstarProfiler("SimpleJoinQuery");
            for (int i = 0; i < 1000; i++)
            {
                using (profiler.Step("ExecuteQuery"))
                {
                    client.ExecuteQuery("SparqlPerformance", sparqlQuery);
                }
            }

            Console.WriteLine(profiler.GetLogString());
        }

        private void EnsureStore()
        {
            var client = BrightstarService.GetClient("type=embedded;storesDirectory=stores");
            if (!client.DoesStoreExist("SparqlPerformance"))
            {
                Console.WriteLine("Creating performance test store.");
                client.CreateStore("SparqlPerformance", PersistenceType.AppendOnly);
                var importPath = Path.Combine("stores", "import");
                if (!Directory.Exists(importPath))
                {
                    Directory.CreateDirectory(importPath);
                }
                var dataPath =
                    Path.GetFullPath(Path.Combine("..", "..", "..", "BrightstarDB.Tests", "data", "bsbm_1M.nt"));
                File.Copy(dataPath, Path.Combine(importPath, "bsbm_1M.nt"));
                var importJob = client.StartImport("SparqlPerformance", "bsbm_1M.nt");
                while (!(importJob.JobCompletedOk || importJob.JobCompletedWithErrors))
                {
                    Thread.Sleep(2000);
                    importJob = client.GetJobInfo("SparqlPerformance", importJob.JobId);
                    Console.WriteLine(importJob.StatusMessage);
                }
                if (importJob.JobCompletedWithErrors)
                {
                    throw new Exception(
                        String.Format(
                            "Import job failed. Testing cannot proceed. Last job message: {0}. Job exception trace: {1}",
                            importJob.StatusMessage, importJob.ExceptionInfo));
                }
            }
        }
    }
}
