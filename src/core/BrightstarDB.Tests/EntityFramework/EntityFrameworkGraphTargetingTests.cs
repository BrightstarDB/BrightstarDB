using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using BrightstarDB.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestClass]
    public class EntityFrameworkGraphTargetingTests : ClientTestBase
    {
        private const string TestStoreLocation = "c:\\brightstar";
        private readonly string _storeName = "EntityFrameworkGraphTargetingTests_" + DateTime.Now.Ticks;

        private MyEntityContext NewContext(bool optimisticLocking, string updateGraph = null, IEnumerable<string> datasetGraphs = null, string versioningGraph = null )
        {
            var connectionString =
                String.Format("type=embedded;storesDirectory={0};storeName={1}",
                              TestStoreLocation, _storeName);
            return new MyEntityContext(connectionString, optimisticLocking, updateGraph, datasetGraphs, versioningGraph);
        }

        private IBrightstarService NewRdfClient()
        {
            var connectionString =
                String.Format("type=embedded;storesDirectory={0};storeName={1}",
                              TestStoreLocation, _storeName);
            return BrightstarService.GetClient(connectionString);
        }

        [TestMethod]
        public void TestCreateInNamedGraph()
        {
            var update = "http://example.org/graphs/update";
            var context = NewContext(false, update);
            var alice = context.FoafPersons.Create();
            alice.Name = "Alice";
            context.SaveChanges();

            // Triples should be in the update graph
            var client = NewRdfClient();
            var results = client.ExecuteQuery(_storeName,
                                "SELECT ?p ?o ?g FROM NAMED <" + update + "> FROM NAMED <" +
                                Constants.DefaultGraphUri + "> WHERE { GRAPH ?g { <http://www.networkedplanet.com/people/" + alice.Id + "> ?p ?o }}");
            var resultsDoc = XDocument.Load(results);
            Assert.IsTrue(resultsDoc.SparqlResultRows().All(r=>r.GetColumnValue("g").ToString().Equals(update)));
        }

        [TestMethod]
        public void TestAddAndDeletePropertyInSeparateGraph()
        {
            const string inferred = "http://example.org/graphs/inferred";
            var context = NewContext(false);
            var woodyAllen = context.DBPediaPersons.Create();
            woodyAllen.GivenName = "Woody";
            woodyAllen.Surname = "Allen";
            context.SaveChanges();

            context = NewContext(false, inferred);
            var woodyAllen2 = context.DBPediaPersons.FirstOrDefault(p => p.Id.Equals(woodyAllen.Id));
            Assert.IsNotNull(woodyAllen2);
            Assert.AreEqual("Woody", woodyAllen2.GivenName);
            Assert.AreEqual("Allen", woodyAllen2.Surname);
            Assert.IsNull(woodyAllen2.Name);
            woodyAllen2.Name = woodyAllen2.GivenName + " " + woodyAllen2.Surname;
            context.SaveChanges();
            
            // Name triple should be in the inferred graph
            var client = NewRdfClient();
            var results = client.ExecuteQuery(_storeName,
                                "SELECT ?p ?o ?g FROM NAMED <" + inferred + ">" +
                                " WHERE { GRAPH ?g { <http://dbpedia.org/resource/" + woodyAllen2.Id + "> ?p ?o }}");
            var resultsDoc = XDocument.Load(results);
            var rows = resultsDoc.SparqlResultRows().ToList();
            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual(inferred, rows[0].GetColumnValue("g").ToString());
            Assert.AreEqual("http://xmlns.com/foaf/0.1/name", rows[0].GetColumnValue("p").ToString());
            Assert.AreEqual("Woody Allen", rows[0].GetColumnValue("o").ToString());

            // Remove property should delete from the graph where the property is stored
            woodyAllen2.Name = null;
            woodyAllen2.Surname = null;
            context.SaveChanges();

            // Inferred graph should now be empy
            results = client.ExecuteQuery(_storeName,
                                "SELECT ?p ?o ?g FROM NAMED <" + inferred + ">" +
                                " WHERE { GRAPH ?g { <http://dbpedia.org/resource/" + woodyAllen2.Id + "> ?p ?o }}");
            resultsDoc = XDocument.Load(results);
            rows = resultsDoc.SparqlResultRows().ToList();
            Assert.AreEqual(0, rows.Count);

            context = NewContext(false);
            var woodyAllen4 = context.DBPediaPersons.FirstOrDefault(p => p.Id.Equals(woodyAllen.Id));
            Assert.IsNotNull(woodyAllen4);
            Assert.AreEqual("Woody", woodyAllen4.GivenName);
            Assert.IsNull(woodyAllen4.Surname);
            Assert.IsNull(woodyAllen4.Name);

        }
    }
}
