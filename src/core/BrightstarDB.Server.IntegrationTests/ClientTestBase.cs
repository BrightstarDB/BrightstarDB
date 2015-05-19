using System.Threading;
using System.Xml.Linq;
using BrightstarDB.Server.Modules.Configuration;
using NUnit.Framework;
#if !PORTABLE
using System;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules;
using BrightstarDB.Server.Modules.Authentication;
using BrightstarDB.Server.Modules.Permissions;
using Nancy.Bootstrapper;
using Nancy.Hosting.Self;

namespace BrightstarDB.Server.IntegrationTests
{
    public class ClientTestBase
    {
        private readonly INancyBootstrapper _bootstrapper;
        private static NancyHost _serviceHost;
        private static bool _closed;
        private static readonly object HostLock = new object();

        public ClientTestBase()
            : this(new BrightstarBootstrapper(
                       BrightstarService.GetClient(),
                       new IAuthenticationProvider[] {new NullAuthenticationProvider()},
                       new FallbackStorePermissionsProvider(StorePermissions.All, StorePermissions.All),
                       new FallbackSystemPermissionsProvider(SystemPermissions.All, SystemPermissions.All),
                       new CorsConfiguration()))
        {
        }

        public ClientTestBase(INancyBootstrapper bootstrapper)
        {
            this._bootstrapper = bootstrapper;
        }

        protected void StartService()
        {
            StartServer();
        }

        protected void CloseService()
        {
            lock (HostLock)
            {
                _serviceHost.Stop();
                _closed = true;
            }
        }

        private void StartServer()
        {
            lock (HostLock)
            {
#if SDK_TESTS
    // We assume that the test framework starts up the service for us.
#else
                if (_serviceHost == null || _closed)
                {
                    _serviceHost = new NancyHost(_bootstrapper,
                                                 new HostConfiguration {AllowChunkedEncoding = false},
                                                 new Uri("http://localhost:8090/brightstar/"));
                    _serviceHost.Start();
                }
#endif
            }
        }

        public static void AssertTriplePatternInGraph(IBrightstarService client, string storeName, string triplePattern,
                                      string graphUri)
        {
            var sparql = "ASK { GRAPH <" + graphUri + "> {" + triplePattern + "}}";
            var resultsDoc = XDocument.Load(client.ExecuteQuery(storeName, sparql));
            Assert.IsTrue(resultsDoc.SparqlBooleanResult());
        }

        public static void AssertTriplePatternInDefaultGraph(IBrightstarService client, string storeName,
                                                              string triplePattern)
        {
            var sparql = "ASK {{" + triplePattern + "}}";
            var resultsDoc = XDocument.Load(client.ExecuteQuery(storeName, sparql));
            Assert.IsTrue(resultsDoc.SparqlBooleanResult());
        }

        public static void AssertTriplePatternNotInGraph(IBrightstarService client, string storeName, string triplePattern,
                                      string graphUri)
        {
            var sparql = "ASK { GRAPH <" + graphUri + "> {" + triplePattern + "}}";
            var resultsDoc = XDocument.Load(client.ExecuteQuery(storeName, sparql));
            Assert.IsFalse(resultsDoc.SparqlBooleanResult());
        }

        public static void AssertTriplePatternNotInDefaultGraph(IBrightstarService client, string storeName,
                                                              string triplePattern)
        {
            var sparql = "ASK {{" + triplePattern + "}}";
            var resultsDoc = XDocument.Load(client.ExecuteQuery(storeName, sparql));
            Assert.IsFalse(resultsDoc.SparqlBooleanResult());
        }

        public static void AssertUpdateTransaction(IBrightstarService client, string storeName,
            UpdateTransactionData txnData)
        {
            var job = client.ExecuteTransaction(storeName, txnData);
            job = WaitForJob(job, client, storeName);
            Assert.IsTrue(job.JobCompletedOk, "Expected update transaction to complete successfully");
        }

        public static IJobInfo WaitForJob(IJobInfo job, IBrightstarService client, string storeName)
        {
            var cycleCount = 0;
            while (!job.JobCompletedOk && !job.JobCompletedWithErrors && cycleCount < 100)
            {
                Thread.Sleep(500);
                cycleCount++;
                job = client.GetJobInfo(storeName, job.JobId);
            }
            if (!job.JobCompletedOk && !job.JobCompletedWithErrors)
            {
                Assert.Fail("Job did not complete in time.");
            }
            return job;
        }

        public static string GetStoreUri(string storeName)
        {
            return "http://localhost:8090/brightstar/" + storeName;
        }

    }
}
#endif