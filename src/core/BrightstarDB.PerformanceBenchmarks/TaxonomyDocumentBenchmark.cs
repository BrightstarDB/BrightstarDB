using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using BrightstarDB.Client;

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
            CreateDocumentsInBatches(1000);
            var end = DateTime.UtcNow;
            LogOperation("created-data", "created 1million items, 2 million triples", end.Subtract(start).TotalMilliseconds.ToString(), null);
        }

        private void CreateTaxonomy()
        {
            var sb = new StringBuilder();

            // root node
            MakeTriple("classification", "root", "schema", "a", "schema", "TaxonomyTerm", sb);

            // level 1
            int countLevelOne = 10;
            // level 2
            int countLevelTwo = 100;
            // level 3
            int countLevelThree = 100;

            for (var i = 0; i < countLevelOne; i++)
            {
                MakeTaxonomyNode("l1-" + i, "root", sb);
                for (var j = 0; j < countLevelTwo; j++)
                {
                    MakeTaxonomyNode("l2-" + j, "l1" + i, sb);
                    for (var k = 0; k < countLevelThree; k++)
                    {
                        MakeTaxonomyNode("l3-" + k, "l2" + j, sb);                        
                    }
                }
            }

            InsertData(sb.ToString());
        }

        private void MakeTaxonomyNode(string id, string parentId, StringBuilder sb)
        {
            MakeTriple("classification", id, "schema", "a", "schema", "TaxonomyTerm", sb);
            MakeTriple("classification", id, "schema", "parent", "classification", parentId, sb);
        }

        private const string TriplePattern = "<{0}> <{1}> <{2}> .";

        private void MakeTriple(string subjContainer, string subjId, string predContainer, string predId, string objContainer, string objId, StringBuilder sb)
        {
            sb.AppendLine(string.Format(TriplePattern, MakeResourceUri(subjContainer, subjId),
                MakeResourceUri(predContainer, predId), MakeResourceUri(objContainer, objId)));
        }

        private void InsertData(String data)
        {
            var updateData = new UpdateTransactionData();
            updateData.InsertData = data;
            Service.ExecuteTransaction(StoreName, updateData);
        }

        // try and create a uri length and namespace that is typical in the real world.
        private const string ResourcePrefix = "http://example.org/taxonomybenchmark/{0}/{1}";

        private string MakeResourceUri(string container, string id)
        {
            return String.Format(ResourcePrefix, container, id);
        }

        private void CreateDocumentsInBatches(int count)
        {            
        }

        public override void RunMix()
        {            
        }
    }
}
