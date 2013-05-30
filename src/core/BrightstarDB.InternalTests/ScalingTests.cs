using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrightstarDB.Client;
using BrightstarDB.Model;
using BrightstarDB.Rdf;
using BrightstarDB.Server;
using BrightstarDB.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests
{
    [TestClass]
    [Ignore]
    public class ScalingTests
    {
        private IStoreManager _storeManager;
        public const string Text =
    @"Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum. Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";


        [TestInitialize]
        public void SetUp()
        {
            _storeManager = StoreManagerFactory.GetStoreManager();
        }

        [TestMethod]
        public void TestImportAndValidateSingleFile()
        {
            const string fileName = "bsbm_1M.nt";
            const string storeName = "ImportAndValidate";

            if (_storeManager.DoesStoreExist(storeName))
            {
                _storeManager.DeleteStore(storeName);
                while (_storeManager.DoesStoreExist(storeName))
                {
                    Thread.Sleep(10);
                }
            }
            using (var store = _storeManager.CreateStore(storeName))
            {
                var jobId = Guid.NewGuid();
                using (var triplesStream = File.OpenRead(fileName))
                {
                    store.Import(jobId, triplesStream);
                }
                store.Commit(jobId);                
            }

            using(var triplesStream = File.OpenRead(fileName))
            {
                using (var store = _storeManager.OpenStore(storeName))
                {
                    var validatorSink = new ValidatorSink(store);
                    var parser = new NTriplesParser();
                    parser.Parse(triplesStream, validatorSink, Constants.DefaultGraphUri);
                    Console.WriteLine("Validated {0} triples in store", validatorSink.ValidationCount);
                }
            }

        }

        class ValidatorImportSink : ITripleSink
        {
            private StoreTripleSink _storeTripleSink;
            private List<Triple> _importedTriples;
            private IStore _store;
            private bool _checkForTriple;
            public ValidatorImportSink(IStore store, Guid jobId)
            {
                _store = store;
                _storeTripleSink = new StoreTripleSink(store, jobId);
                _importedTriples = new List<Triple>();
            }

            #region Implementation of ITripleSink

            /// <summary>
            /// Handler method for an individual RDF statement
            /// </summary>
            /// <param name="subject">The statement subject resource URI</param>
            /// <param name="subjectIsBNode">True if the value of <paramref name="subject"/> is a BNode identifier</param>
            /// <param name="predicate">The predicate resource URI</param>
            /// <param name="predicateIsBNode">True if the value of <paramref name="predicate"/> is a BNode identifier.</param>
            /// <param name="obj">The object of the statement</param>
            /// <param name="objIsBNode">True if the value of <paramref name="obj"/> is a BNode identifier.</param>
            /// <param name="objIsLiteral">True if the value of <paramref name="obj"/> is a literal string</param>
            /// <param name="dataType">The datatype URI for the object literal or null if the object is not a literal</param>
            /// <param name="langCode">The language code for the object literal or null if the object is not a literal</param>
            /// <param name="graphUri">The graph URI for the statement</param>
            public void Triple(string subject, bool subjectIsBNode, string predicate, bool predicateIsBNode, string obj, bool objIsBNode, bool objIsLiteral, string dataType, string langCode, string graphUri)
            {
                
                if (
                    subject.Equals(
                        "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer1/Producer1") &&
                    predicate.Equals("http://xmlns.com/foaf/0.1/homepage") &&
                    obj.Equals("http://www.Producer1.com/"))
                {
                    _checkForTriple = true;
                }
                /*
                var importTriple = new Triple
                                       {
                                           Subject = subject,
                                           Predicate = predicate,
                                           Object = obj,
                                           IsLiteral = objIsLiteral,
                                           DataType = dataType,
                                           LangCode = langCode,
                                           Graph = graphUri
                                       };
                if (!subjectIsBNode && !predicateIsBNode && !objIsBNode)
                {
                    _importedTriples.Add(importTriple);
                }
                 */
                _storeTripleSink.Triple(subject, subjectIsBNode, predicate, predicateIsBNode,
                                        obj, objIsBNode, objIsLiteral, dataType, langCode, graphUri);
                if (_checkForTriple)
                {
                    Assert.AreEqual(1, _store.Match(
                        "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer1/Producer1",
                        "http://xmlns.com/foaf/0.1/homepage",
                        "http://www.Producer1.com/", false, null, null, Constants.DefaultGraphUri).Count(),
                        "Could not find test triple after import of triple <{0}> <{1}> <{2}>",
                        subject, predicate, obj);
                }
                /*
                foreach(var t in _importedTriples)
                {
                    Assert.IsTrue(_store.Match(t.Subject, t.Predicate, t.Object, t.IsLiteral, t.DataType, t.LangCode, t.Graph).Count() == 1,
                        "Could not find a match for triple {0} after import of triple {1}", t, importTriple);
                }
                 */
            }

            #endregion
        }
        class ValidatorSink : ITripleSink
        {
            private IStore _store;
            private int _validationCount;

            public int ValidationCount { get { return _validationCount; } }
            public ValidatorSink(IStore store)
            {
                _store = store;
            }
            #region Implementation of ITripleSink

            /// <summary>
            /// Handler method for an individual RDF statement
            /// </summary>
            /// <param name="subject">The statement subject resource URI</param>
            /// <param name="subjectIsBNode">True if the value of <paramref name="subject"/> is a BNode identifier</param>
            /// <param name="predicate">The predicate resource URI</param>
            /// <param name="predicateIsBNode">True if the value of <paramref name="predicate"/> is a BNode identifier.</param>
            /// <param name="obj">The object of the statement</param>
            /// <param name="objIsBNode">True if the value of <paramref name="obj"/> is a BNode identifier.</param>
            /// <param name="objIsLiteral">True if the value of <paramref name="obj"/> is a literal string</param>
            /// <param name="dataType">The datatype URI for the object literal or null if the object is not a literal</param>
            /// <param name="langCode">The language code for the object literal or null if the object is not a literal</param>
            /// <param name="graphUri">The graph URI for the statement</param>
            public void Triple(string subject, bool subjectIsBNode, string predicate, bool predicateIsBNode, string obj, bool objIsBNode, bool objIsLiteral, string dataType, string langCode, string graphUri)
            {
                if (!subjectIsBNode && !predicateIsBNode && !objIsBNode)
                {
                    Assert.AreEqual(1, _store.Match(subject, predicate, obj, objIsLiteral, dataType, langCode, graphUri).Count(),
                        "Expected match for <{0}> <{1}> <{2}>", subject, predicate, obj);
                    _validationCount++;
                }
            }

            #endregion
        }
        [TestMethod]
        public void TestRepeatedSmallUnitsOfWork()
        {
            var st = DateTime.UtcNow;
            // for the embedded stores the context needs to be common.
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + BrightstarDB.Configuration.StoreLocation + "\\"));
            Assert.IsNotNull(context);

            var storeId = Guid.NewGuid().ToString();
            context.CreateStore(storeId);

            var tasks = new List<Task>();

            for (var i=0;i < 10;i++)
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

        private static void ExecuteSmallUnitOfWork(IDataObjectContext context, string storeId)
        {
            var contextName = Guid.NewGuid();
            var rnd = new Random();
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    var DataObjectStore = context.OpenStore(storeId);

                    // create 50 themes
                    var themes = new IDataObject[50];
                    for (int t = 0; t < 50; t++)
                    {
                        var theme = DataObjectStore.MakeDataObject("http://www.np.com/" + contextName + "/themes/" + t);
                        theme.SetProperty("http://www.np.com/types/label", contextName + "_" + t);
                        theme.SetProperty("http://www.np.com/types/description", contextName + "_desc_" + t);
                        themes[t] = theme;
                    }

                    // 200 documents
                    var docs = new IDataObject[250];
                    for (int t = 0; t < 250; t++)
                    {
                        var doc = DataObjectStore.MakeDataObject("http://www.np.com/" + contextName + "/docs/" + t);
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
                        var email = DataObjectStore.MakeDataObject("http://www.np.com/" + contextName + "/emails/" + t);
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
                    DataObjectStore.SaveChanges();
                    var et = DateTime.UtcNow;
                    var duration = et.Subtract(st).TotalMilliseconds;
                    Console.WriteLine(duration);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception " + ex);
            }
            Console.WriteLine("Finished " + contextName);
        }


        [TestMethod]
        public void TestDataObjectLookupScales()
        {
            // generate data for 1million unique resources and common vocab
            // kickoff a new thread for each activity
        }

        [TestMethod]
        public void TestInsert1000000TriplesWithLargeLiteralProperty()
        {
            var sid = Guid.NewGuid().ToString();
            var store = _storeManager.CreateStore(BrightstarDB.Configuration.StoreLocation + "\\" + sid);

            var start = DateTime.UtcNow;
            for (int i = 0; i < 10000; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    store.InsertTriple("http://www.networkedplanet.com/people/person" + i, "http://www.networkedplanet.com/model/hasSkill", Text + j, true, RdfDatatypes.String, "en", Constants.DefaultGraphUri.ToString());
                }
            }
            var end = DateTime.UtcNow;
            Console.WriteLine("Insert triples took " + (end - start).TotalMilliseconds);

            start = DateTime.UtcNow;
            store.Commit(Guid.Empty);
            end = DateTime.UtcNow;
            Console.WriteLine("Commit triples took " + (end - start).TotalMilliseconds);
        }

        [TestMethod]
        public void TestInsert10000TriplesWithUniqueSubjectAndUniqueLiteral()
        {
            var sid = Guid.NewGuid().ToString();
            var store = _storeManager.CreateStore(BrightstarDB.Configuration.StoreLocation + "\\" + sid);
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 10000; i++)
            {
                store.InsertTriple("http://www.networkedplanet.com/people/person" + i, "http://www.networkedplanet.com/model/hasSkill", Text + i, true, RdfDatatypes.String, "en", Constants.DefaultGraphUri.ToString());
            }
            Console.WriteLine("Insert triples took " + sw.ElapsedMilliseconds);
            sw.Reset();
            sw.Start();
            store.Commit(Guid.Empty);
            Console.WriteLine("Commit triples took " + sw.ElapsedMilliseconds);
        }

        [Timeout(5400000), TestMethod]
        public void TestInsert100MTriplesWithSubjectAndObjectReuse()
        {
            var sid = Guid.NewGuid().ToString();
            var store = _storeManager.CreateStore(BrightstarDB.Configuration.StoreLocation + "\\" + sid);

            var start = DateTime.UtcNow;

            for (int k = 0; k < 100; k++)
            {
                for (int i = 0; i < 10000; i++)
                {
                    for (int j = 0; j < 100; j++)
                    {
                        store.InsertTriple("http://www.networkedplanet.com/people/person" + i,
                                           "http://www.networkedplanet.com/model/hasSkill",
                                           "http://www.networkedplanet.com/skills/skill" + j, false,
                                           null, null, Constants.DefaultGraphUri);
                    }
                }
            }

            var end = DateTime.UtcNow;
            Console.WriteLine("Insert triples took " + (end - start).TotalMilliseconds);

            start = DateTime.UtcNow;
            store.Commit(Guid.Empty);
            end = DateTime.UtcNow;
            Console.WriteLine("Commit triples took " + (end - start).TotalMilliseconds);
        }


        [TestMethod]
        public void TestInsert1000000TriplesWithSubjectAndObjectReuse()
        {
            var sid = Guid.NewGuid().ToString();
            var store = _storeManager.CreateStore(BrightstarDB.Configuration.StoreLocation + "\\" + sid);

            var start = DateTime.UtcNow;
            for (int i = 0; i < 10000; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    store.InsertTriple("http://www.networkedplanet.com/people/person" + i, "http://www.networkedplanet.com/model/hasSkill", "http://www.networkedplanet.com/skills/skill" + j, false, null, null, Constants.DefaultGraphUri.ToString());
                }
            }
            var end = DateTime.UtcNow;
            Console.WriteLine("Insert triples took " + (end - start).TotalMilliseconds);

            start = DateTime.UtcNow;
            store.Commit(Guid.Empty);
            end = DateTime.UtcNow;
            Console.WriteLine("Commit triples took " + (end - start).TotalMilliseconds);
        }

        [TestMethod]
        public void TestInsert1000000TriplesWithUniqueSubjectResources()
        {
            var sid = Guid.NewGuid().ToString();
            var store = _storeManager.CreateStore(BrightstarDB.Configuration.StoreLocation + "\\" + sid);

            var start = DateTime.UtcNow;
            for (int i = 0; i < 1000000; i++)
            {
                var t = new Triple
                {
                    Subject = "http://www.networkedplanet.com/people/" + i,
                    Predicate = "http://www.networkedplanet.com/model/isa",
                    Object = "http://www.networkedplanet.com/types/person"
                };
                store.InsertTriple(t);
            }
            var end = DateTime.UtcNow;
            Console.WriteLine("Insert triples took " + (end - start).TotalMilliseconds);

            start = DateTime.UtcNow;
            store.Commit(Guid.Empty);
            end = DateTime.UtcNow;
            Console.WriteLine("Commit triples took " + (end - start).TotalMilliseconds);
        }

        /* Cannot run this test any more because ulong is not in AbstractStoreManager.PersistentTypeIdentifiers
        [TestMethod]
        public void TestBtreePerformanceAfterManyInsertsOfLongs()
        {
            var timer = new Stopwatch();
            timer.Start();
            var storeId = Guid.NewGuid();
            var store = _storeManager.CreateStore(Configuration.StoreLocation + "\\" + storeId) as Store;
            var btree = store.MakeNewTree<Resource>(201);

            for (ulong i = 0; i < 25000000; i++)
            {
                btree.Insert(i, null);                
            }

            store.Commit(Guid.Empty);

            timer.Stop();
            Console.WriteLine("time to insert and persist 25000000 longs " + timer.ElapsedMilliseconds);
        }
        */
#if BTREESTORE
        [TestMethod]
        public void TestBtreePerformanceAfterManyInsertsOfObjectRefPayload()
        {
            var timer = new Stopwatch();
            timer.Start();
            var storeId = Guid.NewGuid();
            var store = _storeManager.CreateStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            var btree = store.MakeNewTree<ObjectRef>(201);

            for (ulong j = 0; j < 10; j++)
            {
                for (ulong i = 0; i < 1000000; i++)
                {
                    var id = (j*1000000) + i;
                    var objRef = new ObjectRef(id);
                    btree.Insert(id, objRef);
                }
                store.FlushChanges();
            }
            store.Commit(Guid.Empty);

            timer.Stop();
            Console.WriteLine("Time to insert and persist 10 million related resource lists: {0}ms", timer.ElapsedMilliseconds);
        }
#endif

        [TestMethod]
        public void TestQuickLookupIndex()
        {
            var dict = new Dictionary<string, ulong>(10000);
            var timer = new Stopwatch();
            timer.Start();
            ulong val;

            for (ulong i = 0; i < 10000000; i++ )
            {
                var key = "http://www.networkedplanet.com/people/" + i;
                if (!dict.TryGetValue(key, out val))
                {
                    dict.Add(key, i);
                }
            }

            timer.Stop();
            Console.WriteLine("time to check and insert from cache : " + timer.ElapsedMilliseconds);
        }

            [TestMethod]
            public void TestImportAndLookupPerformance()
            {
                if (!File.Exists(BrightstarDB.Configuration.StoreLocation + "\\import\\bsbm_5m.nt"))
                {
                    Assert.Inconclusive("Cannot locate required test file at {0}. Test will not run.",
                        BrightstarDB.Configuration.StoreLocation + "\\import\\bsbm_5m.nt");
                    return;
                }
                var storeId = Guid.NewGuid().ToString();
                _storeManager.CreateStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId);
                var timer = new Stopwatch();
                var storeWorker = new StoreWorker(BrightstarDB.Configuration.StoreLocation, storeId);
                storeWorker.Start();
                timer.Start();
                var jobId = storeWorker.Import("bsbm_5m.nt", Constants.DefaultGraphUri).ToString();
                JobExecutionStatus jobStatus = storeWorker.GetJobStatus(jobId.ToString());
                while (jobStatus.JobStatus == JobStatus.Pending || jobStatus.JobStatus == JobStatus.Started)
                {
                    Thread.Sleep(100);
                    jobStatus = storeWorker.GetJobStatus(jobId.ToString());
                }

                timer.Stop();
                Console.WriteLine("Time to import 5M triples test file: " + timer.ElapsedMilliseconds);

                var store = _storeManager.OpenStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId);
                var validator = new TriplesValidator(store, BrightstarDB.Configuration.StoreLocation + "\\import\\bsbm_5m.nt" );
                timer.Reset();
                timer.Start();
                validator.Run();
                timer.Stop();
                Console.WriteLine("Time to validate 5M triples test file:" + timer.ElapsedMilliseconds);
                if(validator.UnmatchedTriples.Any())
                {
                    Assert.Fail("Validator failed to match {0} triples:\n",
                        validator.UnmatchedTriples.Count,
                        String.Join("\n", validator.UnmatchedTriples)
                        );
                }
            }

            [TestMethod]
            public void TestImportPerformance25M()
            {
                const string fileName = "bsbm_25m.nt";
                if (!File.Exists(BrightstarDB.Configuration.StoreLocation + "\\import\\" + fileName))
                {
                    Assert.Inconclusive("Cannot locate required test file at {0}. Test will not run.",
                        BrightstarDB.Configuration.StoreLocation + "\\import\\"+fileName);
                    return;
                }
                var storeId = Guid.NewGuid().ToString();
                _storeManager.CreateStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId);
                var timer = new Stopwatch();
                var storeWorker = new StoreWorker(BrightstarDB.Configuration.StoreLocation, storeId);
                storeWorker.Start();
                timer.Start();
                var jobId = storeWorker.Import(fileName, Constants.DefaultGraphUri).ToString();
                JobExecutionStatus jobStatus = storeWorker.GetJobStatus(jobId);
                while (jobStatus.JobStatus == JobStatus.Pending || jobStatus.JobStatus == JobStatus.Started)
                {
                    Thread.Sleep(100);
                    jobStatus = storeWorker.GetJobStatus(jobId);
                }
                timer.Stop();
                Console.WriteLine("Time to import test file '" + fileName + "': " + timer.ElapsedMilliseconds);
            }

        [TestMethod]
        public void TestIntersectQueryPerformance()
        {
            var storeId = Guid.NewGuid().ToString();
            var store = _storeManager.CreateStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId);
            var rand = new Random();
            for(int i = 0; i < 1000000; i++)
            {
                store.InsertTriple("http://www.bs.com/doc/" + i,
                    "http://www.bs.com/tag",
                    "http://www.bs.com/category/" + rand.Next(50), false, null, null, Constants.DefaultGraphUri);
                store.InsertTriple("http://www.bs.com/doc/" + i,
                    "http://www.bs.com/tag",
                    "http://www.bs.com/category/" + rand.Next(50), false, null, null, Constants.DefaultGraphUri);
            }
            store.Commit(Guid.Empty);

            var timer = new Stopwatch();
            var sparql =
                "SELECT ?doc WHERE { ?doc <http://www.bs.com/tag> <http://www.bs.com/category/1> . ?doc <http://www.bs.com/tag> <http://www.bs.com/category/2> . }";
            timer.Start();
            var results = store.ExecuteSparqlQuery(sparql, SparqlResultsFormat.Xml);
            timer.Stop();
            Console.WriteLine("1 query took {0}ms", timer.ElapsedMilliseconds );
        }

        /// <summary>
        /// A triple sink that tests for the presence of each triple it receives in a store
        /// </summary>
        /// <remarks>This validator currently ignores all triples that contain bnodes as it is not possible to directly match them in the store.</remarks>
        class TriplesValidator : ITripleSink
        {
            private readonly IStore _store;
            private readonly string _srcPath;
            private readonly List<string> _unmatchedTriples;

            public TriplesValidator(IStore store, string srcPath)
            {
                _store = store;
                _srcPath = srcPath;
                _unmatchedTriples = new List<string>();
            }

            public void Run()
            {
                var p = new NTriplesParser();
                using (var fileReader = new StreamReader(_srcPath))
                {
                    p.Parse(fileReader, this, Constants.DefaultGraphUri);
                }
            }

            public List<string> UnmatchedTriples { get { return _unmatchedTriples; } }

            #region Implementation of ITripleSink

            /// <summary>
            /// Handler method for an individual RDF statement
            /// </summary>
            /// <param name="subject">The statement subject resource URI</param>
            /// <param name="subjectIsBNode">True if the value of <paramref name="subject"/> is a BNode identifier</param>
            /// <param name="predicate">The predicate resource URI</param>
            /// <param name="predicateIsBNode">True if the value of <paramref name="predicate"/> is a BNode identifier.</param>
            /// <param name="obj">The object of the statement</param>
            /// <param name="objIsBNode">True if the value of <paramref name="obj"/> is a BNode identifier.</param>
            /// <param name="objIsLiteral">True if the value of <paramref name="obj"/> is a literal string</param>
            /// <param name="dataType">The datatype URI for the object literal or null if the object is not a literal</param>
            /// <param name="langCode">The language code for the object literal or null if the object is not a literal</param>
            /// <param name="graphUri">The graph URI for the statement</param>
            public void Triple(string subject, bool subjectIsBNode, string predicate, bool predicateIsBNode, string obj, bool objIsBNode, bool objIsLiteral, string dataType, string langCode, string graphUri)
            {
                if (subjectIsBNode || predicateIsBNode || objIsBNode )
                {
                    return;
                }
                if (!_store.Match(subject, predicate, obj, objIsLiteral, dataType, langCode, graphUri).Any())
                {
                    _unmatchedTriples.Add(Stringify(subject, predicate, obj, objIsLiteral, dataType, langCode));
                }
            }

            #endregion

            private static string Stringify(string s, string p, string o, bool isLit, string dt, string lc)
            {
                if (isLit)
                {
                    return string.Format("<{0}> <{1}> \"{2}\"^^{3}@{4}", s, p, o, dt, lc);
                }
                return string.Format("<{0}> <{1}> <{2}>", s, p, o);
            }
        }
    }
}
