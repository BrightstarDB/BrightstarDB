using System;
using System.IO;
using System.ServiceModel;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using BrightstarDB.Client;
using BrightstarDB.Cluster.Common;
using BrightstarDB.ClusterNode;
using BrightstarDB.ClusterManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.ClusterTests
{
    [TestClass]
    public class TwoNodeClusterTests
    {
        private NodeCore _coreA;
        private NodeCore _coreB;
        private ManagerNode _clusterManager;
        private ClusterConfiguration _testConfiguration;

        public TwoNodeClusterTests()
        {
            TestUtils.ResetDirectory("c:\\brightstar\\coreA");
            TestUtils.ResetDirectory("c:\\brightstar\\coreB");
            Thread.Sleep(2000);
            _coreA = new NodeCore("c:\\brightstar\\coreA");
            _coreB = new NodeCore("c:\\brightstar\\coreB");
            _testConfiguration = new ClusterConfiguration
                                     {
                                         ClusterNodes = 
                                             new List<NodeConfiguration>
                                                 {
                                                     new NodeConfiguration("127.0.0.1", 10001, 8090, 8095),
                                                     new NodeConfiguration("127.0.0.1", 10002, 8091, 8096)
                                                 },
                                         MasterConfiguration = new MasterConfiguration { WriteQuorum = 1 }
                                     };
            _clusterManager = new ManagerNode(_testConfiguration);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _coreA.Stop();
            _coreB.Stop();
            _clusterManager.Stop();
        }

        [TestMethod]
        public void TestClusterStart()
        {
            _coreA.Start(10001);
            _coreB.Start(10002);
            Assert.AreEqual(CoreState.WaitingForMaster, _coreA.GetStatus());
            Assert.AreEqual(CoreState.WaitingForMaster, _coreB.GetStatus());
            _clusterManager.Start();

            Thread.Sleep(500);
            Assert.AreEqual(CoreState.RunningMaster, _coreA.GetStatus());
            Assert.AreEqual(CoreState.RunningSlave, _coreB.GetStatus());
        }

        [TestMethod]
        public void TestHalfClusterStart()
        {
            _coreA.Start(10001);
            _clusterManager.Start();
            Thread.Sleep(500);
            Assert.AreEqual(CoreState.WaitingForSlaves, _coreA.GetStatus());
        }

        [TestMethod]
        public void TestStoreCreatedOnSlave()
        {
            // Create a store with a single txn in it for sync test purposes
            var client =
                BrightstarService.GetClient("type=embedded;storesDirectory=c:\\brightstar\\coreA");
            client.CreateStore("TestStore1");
            var result = client.ExecuteTransaction("TestStore1", null, null,
                                      "<http://brightstardb.com/foo> <http://brightstardb.com/isa> <http://brightstardb.com/bar>.");
            Assert.IsTrue(result.JobCompletedOk, "Setup transaction failed on TestStore1");

            _coreA.Start(10001);
            _coreB.Start(10002);
            _clusterManager.Start();

            WaitForState(_coreB, CoreState.RunningSlave, 5000);
            Assert.IsTrue(Directory.Exists("c:\\brightstar\\coreB\\TestStore1"));
        }

        [TestMethod]
        public void TestSimpleUpdate()
        {
            // Create an initial store for sync
            var client =
                BrightstarService.GetClient("type=embedded;storesDirectory=c:\\brightstar\\coreA");
            client.CreateStore("TestStore2");
            var result = client.ExecuteTransaction("TestStore2", null, null,
                                      "<http://brightstardb.com/foo> <http://brightstardb.com/isa> <http://brightstardb.com/bar>.");
            Assert.IsTrue(result.JobCompletedOk, "Setup transaction failed on TestStore2");

            _coreA.Start(10001);
            _coreB.Start(10002);
            _clusterManager.Start();

            WaitForState(_coreB, CoreState.RunningSlave, 5000);
            Assert.IsTrue(Directory.Exists("c:\\brightstar\\coreB\\TestStore2"));

            var results = _coreB.ProcessQuery("TestStore2", "SELECT * WHERE { <http://brightstardb.com/foo> ?s ?p }");
            XDocument resultsDoc = XDocument.Parse(results);
            Assert.AreEqual(1, resultsDoc.SparqlResultRows().Count());

            _coreA.ProcessTransaction(
                new ClusterUpdateTransaction
                    {
                        StoreId = "TestStore2",
                        Inserts =
                            "<http://brightstardb.com/foo> <http://brightstardb.com/property> <http://brightstardb.com/value> ."
                    });

            Thread.Sleep(500);
            results = _coreB.ProcessQuery("TestStore2", "SELECT * WHERE {<http://brightstardb.com/foo> ?s ?p }");
            XDocument newResultsDoc = XDocument.Parse(results);
            Assert.AreEqual(2, newResultsDoc.SparqlResultRows().Count());
        }

        [TestMethod]
        public void TestSparqlUpdate()
        {
            // Create an initial store for sync
            var client =
                BrightstarService.GetClient("type=embedded;storesDirectory=c:\\brightstar\\coreA");
            client.CreateStore("TestStore4");
            var result = client.ExecuteTransaction("TestStore4", null, null,
                                      "<http://brightstardb.com/foo> <http://brightstardb.com/isa> <http://brightstardb.com/bar>.");
            Assert.IsTrue(result.JobCompletedOk, "Setup transaction failed on TestStore2");

            _coreA.Start(10001);
            _coreB.Start(10002);
            _clusterManager.Start();

            WaitForState(_coreB, CoreState.RunningSlave, 5000);
            Assert.IsTrue(Directory.Exists("c:\\brightstar\\coreB\\TestStore4"));

            var results = _coreB.ProcessQuery("TestStore4", "SELECT * WHERE { <http://brightstardb.com/foo> ?s ?p }");
            XDocument resultsDoc = XDocument.Parse(results);
            Assert.AreEqual(1, resultsDoc.SparqlResultRows().Count());

            _coreA.ProcessUpdate(new ClusterSparqlTransaction {StoreId = "TestStore4", 
                                                               Expression =
@"
INSERT DATA
{ 
  <http://brightstardb.com/foo> <http://brightstardb.com/property> <http://brightstardb.com/value> .
}
"
            });

            Thread.Sleep(500);
            results = _coreB.ProcessQuery("TestStore4", "SELECT * WHERE {<http://brightstardb.com/foo> ?s ?p }");
            XDocument newResultsDoc = XDocument.Parse(results);
            Assert.AreEqual(2, newResultsDoc.SparqlResultRows().Count());
        }


        [TestMethod]
        public void TestSyncWithEmptyStoreUpdate()
        {
            // Create an initial store for sync
            var client = BrightstarService.GetClient("type=embedded;storesDirectory=c:\\brightstar\\coreA");
            client.CreateStore("TestStore3");

            _coreA.Start(10001);
            _coreB.Start(10002);
            _clusterManager.Start();

            WaitForState(_coreB, CoreState.RunningSlave, 5000);
            Assert.IsTrue(Directory.Exists("c:\\brightstar\\coreB\\TestStore3"));

            _coreA.ProcessTransaction(
                new ClusterUpdateTransaction
                {
                    StoreId = "TestStore3",
                    Inserts =
                        "<http://brightstardb.com/foo> <http://brightstardb.com/property> <http://brightstardb.com/value> ."
                });

            Thread.Sleep(500);
            var results = _coreB.ProcessQuery("TestStore3", "SELECT * WHERE {<http://brightstardb.com/foo> ?s ?p }");
            XDocument newResultsDoc = XDocument.Parse(results);
            Assert.AreEqual(1, newResultsDoc.SparqlResultRows().Count());
        }

        private void WaitForState(NodeCore node, CoreState expectedState, int timeToWait)
        {
            for(int i = 0; i < 10; i++)
            {
                Thread.Sleep(timeToWait/10);
                if (node.GetStatus() == expectedState) return;
            }
            Thread.Sleep(timeToWait/10);
            Assert.AreEqual(expectedState, node.GetStatus(), "Node did not enter the expected state after {0} milliseconds", timeToWait);
        }
    }
}
