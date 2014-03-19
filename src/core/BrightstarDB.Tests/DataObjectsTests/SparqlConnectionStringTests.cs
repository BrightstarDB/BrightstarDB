using BrightstarDB.Client;
using NUnit.Framework;
using VDS.RDF.Query;

namespace BrightstarDB.Tests.DataObjectsTests
{
    [TestFixture]
    public class SparqlConnectionStringTests
    {
        [Test]
        public void TestReadOnlyConnection()
        {
            var doContext = BrightstarService.GetDataObjectContext("type=sparql;query=http://dbpedia.org/sparql");
            Assert.IsNotNull(doContext);
            var sparqlDoContext = doContext as SparqlDataObjectContext;
            Assert.That(sparqlDoContext, Is.Not.Null);
            Assert.That(sparqlDoContext.UpdateProcessor, Is.Null);
            Assert.That(sparqlDoContext.QueryProcessor, Is.Not.Null);
            Assert.That(sparqlDoContext.OptimisticLockingEnabled, Is.False);

            // Check that the default store name has been applied
            Assert.That(doContext.DoesStoreExist("sparql"));
            var store = doContext.OpenStore("sparql");
            Assert.That(store, Is.Not.Null);
            Assert.That(store.IsReadOnly, Is.True);
        }

#if !PORTABLE && !WINDOWS_PHONE // SPARQL Update not supported on these platforms yet
        [Test]
        public void TestReadWriteConnection()
        {
            var doContext =
                BrightstarService.GetDataObjectContext(
                    "type=sparql;query=http://example.org/sparql;update=http://example.org/update");
            Assert.IsNotNull(doContext);
            Assert.That(doContext.DoesStoreExist("sparql"));
            var store = doContext.OpenStore("sparql");
            Assert.That(store, Is.Not.Null);
            Assert.That(store.IsReadOnly, Is.False);
        }
#endif
        
    }
}