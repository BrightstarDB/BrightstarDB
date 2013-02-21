using System;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using BrightstarDB.Cluster.Common;
using BrightstarDB.ClusterManager;
using BrightstarDB.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BrightstarService = BrightstarDB.Client.BrightstarService;


namespace BrightstarDB.ClusterTests
{
    [TestClass]
    public class NodeServiceTests
    {
        [TestMethod]
        public void TestTwoServiceNodes()
        {
            var storeName = Guid.NewGuid().ToString();

            var client = BrightstarService.GetClient("type=http;endpoint=http://localhost:8091/brightstar");
            client.CreateStore(storeName);
            var result = client.ExecuteTransaction(storeName, null, null,
                                        "<http://brightstardb.com/foo> <http://brightstardb.com/isa> <http://brightstardb.com/bar>.");

            Assert.IsTrue(result.JobCompletedOk, "Setup transaction failed on TestStore2");

            var client1 = BrightstarService.GetClient("type=http;endpoint=http://localhost:8091/brightstar");
            client1.ExecuteTransaction(storeName, null, null,
                                        "<http://brightstardb.com/foo> <http://brightstardb.com/isa> <http://brightstardb.com/bar1> .");

            Thread.Sleep(1000);

            // query the second node
            var client2 = BrightstarService.GetClient("type=http;endpoint=http://localhost:8092/brightstar");
            var results = client2.ExecuteQuery(storeName, "SELECT * WHERE { <http://brightstardb.com/foo> ?s ?p }");
            XDocument newResultsDoc = XDocument.Load(results);
            Assert.AreEqual(2, newResultsDoc.SparqlResultRows().Count());
        }
    }
}
