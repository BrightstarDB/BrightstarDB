using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
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
            CreateDocumentsInBatches(10, 10000);
            var end = DateTime.UtcNow;
            LogOperation("created-data", "created 500,000 triples", end.Subtract(start).TotalMilliseconds.ToString(), null);
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
                    MakeTaxonomyNode("l1-" + i + "-l2-" + j, "l1" + i, sb);
                    for (var k = 0; k < countLevelThree; k++)
                    {
                        MakeTaxonomyNode("l1-" + i + "-l2-" + j + "-l3-" + k, "l1-" + i + "-l2-" + j, sb);                        
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

        private void CreateDocumentsInBatches(int batchSize, int batchItemCount)
        {
            var docId = 0;
            var rnd = new Random(1000000);
            string template = "l1-{0}-l2-{1}-l3-{2}";
            for (int i = 0; i < batchSize; i++)
            {
                var sb = new StringBuilder();
                for (int j = 0; j < batchItemCount; j++)
                {
                    var classification = new List<String>()
                    {
                        string.Format(template, rnd.Next(10), rnd.Next(100), rnd.Next(100)),
                        string.Format(template, rnd.Next(10), rnd.Next(100), rnd.Next(100)),
                        string.Format(template, rnd.Next(10), rnd.Next(100), rnd.Next(100))
                    };
                    MakeDocumentNode(docId.ToString(), classification, sb);
                    docId++;
                }
                InsertData(sb.ToString());
            }
        }

        private void MakeDocumentNode(string id, IEnumerable<string> classification, StringBuilder sb)
        {
            MakeTriple("documents", id, "schema", "a", "schema", "Document", sb);

            foreach (var c in classification)
            {
                MakeTriple("documents", id, "schema", "classified-by", "classification", c, sb);                
            }
        }


        public override void RunMix()
        {            
        }
    }
}
