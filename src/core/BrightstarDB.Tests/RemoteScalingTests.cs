// TODO: Reinstate this test fixture when the REST service is reinstated
/*
#if NETCOREAPP20
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BrightstarDB.Client;
using NUnit.Framework;

namespace BrightstarDB.Tests
{
    [Ignore("Move these tests to performance test suite")]
    public class RemoteScalingTests : ClientTestBase
    {
        private static string _importDirPath;

        [OneTimeSetUp]
        public void SetUp()
        {
            StartService();
            var importDir = Path.Combine(Configuration.StoreLocation, "import");
            if (!Directory.Exists(importDir))
            {
                Directory.CreateDirectory(importDir);
            }
            _importDirPath = importDir;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            CloseService();
        }

        [Test]
        [Category("ScalingTests")]
        public void TestRepeatedSmallUnitsOfWork()
        {
            // ServicePointManager.DefaultConnectionLimit = 1000;
            var sp= ServicePointManager.FindServicePoint(new Uri("http://localhost:8090/brightstar"));
            sp.ConnectionLimit = 64;

            var st = DateTime.UtcNow;
            IDataObjectContext context = new HttpDataObjectContext(new ConnectionString("type=http;endpoint=http://localhost:8090/brightstar"));
            Assert.IsNotNull(context);

            var storeId = Guid.NewGuid().ToString();
            context.CreateStore(storeId);

            var tasks = new List<Task>();

            for (var i = 0; i < 10; i++)
            {
                var t = new Task(() => ExecuteSmallUnitOfWork(context, storeId));
                tasks.Add(t);
                t.Start();
            }

            Task.WaitAll(tasks.ToArray());
            var et = DateTime.UtcNow;
            var duration = et.Subtract(st).TotalMilliseconds;
            Console.WriteLine(duration);
        }

        [Test]
        [Category("ScalingTests")]
        public void TestBulkLoad24M()
        {
            var testTarget = new FileInfo(_importDirPath + Path.DirectorySeparatorChar + "BSBM_24M.nt");
            if (!testTarget.Exists)
            {
                var testSource = new FileInfo("BSBM_24M.nt");
                if (!testSource.Exists)
                {
                    Assert.Inconclusive("Could not locate test source file {0}. Test will not run", testSource.FullName);
                    return;
                }
                testSource.CopyTo(_importDirPath + Path.DirectorySeparatorChar + "BSBM_24M.nt");
            }

            var timer = new Stopwatch();
            timer.Start();
            var bc = BrightstarService.GetClient("type=http;endpoint=http://localhost:8090/brightstar");
            var storeName = Guid.NewGuid().ToString();
            bc.CreateStore(storeName);
            var jobInfo = bc.StartImport(storeName, "BSBM_24M.nt", null);
            while (!jobInfo.JobCompletedOk)
            {
                Thread.Sleep(3000);
                jobInfo = bc.GetJobInfo(storeName, jobInfo.JobId);
            }
            timer.Stop();

            Console.WriteLine("24M triples imported in {0} ms", timer.ElapsedMilliseconds);
        }

        [Test]
        [Category("ScalingTests")]
        public void TestBulkLoad370K()
        {
            var testTarget = new FileInfo(_importDirPath + Path.DirectorySeparatorChar + "BSBM_370k.nt");
            if (!testTarget.Exists)
            {
                var testSource = new FileInfo("BSBM_370k.nt");
                if (!testSource.Exists)
                {
                    Assert.Inconclusive("Could not locate test source file {0}. Test will not run", testSource.FullName);
                    return;
                }
                testSource.CopyTo(_importDirPath + Path.DirectorySeparatorChar + "BSBM_370k.nt");
            }

            var timer = new Stopwatch();
            timer.Start();
            var bc = BrightstarService.GetClient("type=http;endpoint=http://localhost:8090/brightstar");
            var storeName = Guid.NewGuid().ToString();
            bc.CreateStore(storeName);
            var jobInfo = bc.StartImport(storeName, "BSBM_370k.nt", null);
            while (!jobInfo.JobCompletedOk)
            {
                Thread.Sleep(1000);
                jobInfo = bc.GetJobInfo(storeName, jobInfo.JobId);
            }
            timer.Stop();
            Console.WriteLine("370K Triples imported in {0} ms.", timer.ElapsedMilliseconds);
        }

        private static void ExecuteSmallUnitOfWork(IDataObjectContext context, string storeId)
        {
            var contextName = Guid.NewGuid();
            var rnd = new Random();
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    var proxyStore = context.OpenStore(storeId);

                    // create 50 themes
                    var themes = new IDataObject[50];
                    for (int t = 0; t < 50; t++)
                    {
                        var theme = proxyStore.MakeDataObject("http://www.np.com/" + contextName + "/themes/" + t);
                        theme.SetProperty("http://www.np.com/types/label", contextName + "_" + t);
                        theme.SetProperty("http://www.np.com/types/description", contextName + "_desc_" + t);
                        themes[t] = theme;
                    }

                    // 200 documents
                    var docs = new IDataObject[250];
                    for (int t = 0; t < 250; t++)
                    {
                        var doc = proxyStore.MakeDataObject("http://www.np.com/" + contextName + "/docs/" + t);
                        doc.SetProperty("http://www.np.com/types/label", contextName + "_" + t);
                        doc.SetProperty("http://www.np.com/types/description", contextName + "_desc_" + t);
                        doc.SetProperty("http://www.np.com/types/created", DateTime.UtcNow);
                        doc.SetProperty("http://www.np.com/types/published", DateTime.UtcNow);
                        doc.SetProperty("http://www.np.com/types/author", "Graham " + contextName + t);

                        doc.AddProperty("http://www.np.com/types/classification", themes[rnd.Next(49)]);
                        doc.AddProperty("http://www.np.com/types/classification", themes[rnd.Next(49)]);
                        doc.AddProperty("http://www.np.com/types/classification", themes[rnd.Next(49)]);
                        docs[t] = doc;
                    }

                    // 200 emails
                    var emails = new IDataObject[200];
                    for (int t = 0; t < 200; t++)
                    {
                        var email = proxyStore.MakeDataObject("http://www.np.com/" + contextName + "/emails/" + t);
                        email.SetProperty("http://www.np.com/types/label", contextName + "_" + t);
                        email.SetProperty("http://www.np.com/types/description", contextName + "_desc_" + t);
                        email.SetProperty("http://www.np.com/types/written", DateTime.UtcNow);
                        email.SetProperty("http://www.np.com/types/received", DateTime.UtcNow);
                        email.SetProperty("http://www.np.com/types/responded", DateTime.UtcNow);

                        email.AddProperty("http://www.np.com/types/classification", themes[rnd.Next(49)]);
                        email.AddProperty("http://www.np.com/types/classification", themes[rnd.Next(49)]);
                        email.AddProperty("http://www.np.com/types/classification", themes[rnd.Next(49)]);

                        emails[t] = email;
                    }

                    var st = DateTime.UtcNow;
                    proxyStore.SaveChanges();
                    var et = DateTime.UtcNow;
                    var duration = et.Subtract(st).TotalMilliseconds;
                    Console.WriteLine(duration);
                    Thread.Yield();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception " + ex.Message);
            }
            Console.WriteLine("Finished " + contextName);
        }
    }
}
#endif
*/