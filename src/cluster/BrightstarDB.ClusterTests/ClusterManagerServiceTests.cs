using System;
using System.ServiceModel;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml;
using BrightstarDB.ClusterManager;
using BrightstarDB.ClusterNode;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.ClusterTests
{
    [TestClass]
    public class ClusterManagerServiceTests
    {
        private ServiceHost _serviceHost;
        private NodeCore _coreA;
        private NodeCore _coreB;

        public ClusterManagerServiceTests()
        {
            TestUtils.ResetDirectory("c:\\brightstar\\coreA");
            TestUtils.ResetDirectory("c:\\brightstar\\coreB");
            Thread.Sleep(2000);
            _coreA = new NodeCore("c:\\brightstar\\coreA");
            _coreB = new NodeCore("c:\\brightstar\\coreB");

        }

        private void StartClusterManagerService()
        {
            var serviceHostFactory = new ClusterManagerServiceHostFactory();
            _serviceHost = serviceHostFactory.CreateServiceHost();
            _serviceHost.Open();
        }

        [TestCleanup]
        public void CleanUp()
        {
            if (_serviceHost != null)
            {
                _serviceHost.Close();
                _serviceHost = null;
            }
            if (_coreA != null)
            {
                _coreA.Stop();
            }
            if (_coreB != null)
            {
                _coreB.Stop();
            }
        }

        ~ClusterManagerServiceTests()
        {
            if (_serviceHost != null)
            {
                _serviceHost.Close();
                _serviceHost = null;
            }
        }

        [TestMethod]
        public void TestUnavailableWithNoNodes()
        {
            StartClusterManagerService();
            var endpointUri = "http://127.0.0.1:9090/brightstarcluster";
            var binding = new BasicHttpContextBinding
            {
                MaxReceivedMessageSize = Int32.MaxValue,
                SendTimeout = TimeSpan.FromMinutes(30),
                TransferMode = TransferMode.StreamedResponse,
                ReaderQuotas = XmlDictionaryReaderQuotas.Max
            };
            var endpointAddress = new EndpointAddress(endpointUri);
            
            var client = new BrightstarClusertManagerServiceClient (binding, endpointAddress);
            var clusterDescription = client.GetClusterDescription();
            Assert.IsNotNull(clusterDescription);
            Assert.AreEqual(ClusterStatus.Unavailable, clusterDescription.Status);
        }

        [TestMethod]
        public void TestReadOnlyWithMasterNode()
        {
            _coreA.Start(10001);
            StartClusterManagerService();
            var client = GetClusterClient();
            var clusterDescription = client.GetClusterDescription();
            Assert.IsNotNull(clusterDescription);
            Assert.AreEqual(ClusterStatus.ReadOnly, clusterDescription.Status);
        }

        [TestMethod]
        public void TestWriteableWithBothNodes()
        {
            _coreA.Start(10001);
            _coreB.Start(10002);
            StartClusterManagerService();
            var client = GetClusterClient();
            var clusterDescription = client.GetClusterDescription();
            Assert.IsNotNull(clusterDescription);
            Assert.AreEqual(ClusterStatus.Available, clusterDescription.Status);
            Assert.AreEqual("tcp://127.0.0.1:8095/brightstar", clusterDescription.MasterTcpAddress.ToString());
            Assert.AreEqual("http://127.0.0.1:8090/brightstar", clusterDescription.MasterHttpAddress.ToString());
        }

        private static BrightstarClusertManagerServiceClient GetClusterClient()
        {
            const string endpointUri = "http://127.0.0.1:9090/brightstarcluster";
            var binding = new BasicHttpContextBinding
                              {
                                  MaxReceivedMessageSize = Int32.MaxValue,
                                  SendTimeout = TimeSpan.FromMinutes(30),
                                  TransferMode = TransferMode.StreamedResponse,
                                  ReaderQuotas = XmlDictionaryReaderQuotas.Max
                              };
            var endpointAddress = new EndpointAddress(endpointUri);

            var client = new BrightstarClusertManagerServiceClient(binding, endpointAddress);
            return client;
        }
    }
}
