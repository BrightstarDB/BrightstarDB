using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using BrightstarDB.Client;
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            int tripleCount = CreateTaxonomy();
            var end = DateTime.UtcNow;
            LogOperation("created-taxonomy", string.Format("Created {0} triples", tripleCount), end.Subtract(start).TotalMilliseconds.ToString(), null);

            start = DateTime.UtcNow;
            tripleCount = CreateDocumentsInBatches(10, 10000);
            end = DateTime.UtcNow;
            LogOperation("created-documents", string.Format("created {0} triples",  tripleCount), end.Subtract(start).TotalMilliseconds.ToString(), null);

            CheckConsistency();
        }

        /// <summary>
        /// Used to check that the data structures are in the expected form before doing any queries or updates
        /// </summary>
        private void CheckConsistency()
        {
            var docUri = "http://example.org/taxonomybenchmark/documents/400";
            var result = XDocument.Load(Service.ExecuteQuery(StoreName, "select * where { <" + docUri + "> ?p ?o }"));
            if (result.SparqlResultRows().Count() == 0)
            {
                throw new Exception("Bad data - document resource not found.");
            }

            var taxterm = "http://example.org/taxonomybenchmark/classification/l1-0-l2-87-l3-69";
            result = XDocument.Load(Service.ExecuteQuery(StoreName, "select * where { <" + taxterm + "> ?p ?o }"));
            var hasParent = false;
            foreach (XElement row in result.SparqlResultRows())
            {
                if (row.GetColumnValue("p").Equals("http://example.org/taxonomybenchmark/schema/parent"))
                {
                    hasParent = true;
                }
            }
            
            if (!hasParent) {
                throw new Exception("Bad data - resource not connected to a taxonomy term.");
            }
        }

        private int CreateTaxonomy()
        {
            var sb = new StringBuilder();
            int tripleCount = 0;

            // root node
            MakeTriple("classification", "root", "schema", "a", "schema", "TaxonomyTerm", sb);
            tripleCount++;

            // level 1
            int countLevelOne = 10;
            // level 2
            int countLevelTwo = 100;
            // level 3
            int countLevelThree = 100;

            for (var i = 0; i < countLevelOne; i++)
            {
                tripleCount += MakeTaxonomyNode("l1-" + i, "root", sb);
                for (var j = 0; j < countLevelTwo; j++)
                {
                    tripleCount += MakeTaxonomyNode("l1-" + i + "-l2-" + j, "l1" + i, sb);
                    for (var k = 0; k < countLevelThree; k++)
                    {
                        tripleCount += MakeTaxonomyNode("l1-" + i + "-l2-" + j + "-l3-" + k, "l1-" + i + "-l2-" + j, sb);                        
                    }
                }
            }

            InsertData(sb.ToString());
            return tripleCount;
        }

        private int MakeTaxonomyNode(string id, string parentId, StringBuilder sb)
        {
            MakeTriple("classification", id, "schema", "a", "schema", "TaxonomyTerm", sb);
            MakeTriple("classification", id, "schema", "parent", "classification", parentId, sb);
            return 2;
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

        private int CreateDocumentsInBatches(int batchSize, int batchItemCount)
        {
            var tripleCount = 0;
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
                    tripleCount += MakeDocumentNode(docId.ToString(), classification, sb);
                    docId++;
                }
                InsertData(sb.ToString());
            }
            return tripleCount;
        }

        private int MakeDocumentNode(string id, IEnumerable<string> classification, StringBuilder sb)
        {
            int count = 1;
            MakeTriple("documents", id, "schema", "a", "schema", "Document", sb);

            foreach (var c in classification)
            {
                MakeTriple("documents", id, "schema", "classified-by", "classification", c, sb);
                count++;
            }

            return count;
        }

        public override void RunMix()
        {
            // get document metadata
            Random rnd = new Random(565979575);

            var start = DateTime.UtcNow;
            for (int i = 0; i < 10000; i++)
            {
                var docId = rnd.Next(400000);
                var result = XDocument.Load(Service.ExecuteQuery(StoreName, "select * where { <http://example.org/taxonomybenchmark/documents/" + docId + "> ?p ?o }"));
            }
            var end = DateTime.UtcNow;
            LogOperation("completed-document-metadata-lookup-random-small-sample", string.Format("Fetched metadata for {0} documents", 1000), end.Subtract(start).TotalMilliseconds.ToString(), null);

            start = DateTime.UtcNow;
            for (int i = 0; i < 10000; i++)
            {
                var docId = 60000;
                var result = XDocument.Load(Service.ExecuteQuery(StoreName, "select * where { <http://example.org/taxonomybenchmark/documents/" + docId + "> ?p ?o }"));
            }
            end = DateTime.UtcNow;
            LogOperation("completed-document-metadata-lookup-repeated-document", string.Format("Fetched metadata for 1 document"), end.Subtract(start).TotalMilliseconds.ToString(), null);

            start = DateTime.UtcNow;
            for (int i = 0; i < 400000; i++)
            {
                var docId = rnd.Next(400000);
                var result = XDocument.Load(Service.ExecuteQuery(StoreName, "select * where { <http://example.org/taxonomybenchmark/documents/" + docId + "> ?p ?o }"));
            } 
            end = DateTime.UtcNow;
            LogOperation("completed-document-metadata-lookup", string.Format("Fetched metadata for {0} documents", 400000), end.Subtract(start).TotalMilliseconds.ToString(), null);

            // run threaded document lookup
            start = DateTime.UtcNow;
            Parallel.Invoke(() =>
                                {
                                    Random rnd1 = new Random(1);
                                    GetDocumentMetadata(rnd);
                                 },  // close

                                 () =>
                                 {
                                    Random rnd2 = new Random(2);
                                    GetDocumentMetadata(rnd);
                                 }, //close 

                                () =>
                                {
                                    Random rnd3 = new Random(3);
                                    GetDocumentMetadata(rnd);
                                }, //close 

                                () =>
                                {
                                    Random rnd4 = new Random(4);
                                    GetDocumentMetadata(rnd);
                                } 
                         ); //close parallel.invoke

            end = DateTime.UtcNow;
            LogOperation("completed-parallel-document-metadata-lookup", string.Format("Fetched metadata for {0} documents using {1} parallel tasks", 400000, 4), end.Subtract(start).TotalMilliseconds.ToString(), null);
        }

        private void GetDocumentMetadata(Random rnd)
        {
            for (int i = 0; i < 100000; i++)
            {
                var docId = rnd.Next(400000);
                var result = XDocument.Load(Service.ExecuteQuery(StoreName, "select * where { <http://example.org/taxonomybenchmark/documents/" + docId + "> ?p ?o }"));                
            }   
        }
    }
}
