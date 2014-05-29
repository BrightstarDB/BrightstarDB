using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB.PerformanceBenchmarks
{
    public class TaxonomyDocumentBenchmark : BenchmarkBase
    {
        public TaxonomyDocumentBenchmark(string filename, string connectionString) : base(filename, connectionString)
        {
            
        }

        public override void Setup()
        {
            var start = DateTime.UtcNow;
            CreateTaxonomy();
            CreateDocumentsInBatchesOfHundred();
            var end = DateTime.UtcNow;
        }

        private void CreateTaxonomy()
        {
            
        }

        private void CreateDocumentsInBatchesOfHundred()
        {
            
        }

        public override void RunMix()
        {
            throw new NotImplementedException();
        }

        public override void CleanUp()
        {
            base.CleanUp();
        }
    }
}
