using System;
using System.Linq;
using System.Text;
using BrightstarDB.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestClass]
    public class EFClientTests
    {
        private static IBrightstarService GetClient()
        {
            return BrightstarService.GetClient("type=http;endpoint=http://localhost:8090/brightstar");
        }


        [TestMethod]
        public void TestEmbeddedClientMapToRdf()
        {
            var storeName = "foaf_" + Guid.NewGuid().ToString();
            var embeddedClient =
                BrightstarService.GetClient("type=embedded;storesDirectory=c:\\brightstar;");
            embeddedClient.CreateStore(storeName);

            //add rdf data for a person
            var triples = new StringBuilder();
            triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://xmlns.com/foaf/0.1/Person> .");
            triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://xmlns.com/foaf/0.1/nick> ""Jen"" .");
            triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://xmlns.com/foaf/0.1/name> ""Jen Williams"" .");
            triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://xmlns.com/foaf/0.1/Organization> ""Networked Planet"" .");

            embeddedClient.ExecuteTransaction(storeName,null, null, triples.ToString(), true);

            //check EF can access all properties
            var context = new MyEntityContext(string.Format(@"type=embedded;storesDirectory=c:\\brightstar;storeName={0}", storeName));

            Assert.IsNotNull(context.FoafPersons);
            Assert.AreEqual(1, context.FoafPersons.Count());
            var person = context.FoafPersons.FirstOrDefault();
            Assert.IsNotNull(person);

            Assert.IsNotNull(person.Id);
            Assert.AreEqual("j.williams", person.Id);
            Assert.IsNotNull(person.Name);
            Assert.AreEqual("Jen Williams", person.Name);
            Assert.IsNotNull(person.Nickname);
            Assert.AreEqual("Jen", person.Nickname);
            Assert.IsNotNull(person.Organisation);
            Assert.AreEqual("Networked Planet", person.Organisation);
        }

        [TestMethod]
        public void TestMapToRdfDataTypeDate()
        {
            var storeName = "foaf_" + Guid.NewGuid().ToString();
            var embeddedClient =
                BrightstarService.GetClient("type=embedded;storesDirectory=c:\\brightstar;");
            embeddedClient.CreateStore(storeName);

            //add rdf data for a person
            var triples = new StringBuilder();
            triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://xmlns.com/foaf/0.1/Person> .");
            triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://xmlns.com/foaf/0.1/name> ""Jen Williams"" .");
            triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://dbpedia.org/ontology/birthDate> ""1921-11-28""^^<http://www.w3.org/2001/XMLSchema#date> .");
            

            embeddedClient.ExecuteTransaction(storeName, null, null, triples.ToString(), true);

            //check EF can access all properties
            var context = new MyEntityContext(string.Format(@"type=embedded;storesDirectory=c:\\brightstar;storeName={0}", storeName));

            Assert.IsNotNull(context.FoafPersons);
            Assert.AreEqual(1, context.FoafPersons.Count());
            var person = context.FoafPersons.FirstOrDefault();
            Assert.IsNotNull(person);

            Assert.IsNotNull(person.Id);
            Assert.AreEqual("j.williams", person.Id);
            Assert.IsNotNull(person.Name);
            Assert.AreEqual("Jen Williams", person.Name);
            Assert.IsNotNull(person.BirthDate);
        }

        //[TestMethod]
        //public void TestMapToRdfDataTypeTime()
        //{
        //    var storeName = "foaf_" + Guid.NewGuid().ToString();
        //    var embeddedClient =
        //        BrightstarService.GetClient("type=embedded;storesDirectory=c:\\brightstar;");
        //    embeddedClient.CreateStore(storeName);

        //    //add rdf data for a person
        //    var triples = new StringBuilder();
        //    triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://xmlns.com/foaf/0.1/Person> .");
        //    triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://xmlns.com/foaf/0.1/name> ""Jen Williams"" .");
        //    triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://example.org/ontology/startTime> ""08:00:00""^^<http://www.w3.org/2001/XMLSchema#time> .");


        //    embeddedClient.ExecuteTransaction(storeName, null, null, triples.ToString(), true);

        //    //check EF can access all properties
        //    var context = new MyEntityContext(string.Format(@"type=embedded;storesDirectory=c:\\brightstar;storeName={0}", storeName));

        //    Assert.IsNotNull(context.FoafPersons);
        //    Assert.AreEqual(1, context.FoafPersons.Count());
        //    var person = context.FoafPersons.FirstOrDefault();
        //    Assert.IsNotNull(person);

        //    Assert.IsNotNull(person.Id);
        //    Assert.AreEqual("j.williams", person.Id);
        //    Assert.IsNotNull(person.Name);
        //    Assert.AreEqual("Jen Williams", person.Name);
        //    Assert.IsNotNull(person.BirthDate);
        //}

        [TestMethod]
        public void TestHttpClientMapToRdf()
        {
            var storeName = "foaf_" + Guid.NewGuid().ToString();
            var httpClient = GetClient();
            httpClient.CreateStore(storeName);

            //add rdf data for a person
            var triples = new StringBuilder();
            triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://xmlns.com/foaf/0.1/Person> .");
            triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://xmlns.com/foaf/0.1/nick> ""Jen"" .");
            triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://xmlns.com/foaf/0.1/name> ""Jen Williams"" .");
            triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://xmlns.com/foaf/0.1/Organization> ""Networked Planet"" .");

            httpClient.ExecuteTransaction(storeName,null, null, triples.ToString(), true);

            //check EF can access all properties
            var context = new MyEntityContext(string.Format(@"type=http;endpoint=http://localhost:8090/brightstar;storeName={0}", storeName));

            Assert.IsNotNull(context.FoafPersons);
            Assert.AreEqual(1, context.FoafPersons.Count());
            var person = context.FoafPersons.FirstOrDefault();
            Assert.IsNotNull(person);

            Assert.IsNotNull(person.Id);
            Assert.AreEqual("j.williams", person.Id);
            Assert.IsNotNull(person.Name);
            Assert.AreEqual("Jen Williams", person.Name);
            Assert.IsNotNull(person.Nickname);
            Assert.AreEqual("Jen", person.Nickname);
            Assert.IsNotNull(person.Organisation);
            Assert.AreEqual("Networked Planet", person.Organisation);
        }
    }
}
