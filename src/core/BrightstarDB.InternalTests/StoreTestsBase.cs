using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using BrightstarDB.Client;
using BrightstarDB.Model;
using BrightstarDB.Rdf;
using BrightstarDB.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests
{
    public abstract class StoreTestsBase
    {
        internal abstract IStoreManager StoreManager { get; }

        private static readonly XNamespace SparqlResult = "http://www.w3.org/2005/sparql-results#";

        public const string Text =
            @"Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum. Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

        public virtual void TestOpenStoreFailure()
        {
            var sid = Guid.NewGuid().ToString();
            StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid);
        }

        public virtual void TestCreateStore()
        {
            var sid = Guid.NewGuid().ToString();
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);
            Assert.IsNotNull(store);
        }

        public virtual void TestOpenStore()
        {
            // create store
            var sid = Guid.NewGuid().ToString();
            using (var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid))
            {
                Assert.IsNotNull(store);
            }
            using (var store1 = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid))
            {
                Assert.IsNotNull(store1);
            }
        }

#if SILVERLIGHT
        public virtual void TestDeleteStore()
        {
            var sid = Guid.NewGuid().ToString();
            var store = _storeManager.CreateStore(Configuration.StoreLocation + "\\" + sid);
            Assert.IsNotNull(store);
            var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();
            Assert.IsTrue(
                isolatedStorage.DirectoryExists(Configuration.StoreLocation + "\\" + sid));
            _storeManager.DeleteStore(Configuration.StoreLocation + "\\" + sid);
            Assert.IsFalse(
                isolatedStorage.DirectoryExists(Configuration.StoreLocation + "\\" + sid));
        }
#else
        public virtual void TestDeleteStore()
        {
            var sid = Guid.NewGuid().ToString();
            using (var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid))
            {
                Assert.IsNotNull(store);
            }
            var dir = new DirectoryInfo(Configuration.StoreLocation + "\\" + sid);
            Assert.IsTrue(dir.Exists);

            dir = new DirectoryInfo(Configuration.StoreLocation + "\\" + sid);
            StoreManager.DeleteStore(Configuration.StoreLocation + "\\" + sid);
            Assert.IsFalse(dir.Exists);
        }
#endif

        public virtual void TestInsertTriple()
        {
            var t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/gra",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "http://www.networkedplanet.com/types/person"
                        };

            var sid = Guid.NewGuid().ToString();
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);

            store.InsertTriple(t);
            store.Commit(Guid.Empty);

            store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid, true);
            var triples = store.GetResourceStatements("http://www.networkedplanet.com/people/gra").ToList();
            Assert.AreEqual(1, triples.Count());
            Assert.AreEqual("http://www.networkedplanet.com/people/gra", triples[0].Subject);
            Assert.AreEqual("http://www.networkedplanet.com/model/isa", triples[0].Predicate);
            Assert.AreEqual("http://www.networkedplanet.com/types/person", triples[0].Object);
            Assert.IsFalse(triples[0].IsLiteral);
        }

        public virtual void TestGetAllTriples()
        {
            var t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/gra",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "http://www.networkedplanet.com/types/person"
                        };

            var sid = Guid.NewGuid().ToString();
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);

            store.InsertTriple(t);
            store.Commit(Guid.Empty);

            store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid, true);
            var triples = store.Match(null, null, null, graph: Constants.DefaultGraphUri);
            Assert.AreEqual(1, triples.Count());
        }

        public virtual void TestInsertAndRetrieveTriplesInNamedGraphs()
        {
            var t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/gra",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "http://www.networkedplanet.com/types/person",
                            Graph = "http://www.networkedplanet.com/graphs/1"
                        };

            var t1 = new Triple
                         {
                             Subject = "http://www.networkedplanet.com/people/gra",
                             Predicate = "http://www.networkedplanet.com/model/isa",
                             Object = "http://www.networkedplanet.com/types/person",
                             Graph = "http://www.networkedplanet.com/graphs/2"
                         };

            var sid = Guid.NewGuid().ToString();
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);

            store.InsertTriple(t);
            store.InsertTriple(t1);
            store.Commit(Guid.Empty);

            store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid, true);
            var triples = store.Match(null, null, null, graph: "http://www.networkedplanet.com/graphs/1");
            Assert.AreEqual(1, triples.Count());
        }

        public virtual void TestInsertAndRetrieveTriplesInNamedGraphs2()
        {
            var t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/gra",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "http://www.networkedplanet.com/types/person",
                            Graph = "http://www.networkedplanet.com/graphs/1"
                        };

            var t1 = new Triple
                         {
                             Subject = "http://www.networkedplanet.com/people/gra",
                             Predicate = "http://www.networkedplanet.com/model/isa",
                             Object = "http://www.networkedplanet.com/types/personX",
                             Graph = "http://www.networkedplanet.com/graphs/2"
                         };

            var sid = Guid.NewGuid().ToString();
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);

            store.InsertTriple(t);
            store.InsertTriple(t1);
            store.Commit(Guid.Empty);

            store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid, true);
            var triples = store.Match("http://www.networkedplanet.com/people/gra", null, null,
                                      graph: "http://www.networkedplanet.com/graphs/1");
            Assert.AreEqual(1, triples.Count());

            // test get all triples across graphs
            triples = store.Match(null, null, null, graphs: null);
            Assert.AreEqual(2, triples.Count());
        }

        public virtual void TestDuplicateTriplesAreNotInserted()
        {
            var t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/gra",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "http://www.networkedplanet.com/types/person",
                        };

            var t1 = new Triple
                         {
                             Subject = "http://www.networkedplanet.com/people/gra",
                             Predicate = "http://www.networkedplanet.com/model/isa",
                             Object = "http://www.networkedplanet.com/types/person",
                         };

            var sid = Guid.NewGuid().ToString();
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);

            store.InsertTriple(t);
            store.InsertTriple(t1);
            store.Commit(Guid.Empty);

            store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid, true);
            var triples = store.Match(null, null, null, graph: Constants.DefaultGraphUri);
            Assert.AreEqual(1, triples.Count());
        }

        public virtual void TestDuplicateTriplesAreAllowedInDifferentGraphs()
        {
            var t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/gra",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "http://www.networkedplanet.com/types/person",
                            Graph = "http://www.networkedplanet.com/graphs/1"
                        };

            var t1 = new Triple
                         {
                             Subject = "http://www.networkedplanet.com/people/gra",
                             Predicate = "http://www.networkedplanet.com/model/isa",
                             Object = "http://www.networkedplanet.com/types/person",
                             Graph = "http://www.networkedplanet.com/graphs/2"
                         };

            var sid = Guid.NewGuid().ToString();
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);

            store.InsertTriple(t);
            store.InsertTriple(t1);
            store.InsertTriple(t1);
            store.Commit(Guid.Empty);

            store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid, true);
            var triples = store.Match(null, null, null, graph: "http://www.networkedplanet.com/graphs/2");
            Assert.AreEqual(1, triples.Count());

            triples = store.Match(null, null, null, graph: "http://www.networkedplanet.com/graphs/1");
            Assert.AreEqual(1, triples.Count());
        }

        public virtual void TestInsertMulitpleTriples()
        {
            var sid = Guid.NewGuid().ToString();
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);

            for (int i = 0; i < 100; i++)
            {
                var t = new Triple
                            {
                                Subject = "http://www.networkedplanet.com/people/" + i,
                                Predicate = "http://www.networkedplanet.com/model/isa",
                                Object = "http://www.networkedplanet.com/types/person"
                            };
                store.InsertTriple(t);
            }
            store.Commit(Guid.Empty);

            store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid, true);
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(1, store.GetResourceStatements("http://www.networkedplanet.com/people/" + i).Count());
            }
        }

        public virtual void TestFetchResourceStatements()
        {
            var sid = Guid.NewGuid().ToString();
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);

            var t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/10",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "http://www.networkedplanet.com/types/person",
                            Graph = Constants.DefaultGraphUri
                        };
            store.InsertTriple(t);
            store.Commit(Guid.Empty);

            store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid, true);
            var triples = store.GetResourceStatements("http://www.networkedplanet.com/people/10");
            Assert.AreEqual(1, triples.Count());
        }

        public virtual void TestFetchMultipleResourceStatements()
        {
            var sid = Guid.NewGuid().ToString();
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);

            for (int i = 0; i < 1000; i++)
            {
                var t = new Triple
                            {
                                Subject = "http://www.networkedplanet.com/people/gra",
                                Predicate = "http://www.networkedplanet.com/model/hasSkill",
                                Object = "http://www.networkedplanet.com/skills/" + i
                            };
                store.InsertTriple(t);
            }

            store.Commit(Guid.Empty);

            store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid, true);

            var triples = store.GetResourceStatements("http://www.networkedplanet.com/people/gra");

            Assert.AreEqual(1000, triples.Count());
        }

        public virtual void TestDeleteTriples()
        {
            var sid = Guid.NewGuid().ToString();
            var t1 = new Triple
                         {
                             Subject = "http://www.networkedplanet.com/people/10",
                             Predicate = "http://www.networkedplanet.com/model/isa",
                             Object = "bob",
                             IsLiteral = true
                         };
            var t2 = new Triple
                         {
                             Subject = "http://www.networkedplanet.com/people/10",
                             Predicate = "http://www.networkedplanet.com/model/isa",
                             Object = "kal",
                             IsLiteral = true
                         };

            using (var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid))
            {
                store.InsertTriple(t1);
                store.InsertTriple(t2);
                store.Commit(Guid.Empty);
            }
            // delete triple
            using (var store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid))
            {
                store.DeleteTriple(t2);
                store.Commit(Guid.Empty);
            }
            using (var store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid))
            {
                var triples = store.GetResourceStatements("http://www.networkedplanet.com/people/10");
                Assert.AreEqual(1, triples.Count());
            }
        }

        public virtual void TestInsertAndRetrieveTripleWithLiteral()
        {
            var sid = Guid.NewGuid().ToString();
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);

            var t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/10",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "bob",
                            IsLiteral = true
                        };
            store.InsertTriple(t);
            store.Commit(Guid.Empty);

            // match triple
            store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid, true);

            var matches = store.Match(t.Subject, t.Predicate, t.Object, true,
                                      RdfDatatypes.PlainLiteral, null, Constants.DefaultGraphUri);
            Assert.AreEqual(1, matches.Count());

            matches = store.Match(null, t.Predicate, t.Object, true, RdfDatatypes.PlainLiteral,
                                  null, Constants.DefaultGraphUri);
            Assert.AreEqual(1, matches.Count());

            matches = store.Match(t.Subject, null, t.Object, true, RdfDatatypes.PlainLiteral, null,
                                  Constants.DefaultGraphUri);
            Assert.AreEqual(1, matches.Count());

            matches = store.Match(null, null, t.Object, true, RdfDatatypes.PlainLiteral, null,
                                  Constants.DefaultGraphUri);
            Assert.AreEqual(1, matches.Count());

        }

        public virtual void TestInsertAndRetrieveTripleWithSameLiteralAndDifferentLanguageCode()
        {
            var sid = Guid.NewGuid().ToString();
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);

            var t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/10",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "bob",
                            LangCode = "en",
                            DataType = RdfDatatypes.String,
                            IsLiteral = true
                        };
            store.InsertTriple(t);

            t = new Triple
                    {
                        Subject = "http://www.networkedplanet.com/people/10",
                        Predicate = "http://www.networkedplanet.com/model/isa",
                        Object = "bob",
                        LangCode = "fr",
                        DataType = RdfDatatypes.String,
                        IsLiteral = true
                    };
            store.InsertTriple(t);
            store.Commit(Guid.Empty);

            // match triple
            store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid, true);
            var matches = store.Match(t.Subject, t.Predicate, t.Object, true, RdfDatatypes.String, "fr",
                                      Constants.DefaultGraphUri);
            Assert.AreEqual(1, matches.Count());
            matches = store.Match(t.Subject, t.Predicate, t.Object, true, RdfDatatypes.String, "en",
                                  Constants.DefaultGraphUri);
            Assert.AreEqual(1, matches.Count());

        }

        public virtual void TestInsertAndRetrieveTripleWithSameLiteralAndDifferentDataType()
        {
            var sid = Guid.NewGuid().ToString();
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);

            var t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/10",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "24/03/76",
                            DataType = RdfDatatypes.String,
                            IsLiteral = true
                        };
            store.InsertTriple(t);

            t = new Triple
                    {
                        Subject = "http://www.networkedplanet.com/people/10",
                        Predicate = "http://www.networkedplanet.com/model/isa",
                        Object = "24/03/76",
                        DataType = RdfDatatypes.DateTime,
                        IsLiteral = true
                    };
            store.InsertTriple(t);
            store.Commit(Guid.Empty);

            // match triple
            store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid, true);

            var matches = store.Match(t.Subject, t.Predicate, t.Object, true,
                                      RdfDatatypes.DateTime, null, Constants.DefaultGraphUri);
            Assert.AreEqual(1, matches.Count());
            matches = store.Match(t.Subject, t.Predicate, t.Object, true,
                                  RdfDatatypes.String, null, Constants.DefaultGraphUri);
            Assert.AreEqual(1, matches.Count());
        }

        public virtual void TestInsertAndRetrieveLiteralObjectTriple()
        {
            var sid = Guid.NewGuid().ToString();
            var t = new Triple
            {
                Subject = "http://www.networkedplanet.com/people/10",
                Predicate = "http://www.networkedplanet.com/model/isa",
                Object = "graham",
                LangCode = "en",
                IsLiteral = true
            };
            using (var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid))
            {
                store.InsertTriple(t);
                store.Commit(Guid.Empty);
            }

            // match triple
            using (var store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid))
            {
                var matches = store.Match(t.Subject, t.Predicate, t.Object, true,
                                          RdfDatatypes.PlainLiteral, "en", Constants.DefaultGraphUri).ToList();
                Assert.AreEqual(1, matches.Count());
                var tout = matches.First();
                Assert.AreEqual("en", tout.LangCode);
                Assert.AreEqual(RdfDatatypes.PlainLiteral, tout.DataType);
            }
        }

        public virtual void TestInsertAndRetrieveXmlLiteral()
        {
            var sid = Guid.NewGuid().ToString();
            var doc = new XDocument(
                new XComment("This is a comment"),
                new XElement("Root",
                             new XElement("Child1", "data1"),
                             new XElement("Child2", "data2"),
                             new XElement("Child3", "data3"),
                             new XElement("Child2", "data4"),
                             new XElement("Info5", "info5"),
                             new XElement("Info6", "info6"),
                             new XElement("Info7", "info7"),
                             new XElement("Info8", "info8")
                    )
                );

            var t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/10",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = doc.ToString(),
                            DataType = RdfDatatypes.XmlLiteral,
                            IsLiteral = true
                        };
            using (var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid))
            {
                store.InsertTriple(t);

                store.Commit(Guid.Empty);
            }

            // match triple
            using (var store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid))
            {
                var matches = store.Match(t.Subject, t.Predicate, t.Object, true,
                                          RdfDatatypes.XmlLiteral, null, Constants.DefaultGraphUri)
                    .ToList();
                Assert.AreEqual(1, matches.Count());

                // check document is ok.
                var outDoc = XDocument.Parse(matches.First().Object);

                Assert.IsNotNull(outDoc);
                Assert.IsNotNull(outDoc.Root);
                Assert.AreEqual(8, outDoc.Root.Elements().Count());
            }
        }

        public virtual void TestMatchTriples()
        {
            var sid = Guid.NewGuid().ToString();
            var t1 = new Triple
            {
                Subject = "http://www.networkedplanet.com/people/10",
                Predicate = "http://www.networkedplanet.com/model/isa",
                Object = "bob",
                DataType = RdfDatatypes.String,
                IsLiteral = true
            };

            var t2 = new Triple
            {
                Subject = "http://www.networkedplanet.com/people/10",
                Predicate = "http://www.networkedplanet.com/model/isa",
                Object = "kal",
                DataType = RdfDatatypes.String,
                IsLiteral = true
            };

            using (var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid))
            {
                store.InsertTriple(t1);
                store.InsertTriple(t2);
                store.Commit(Guid.Empty);
            }
            // match triple
            using (var store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid))
            {
                var matches = store.Match(t2.Subject, t2.Predicate, t2.Object, true,
                                          RdfDatatypes.String, null, Constants.DefaultGraphUri);
                Assert.AreEqual(1, matches.Count());
            }
            using (var store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid))
            {
                var triples = store.GetResourceStatements("http://www.networkedplanet.com/people/10");
                Assert.AreEqual(2, triples.Count());
            }
        }

        public virtual void TestMatchTriplesWithNulls()
        {
            var sid = Guid.NewGuid().ToString();
            var t1 = new Triple
            {
                Subject = "http://www.networkedplanet.com/people/10",
                Predicate = "http://www.networkedplanet.com/model/isa",
                Object = "bob",
                DataType = RdfDatatypes.String,
                IsLiteral = true
            };
            var t2 = new Triple
            {
                Subject = "http://www.networkedplanet.com/people/10",
                Predicate = "http://www.networkedplanet.com/model/isa",
                Object = "kal",
                DataType = RdfDatatypes.String,
                IsLiteral = true
            };
            using (var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid))
            {
                store.InsertTriple(t1);
                store.InsertTriple(t2);

                store.Commit(Guid.Empty);
            }
            // match triple
            using (var store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid))
            {
                var matches = store.Match(t1.Subject, t1.Predicate, t1.Object, true, RdfDatatypes.String, null,
                                          Constants.DefaultGraphUri);
                Assert.AreEqual(1, matches.Count());
                matches = store.Match(t1.Subject, t1.Predicate, t1.Object, true, RdfDatatypes.String, null, graphs: null);
                Assert.AreEqual(1, matches.Count(), "Failed to match triple using null graphs array");
            }

            using (var store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid))
            {
                var triples = store.GetResourceStatements("http://www.networkedplanet.com/people/10");
                Assert.AreEqual(2, triples.Count());
            }
        }

        public virtual void TestSparql1()
        {
            var sid = Guid.NewGuid().ToString();
            using (var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid))
            {

                var t = new Triple
                            {
                                Subject = "http://www.networkedplanet.com/people/10",
                                Predicate = "http://www.networkedplanet.com/model/isa",
                                Object = "bob",
                                DataType = RdfDatatypes.String,
                                IsLiteral = true
                            };
                store.InsertTriple(t);
                t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/10",
                            Predicate = "http://www.networkedplanet.com/model/worksfor",
                            Object = "http://www.networkedplanet.com/np",
                        };
                store.InsertTriple(t);
                store.Commit(Guid.Empty);
            }
            // match triple
            using (var store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid))
            {
                const string query =
                    "select ?t where { ?t <http://www.networkedplanet.com/model/worksfor> <http://www.networkedplanet.com/np> }";
                var result = store.ExecuteSparqlQuery(query, SparqlResultsFormat.Xml);
                Assert.IsNotNull(result);

                var resultDoc = XDocument.Parse(result);
                var rows = resultDoc.Descendants(SparqlResult + "result").ToList();
                Assert.AreEqual(1, rows.Count());

                var uriBinding = rows.Descendants(SparqlResult + "uri").FirstOrDefault();
                Assert.IsNotNull(uriBinding);
                Assert.AreEqual("http://www.networkedplanet.com/people/10", uriBinding.Value);
            }
        }

#if SILVERLIGHT
        public virtual void TestReadConfiguration()
        {
            var storeLocation = Configuration.StoreLocation;
            Assert.AreEqual("brightstar", storeLocation);
        }
#else
        public virtual void TestReadConfiguration()
        {
            var storeLocation = Configuration.StoreLocation;
            Assert.AreEqual("c:\\brightstar", storeLocation);
        }
#endif

        public virtual void TestListCommitPoints()
        {
            var sid = Guid.NewGuid().ToString();
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);

            Assert.AreEqual(1, store.GetCommitPoints().Count());

            var t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/10",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "bob",
                            DataType = RdfDatatypes.String,
                            IsLiteral = true
                        };
            store.InsertTriple(t);
            store.Commit(Guid.Empty);

            Assert.AreEqual(2, store.GetCommitPoints().Count());

            t = new Triple
                    {
                        Subject = "http://www.networkedplanet.com/people/11",
                        Predicate = "http://www.networkedplanet.com/model/isa",
                        Object = "bob",
                        DataType = RdfDatatypes.String,
                        IsLiteral = true
                    };
            store.InsertTriple(t);
            store.Commit(Guid.Empty);

            Assert.AreEqual(3, store.GetCommitPoints().Count());
        }

        public virtual void TestRevertToCommitPoint()
        {
            // create 3 commit points
            var sid = Guid.NewGuid().ToString();
            using (var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid))
            {

                Assert.AreEqual(1, store.GetCommitPoints().Count());

                var t = new Triple
                            {
                                Subject = "http://www.networkedplanet.com/people/10",
                                Predicate = "http://www.networkedplanet.com/model/isa",
                                Object = "bob",
                                DataType = RdfDatatypes.String,
                                IsLiteral = true
                            };
                store.InsertTriple(t);
                store.Commit(Guid.Empty);

                Assert.AreEqual(2, store.GetCommitPoints().Count());

                t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/11",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "bob",
                            DataType = RdfDatatypes.String,
                            IsLiteral = true
                        };
                store.InsertTriple(t);
                store.Commit(Guid.Empty);

                var triples = store.Match(null, null, null, graph: Constants.DefaultGraphUri);
                Assert.AreEqual(2, triples.Count());

                Assert.AreEqual(3, store.GetCommitPoints().Count());

                // get all the commitpoints
                var commitPoints = store.GetCommitPoints().ToList();

                // the last returned commit point was the first to be written.
                var firstCommitPoint = commitPoints.Last();

                // now revert to the 1st commit point where there should be no data.
                store.RevertToCommitPoint(firstCommitPoint);
            }
            using (var store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid))
            {
                var triples = store.Match(null, null, null, graph: Constants.DefaultGraphUri);
                Assert.AreEqual(0, triples.Count());

                // there should be 4 commitpoints now.
                Assert.AreEqual(4, store.GetCommitPoints().Count());
            }
        }

        public virtual void TestGetCommitPoint()
        {
            var testTimestamps = new List<DateTime> { DateTime.UtcNow };
            Thread.Sleep(100);

            var sid = Guid.NewGuid().ToString();
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);
            Assert.AreEqual(1, store.GetCommitPoints().Count());

            testTimestamps.Add(DateTime.UtcNow);
            Thread.Sleep(100);

            var t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/10",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "bob",
                            DataType = RdfDatatypes.String,
                            IsLiteral = true
                        };
            store.InsertTriple(t);
            store.Commit(Guid.Empty);
            Assert.AreEqual(2, store.GetCommitPoints().Count());

            testTimestamps.Add(DateTime.UtcNow);
            Thread.Sleep(100);

            t = new Triple
                    {
                        Subject = "http://www.networkedplanet.com/people/11",
                        Predicate = "http://www.networkedplanet.com/model/isa",
                        Object = "bob",
                        DataType = RdfDatatypes.String,
                        IsLiteral = true
                    };
            store.InsertTriple(t);
            store.Commit(Guid.Empty);
            Assert.AreEqual(3, store.GetCommitPoints().Count());

            testTimestamps.Add(DateTime.UtcNow);
            Thread.Sleep(100);

            var allCommitPoints = store.GetCommitPoints().ToList();
            var client = BrightstarService.GetEmbeddedClient(Configuration.StoreLocation);
            var commitPoint = client.GetCommitPoint(sid, testTimestamps[0]);
            Assert.IsNull(commitPoint);

            commitPoint = client.GetCommitPoint(sid, testTimestamps[1]);
            Assert.IsNotNull(commitPoint);
            Assert.AreEqual(allCommitPoints[2].LocationOffset, commitPoint.Id);

            commitPoint = client.GetCommitPoint(sid, testTimestamps[2]);
            Assert.IsNotNull(commitPoint);
            Assert.AreEqual(allCommitPoints[1].LocationOffset, commitPoint.Id);

            commitPoint = client.GetCommitPoint(sid, testTimestamps[3]);
            Assert.IsNotNull(commitPoint);
            Assert.AreEqual(allCommitPoints[0].LocationOffset, commitPoint.Id);
            Assert.AreEqual(allCommitPoints[0].JobId, commitPoint.JobId);
            Assert.AreEqual(allCommitPoints[0].CommitTime, commitPoint.CommitTime);
            Assert.AreEqual(sid, commitPoint.StoreName);
        }

        public virtual void TestQueryAtCommitPoint()
        {
            var testTimestamps = new List<DateTime> { DateTime.UtcNow };
            Thread.Sleep(100);

            var sid = Guid.NewGuid().ToString();
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);
            Assert.AreEqual(1, store.GetCommitPoints().Count());

            testTimestamps.Add(DateTime.UtcNow);
            Thread.Sleep(100);

            var t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/10",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "bob",
                            DataType = RdfDatatypes.String,
                            IsLiteral = true
                        };
            store.InsertTriple(t);
            store.Commit(Guid.Empty);
            Assert.AreEqual(2, store.GetCommitPoints().Count());

            testTimestamps.Add(DateTime.UtcNow);
            Thread.Sleep(100);

            t = new Triple
                    {
                        Subject = "http://www.networkedplanet.com/people/11",
                        Predicate = "http://www.networkedplanet.com/model/isa",
                        Object = "bob",
                        DataType = RdfDatatypes.String,
                        IsLiteral = true
                    };
            store.InsertTriple(t);
            store.Commit(Guid.Empty);
            Assert.AreEqual(3, store.GetCommitPoints().Count());

            testTimestamps.Add(DateTime.UtcNow);
            Thread.Sleep(100);

            var client = BrightstarService.GetEmbeddedClient(Configuration.StoreLocation);
            const string queryString = "SELECT ?p ?x WHERE {?p <http://www.networkedplanet.com/model/isa> ?x . }";

            var commitPoint = client.GetCommitPoint(sid, testTimestamps[1]);
            using (var results = client.ExecuteQuery(commitPoint, queryString))
            {
                var resultsDoc = XDocument.Load(results);
                Assert.AreEqual(0, resultsDoc.SparqlResultRows().Count());
            }

            commitPoint = client.GetCommitPoint(sid, testTimestamps[2]);
            Assert.IsNotNull(commitPoint);
            using (var results = client.ExecuteQuery(commitPoint, queryString))
            {
                var resultsDoc = XDocument.Load(results);
                Assert.AreEqual(1, resultsDoc.SparqlResultRows().Count());
            }

            commitPoint = client.GetCommitPoint(sid, testTimestamps[3]);
            Assert.IsNotNull(commitPoint);
            using (var results = client.ExecuteQuery(commitPoint, queryString))
            {
                var resultsDoc = XDocument.Load(results);
                Assert.AreEqual(2, resultsDoc.SparqlResultRows().Count());
            }
        }

        public virtual void TestListStoreGraphs()
        {
            // create 3 commit points
            var sid = Guid.NewGuid().ToString();
            using (var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid))
            {

                // Initially should be no graph URIs in the index
                Assert.IsFalse(store.GetGraphUris().Any());

                var t = new Triple
                            {
                                Subject = "http://www.networkedplanet.com/people/12",
                                Predicate = "http://www.networkedplanet.com/model/name",
                                Object = "bob",
                                IsLiteral = true,
                                DataType = RdfDatatypes.String
                            };
                store.InsertTriple(t);
                store.Commit(Guid.Empty);

                List<string> allGraphUris = store.GetGraphUris().ToList();
                Assert.AreEqual(1, allGraphUris.Count);
                Assert.IsTrue(allGraphUris.Contains(Constants.DefaultGraphUri));

                t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/12",
                            Predicate = "http://www.networkedplanet.com/model/name",
                            Object = "bob",
                            IsLiteral = true,
                            DataType = RdfDatatypes.String,
                            Graph = "http://www.networkedplanet.com/graphs/1"
                        };
                store.InsertTriple(t);
                store.Commit(Guid.Empty);

                allGraphUris = store.GetGraphUris().ToList();
                Assert.AreEqual(2, allGraphUris.Count);
                Assert.IsTrue(allGraphUris.Contains(Constants.DefaultGraphUri));
                Assert.IsTrue(allGraphUris.Contains("http://www.networkedplanet.com/graphs/1"));
            }
            using (var reopenStore = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid))
            {
                var allGraphUris = reopenStore.GetGraphUris().ToList();
                Assert.AreEqual(2, allGraphUris.Count);
                Assert.IsTrue(allGraphUris.Contains(Constants.DefaultGraphUri));
                Assert.IsTrue(allGraphUris.Contains("http://www.networkedplanet.com/graphs/1"));
            }
        }

        public virtual void TestListStores()
        {
            var stores = StoreManager.ListStores(Configuration.StoreLocation);
            Assert.IsFalse(stores.Contains("import"));
        }

        public virtual void TestRecoverFromBadCommitPointWrite()
        {
            var sid = Guid.NewGuid().ToString();
            using (var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid))
            {

                var t = new Triple
                            {
                                Subject = "http://www.networkedplanet.com/people/10",
                                Predicate = "http://www.networkedplanet.com/model/isa",
                                Object = "bob",
                                DataType = RdfDatatypes.String,
                                IsLiteral = true
                            };
                store.InsertTriple(t);

                t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/10",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "kal",
                            DataType = RdfDatatypes.String,
                            IsLiteral = true
                        };
                store.InsertTriple(t);

                store.Commit(Guid.Empty);

                t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/10",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "gra",
                            DataType = RdfDatatypes.String,
                            IsLiteral = true
                        };

                store.InsertTriple(t);
                store.Commit(Guid.Empty);
            }
            var storePath = Path.Combine(Configuration.StoreLocation, sid);
            var masterFilePath = Path.Combine(Configuration.StoreLocation, sid, MasterFile.MasterFileName);

            using (var store = StoreManager.OpenStore(storePath))
            {
                Assert.AreEqual(3, store.GetResourceStatements("http://www.networkedplanet.com/people/10").Count());
            }

            // mess with the file
            using (var stream = new FileStream(masterFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                stream.Seek(-130, SeekOrigin.End);
                stream.WriteByte(5);
                stream.Flush();
            }

            // open it and should still be at the third commit point (using second copy)
            using (var store = StoreManager.OpenStore(storePath))
            {
                var triples = store.GetResourceStatements("http://www.networkedplanet.com/people/10");
                Assert.AreEqual(3, triples.Count());

                // Mess with the file again (fuck up the duplicate copy)
                using (var stream = new FileStream(masterFilePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    stream.Seek(-5, SeekOrigin.End);
                    var currentValue = (byte)stream.ReadByte();
                    stream.Seek(-1, SeekOrigin.Current);
                    stream.WriteByte((byte)~currentValue);
                    stream.Flush();
                }
            }

            // Open it now and we will be back to the second commit point
            using (var store = StoreManager.OpenStore(storePath))
            {
                var triples = store.GetResourceStatements("http://www.networkedplanet.com/people/10");
                Assert.AreEqual(2, triples.Count());
            }
        }

        public virtual void TestRecoverFromBadCommitPointWrite2()
        {
            var sid = Guid.NewGuid().ToString();
            using (var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid))
            {

                var t = new Triple
                            {
                                Subject = "http://www.networkedplanet.com/people/10",
                                Predicate = "http://www.networkedplanet.com/model/isa",
                                Object = "bob",
                                DataType = RdfDatatypes.String,
                                IsLiteral = true
                            };
                store.InsertTriple(t);

                t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/10",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "kal",
                            DataType = RdfDatatypes.String,
                            IsLiteral = true
                        };
                store.InsertTriple(t);

                store.Commit(Guid.Empty);

                t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/10",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "gra",
                            DataType = RdfDatatypes.String,
                            IsLiteral = true
                        };

                store.InsertTriple(t);
                store.Commit(Guid.Empty);
            }
            // mess with the file
            var masterFilePath = Path.Combine(Configuration.StoreLocation, sid, MasterFile.MasterFileName);
            using (var stream = new FileStream(masterFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                stream.SetLength(stream.Length - 1);
            }

            // open it and should be at the second commit point
            using (var store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid))
            {
                var triples = store.GetResourceStatements("http://www.networkedplanet.com/people/10");
                Assert.AreEqual(2, triples.Count());
            }
        }

        public virtual void TestWriteAllowedAfterRecoverFromBadCommitPointWrite()
        {
            var sid = Guid.NewGuid().ToString();
            using (var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid))
            {

                var t = new Triple
                            {
                                Subject = "http://www.networkedplanet.com/people/10",
                                Predicate = "http://www.networkedplanet.com/model/isa",
                                Object = "bob",
                                DataType = RdfDatatypes.String,
                                IsLiteral = true
                            };
                store.InsertTriple(t);

                t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/10",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "kal",
                            DataType = RdfDatatypes.String,
                            IsLiteral = true
                        };
                store.InsertTriple(t);

                store.Commit(Guid.Empty);

                t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/10",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "gra",
                            DataType = RdfDatatypes.String,
                            IsLiteral = true
                        };

                store.InsertTriple(t);
                store.Commit(Guid.Empty);
            }

            // mess with the file
            var masterFilePath = Path.Combine(Configuration.StoreLocation, sid, MasterFile.MasterFileName);
            using (var stream = new FileStream(masterFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                stream.SetLength(stream.Length - 1);
            }

            // open it and should be at the second commit point
            using (var store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid))
            {
                var triples = store.GetResourceStatements("http://www.networkedplanet.com/people/10");
                Assert.AreEqual(2, triples.Count());

                var t = new Triple
                        {
                            Subject = "http://www.networkedplanet.com/people/10",
                            Predicate = "http://www.networkedplanet.com/model/isa",
                            Object = "gra",
                            DataType = RdfDatatypes.String,
                            IsLiteral = true
                        };
                store.InsertTriple(t);

                store.Commit(Guid.Empty);
            }

            using (var store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid))
            {
                var triples = store.GetResourceStatements("http://www.networkedplanet.com/people/10");
                Assert.AreEqual(3, triples.Count());
            }
        }

        public virtual void TestBadXmlInSparqlResult()
        {
            var sid = Guid.NewGuid().ToString();
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);
            var t = new Triple
                        {
                            Subject = "http://example.org/resource",
                            Predicate = "http://example.org/description",
                            Object = "This is <i>BAD</I> XML and it will cause us problems",
                            DataType = RdfDatatypes.String,
                            IsLiteral = true
                        };
            store.InsertTriple(t);
            store.Commit(Guid.Empty);

            var store1 = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid, true);
            var results = store1.ExecuteSparqlQuery("SELECT ?s ?p ?o WHERE { ?s ?p ?o }", SparqlResultsFormat.Xml);
            XDocument resultsDoc = XDocument.Parse(results);
            Assert.IsNotNull(resultsDoc);
            Assert.IsNotNull(resultsDoc.Root);
            Assert.AreEqual(1, resultsDoc.SparqlResultRows().Count());
        }

        public virtual void TestConsolidateStore()
        {
            var sid = "TestConsolidateStore_" + DateTime.Now.Ticks;
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);
            Guid job1Id = Guid.NewGuid(),
                 job2Id = Guid.NewGuid(),
                 job3Id = Guid.NewGuid(),
                 consolidateJobId = Guid.NewGuid();
            store.InsertTriple(new Triple
                                   {
                                       Subject = "http://example.org/alice",
                                       Predicate = "http://example.org/name",
                                       Object = "Alice",
                                       DataType = RdfDatatypes.String,
                                       IsLiteral = true
                                   });
            store.Commit(job1Id);

            store.InsertTriple(new Triple
                                   {
                                       Subject = "http://example.org/bob",
                                       Predicate = "http://example.org/name",
                                       Object = "Bob",
                                       DataType = RdfDatatypes.String,
                                       IsLiteral = true
                                   });
            store.Commit(job2Id);

            store.InsertTriple(new Triple
                                   {
                                       Subject = "http://example.org/Charlie",
                                       Predicate = "http://example.org/name",
                                       Object = "Charlie",
                                       DataType = RdfDatatypes.String,
                                       IsLiteral = true
                                   });
            store.Commit(job3Id);

            // Before consolidate there should be 4 commit points
            var commitPoints = store.GetCommitPoints().ToList();
            Assert.AreEqual(4, commitPoints.Count);
            Assert.AreEqual(job3Id, commitPoints[0].JobId);
            Assert.AreEqual(job2Id, commitPoints[1].JobId);
            Assert.AreEqual(job1Id, commitPoints[2].JobId);
            Assert.AreEqual(Guid.Empty, commitPoints[3].JobId);

            store.Consolidate(consolidateJobId);

            commitPoints = store.GetCommitPoints().ToList();
            Assert.AreEqual(1, commitPoints.Count);
            Assert.AreEqual(consolidateJobId, commitPoints[0].JobId);

            store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid);
            var results = store.ExecuteSparqlQuery("SELECT ?l WHERE { ?s <http://example.org/name> ?l }", SparqlResultsFormat.Xml);
            var resultsDoc = XDocument.Parse(results);
            var labels = resultsDoc.SparqlResultRows().Select(x => x.GetColumnValue("l")).ToList();
            Assert.AreEqual(3, labels.Count);
            Assert.IsTrue(labels.Contains("Alice"));
            Assert.IsTrue(labels.Contains("Bob"));
            Assert.IsTrue(labels.Contains("Charlie"));
        }

        public virtual void TestConsolidateEmptyStore()
        {
            var sid = "TestConsolidateStore_" + DateTime.Now.Ticks;
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);
            var consolidateJobId = Guid.NewGuid();
            store.Consolidate(consolidateJobId);
            store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid);
            var commitPoints = store.GetCommitPoints().ToList();
            Assert.AreEqual(1, commitPoints.Count);
            Assert.AreEqual(consolidateJobId, commitPoints[0].JobId);
        }

        public virtual void TestBatchedInserts()
        {
            var sid = "TestBatchedInserts_" + DateTime.Now.Ticks;
            var store = StoreManager.CreateStore(Configuration.StoreLocation + "\\" + sid);

            string result;
            XDocument resultDoc;
            XElement row;
            for (int i = 1; i <= 10000; i++)
            {
                Guid subjectId = Guid.NewGuid();
                store.InsertTriple(
                    String.Format("http://www.brightstardb.com/.well-known/genid/{0}", subjectId),
                    "http://www.w3.org/1999/02/22-rdf-syntax-ns#type",
                    "http://www.example.org/schema/Entity",
                    false, null, null, Constants.DefaultGraphUri);
                store.InsertTriple(
                    String.Format("http://www.brightstardb.com/.well-known/genid/{0}", subjectId),
                    "http://www.example.org/schema/someString",
                    String.Format("Entity {0}", i),
                    true,
                    "http://www.w3.org/2001/XMLSchema#string",
                    null,
                    Constants.DefaultGraphUri);
                if (i % 2000 == 0)
                {
                    store.Commit(Guid.NewGuid());
                    var tripleCount = store.Match(null, null, null, false, null, null, Constants.DefaultGraphUri).Count();
                    Assert.AreEqual(i * 2, tripleCount, "Unexpected triple count after import batch to {0}", i);
                    result = store.ExecuteSparqlQuery("SELECT COUNT(?x) WHERE { ?x a <http://www.example.org/schema/Entity>}", SparqlResultsFormat.Xml);
                    resultDoc = XDocument.Parse(result);
                    Assert.AreEqual(1, resultDoc.SparqlResultRows().Count());
                    row = resultDoc.SparqlResultRows().First();
                    Assert.AreEqual(i, row.GetColumnValue(0), "Unexpected results count after import batch.");
                    store.Close();
                    store = StoreManager.OpenStore(Configuration.StoreLocation + "\\" + sid);
                }
            }
            store.Commit(Guid.NewGuid());

            result = store.ExecuteSparqlQuery("SELECT COUNT(?x) WHERE { ?x a <http://www.example.org/schema/Entity>}", SparqlResultsFormat.Xml);
            resultDoc = XDocument.Parse(result);
            Assert.AreEqual(1, resultDoc.SparqlResultRows().Count());
            row = resultDoc.SparqlResultRows().First();
            Assert.AreEqual(10000, row.GetColumnValue(0), "Unexpected results count after final import");
        }

        public virtual void TestMultiThreadedReadAccess()
        {
            var sid = "TestMultiThreadedReadAccess_" + DateTime.Now.Ticks;
            using (var store = StoreManager.CreateStore(sid))
            {
                for (int i = 1; i <= 10000; i++)
                {
                    Guid subjectId = Guid.NewGuid();
                    store.InsertTriple(
                        String.Format("http://www.brightstardb.com/.well-known/genid/{0}", subjectId),
                        "http://www.w3.org/1999/02/22-rdf-syntax-ns#type",
                        "http://www.example.org/schema/Entity",
                        false, null, null, Constants.DefaultGraphUri);
                    store.InsertTriple(
                        String.Format("http://www.brightstardb.com/.well-known/genid/{0}", subjectId),
                        "http://www.example.org/schema/someString",
                        String.Format("Entity {0}", i),
                        true,
                        "http://www.w3.org/2001/XMLSchema#string",
                        null,
                        Constants.DefaultGraphUri);
                }
                store.Commit(Guid.NewGuid());
            }

            
            var tf = new TaskFactory();
            using (var readStore = StoreManager.OpenStore(sid, true))
            {
                var task1 = tf.StartNew<int>(EnumerateStore, readStore);
                var task2 = tf.StartNew<int>(EnumerateStore, readStore);
                var task3 = tf.StartNew<int>(EnumerateStore, readStore);
                var task4 = tf.StartNew<int>(EnumerateStore, readStore);
                try
                {
                    Task.WaitAll(new Task[] {task1, task2, task3, task4});
                } catch(Exception ex)
                {
                    Assert.Fail("Unexpected exception: {0}", ex);
                }
                Assert.AreEqual(20000, task1.Result);
                Assert.AreEqual(20000, task2.Result);
                Assert.AreEqual(20000, task3.Result);
                Assert.AreEqual(20000, task4.Result);
            }
        }

        private static int EnumerateStore(object state)
        {
            var store = state as IStore;
            Assert.IsNotNull(store);
            var result = store.Match(null, null, null, false, null, null, Constants.DefaultGraphUri).ToList();
            return result.Count;
        }
    }
}