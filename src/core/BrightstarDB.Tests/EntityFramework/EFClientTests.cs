using System;
using System.Linq;
using System.Text;
using BrightstarDB.Client;
using NUnit.Framework;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestFixture]
    public class EFClientTests
    {
        private static IBrightstarService GetClient()
        {
            return BrightstarService.GetClient("type=http;endpoint=http://localhost:8090/brightstar");
        }


        [Test]
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

            var job = embeddedClient.ExecuteTransaction(storeName,null, null, triples.ToString());
            TestHelper.AssertJobCompletesSuccessfully(embeddedClient, storeName, job);

            //check EF can access all properties
            using (
                var context =
                    new MyEntityContext(string.Format(@"type=embedded;storesDirectory=c:\\brightstar;storeName={0}",
                                                      storeName)))
            {

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

        [Test]
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
            

            var job = embeddedClient.ExecuteTransaction(storeName, null, null, triples.ToString());
            TestHelper.AssertJobCompletesSuccessfully(embeddedClient, storeName, job);

            //check EF can access all properties
            using (
                var context =
                    new MyEntityContext(string.Format(@"type=embedded;storesDirectory=c:\\brightstar;storeName={0}",
                                                      storeName)))
            {

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
        }

       
#if !PORTABLE
        [Test]
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

            httpClient.ExecuteTransaction(storeName,null, null, triples.ToString());

            //check EF can access all properties
            using (
                var context =
                    new MyEntityContext(
                        string.Format(@"type=http;endpoint=http://localhost:8090/brightstar;storeName={0}", storeName)))
            {

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
#endif
    }
}
