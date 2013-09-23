using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using BrightstarDB.Client;
using BrightstarDB.EntityFramework;
using BrightstarDB.Rdf;
using NUnit.Framework;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestFixture]
    public class PlainLiteralTests
    {
        private IDataObjectContext _dataObjectContext;

        [TestFixtureSetUp]
        public void SetUp()
        {
            var connectionString = new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation);
            _dataObjectContext = new EmbeddedDataObjectContext(connectionString);
        }

        [Test]
        public void TestCreatePlainLiteral()
        {
            var storeName = "PlainLiteralTests_CreatePlainLiteral" + DateTime.Now.Ticks;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                string conceptAId;
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var conceptA = context.Concepts.Create();
                    conceptA.PrefLabel = new []
                        {
                            new PlainLiteral("Default value"),
                            new PlainLiteral("English value", "en"), 
                            new PlainLiteral("US English value", "en-US"), 
                        };
                    context.SaveChanges();
                    conceptAId = conceptA.Id;
                }
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var conceptA = context.Concepts.FirstOrDefault(c => c.Id.Equals(conceptAId));
                    Assert.IsNotNull(conceptA);
                    Assert.That(conceptA.PrefLabel.Count, Is.EqualTo(3));
                    Assert.That(conceptA.PrefLabel.Any(l=>l.Value.Equals("Default value") && l.Language.Equals(String.Empty)));
                    Assert.That(conceptA.PrefLabel.Any(l=>l.Value.Equals("English value") && l.Language.Equals("en")));
                    Assert.That(conceptA.PrefLabel.Any(l=>l.Value.Equals("US English value") && l.Language.Equals("en-us")));
                }
            }
        }

        [Test]
        public void TestChangeLiteralLanguage()
        {
            var storeName = "PlainLiteralTests_ChangeLiteralLanguage" + DateTime.Now.Ticks;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                string conceptBId;
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var conceptB = context.Concepts.Create();
                    conceptB.PrefLabel = new[]
                        {
                            new PlainLiteral("US English value", "en")
                        };
                    context.SaveChanges();
                    conceptBId = conceptB.Id;
                }
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var conceptB = context.Concepts.FirstOrDefault(c => c.Id.Equals(conceptBId));
                    Assert.IsNotNull(conceptB);
                    var toReplace = conceptB.PrefLabel.FirstOrDefault(l => l.Language.Equals("en"));
                    Assert.IsNotNull(toReplace);
                    conceptB.PrefLabel.Remove(toReplace);
                    conceptB.PrefLabel.Add(new PlainLiteral(toReplace.Value, "en-us"));
                    context.SaveChanges();
                }

                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var conceptB = context.Concepts.FirstOrDefault(c => c.Id.Equals(conceptBId));
                    Assert.IsNotNull(conceptB);
                    Assert.IsNull(conceptB.PrefLabel.FirstOrDefault(l=>l.Language.Equals("en")));
                    var label = conceptB.PrefLabel.FirstOrDefault(l => l.Language.Equals("en-us"));
                    Assert.IsNotNull(label);
                    Assert.AreEqual("US English value", label.Value);
                }
            }
        }

        [Test]
        public void TestFindConceptByPlainLiteral()
        {
            var storeName = "PlainLiterals_FindConceptByPlainLiteral_" + DateTime.Now.Ticks;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                string conceptAId, conceptBId;
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var conceptA = context.Concepts.Create();
                    conceptA.PrefLabel.Add(new PlainLiteral("Topic Maps", "en"));
                    conceptA.PrefLabel.Add(new PlainLiteral("Cartes topiques", "fr"));
                    var conceptB = context.Concepts.Create();
                    conceptB.PrefLabel.Add(new PlainLiteral("Semantic Web", "en"));
                    conceptB.PrefLabel.Add(new PlainLiteral("Web sémantique", "fr"));
                    context.SaveChanges();
                    conceptAId = conceptA.Id;
                    conceptBId = conceptB.Id;
                }

                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var topicMaps =
                        context.Concepts.Where(
                            c => c.PrefLabel.Any(pref => pref.Equals(new PlainLiteral("Topic Maps", "en")))).ToList();
                    Assert.IsNotNull(topicMaps);
                    Assert.AreEqual(1, topicMaps.Count);
                    Assert.AreEqual(conceptAId, topicMaps[0].Id);
                }

                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var results =
                        context.Concepts.Where(c => c.PrefLabel.Any(pref => pref.Value.Contains("Web"))).ToList();
                    Assert.IsNotNull(results);
                    Assert.AreEqual(1, results.Count);
                    Assert.AreEqual(conceptBId, results[0].Id);
                }
            }
        }

        [Test]
        public void TestRetrieveLiterals()
        {
            var storeName = "PlainLiterals_RetrieveLiterals_" + DateTime.Now.Ticks;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var conceptA = context.Concepts.Create();
                    conceptA.PrefLabel.Add(new PlainLiteral("Topic Maps", "en"));
                    conceptA.PrefLabel.Add(new PlainLiteral("Cartes topiques", "fr"));
                    var conceptB = context.Concepts.Create();
                    conceptB.PrefLabel.Add(new PlainLiteral("Semantic Web", "en"));
                    conceptB.PrefLabel.Add(new PlainLiteral("Web sémantique", "fr"));
                    var conceptC = context.Concepts.Create();
                    conceptC.PrefLabel.Add(new PlainLiteral("RDF", "en"));
                    context.SaveChanges();

                }
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var results =
                        context.Concepts.SelectMany(c => c.PrefLabel.Where(p => p.Language.Equals("fr"))).ToList();
                    Assert.AreEqual(2, results.Count());
                    foreach(var r in results) Console.WriteLine("{0}@{1}", r.Value, r.Language);
                    Assert.IsTrue(results.All(r=>r.Language.Equals("fr")));
                    Assert.IsTrue(results.Any(r=>r.Value.Equals("Cartes topiques")));
                    Assert.IsTrue(results.Any(r => r.Value.Equals("Web sémantique")));
                }
            }
        }

        
    }
}
