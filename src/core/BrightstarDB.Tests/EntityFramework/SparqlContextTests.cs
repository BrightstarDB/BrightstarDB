using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using BrightstarDB.Client;
using Moq;
using VDS.RDF.Query;
using VDS.RDF.Update;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestFixture]
    public class SparqlContextTests
    {
        [Test]
        public void TestReadOnlyConnection()
        {
            var context = new MyEntityContext("type=sparql;query=http://dbpedia.org/sparql");
            Assert.IsNotNull(context);
        }

#if !PORTABLE && !WINDOWS_PHONE // SPARQL Update not supported on these platforms yet
        [Test]
        public void TestReadWriteConnection()
        {
            var doContext =
                new MyEntityContext(
                    "type=sparql;query=http://example.org/sparql;update=http://example.org/update");
            Assert.IsNotNull(doContext);
        }
#endif

        [Test]
        public void TestDeleteWildcards()
        {
            var mockQueryProcessor = new Mock<ISparqlQueryProcessor>();
            var mockUpdateProcessor = new Mock<ISparqlUpdateProcessor>();
            mockUpdateProcessor.Setup(m => m.ProcessCommandSet(It.IsAny<SparqlUpdateCommandSet>()));
            var doContext = new SparqlDataObjectContext(mockQueryProcessor.Object, mockUpdateProcessor.Object, false);
            var store = doContext.OpenStore("sparql");
            var context = new MyEntityContext(store);
            var p = context.Persons.Create();
            var pid = p.Id;
            context.SaveChanges();

            context.DeleteObject(p);
            context.SaveChanges();

            mockUpdateProcessor.Verify(m=>m.ProcessCommandSet(It.Is<SparqlUpdateCommandSet>(s=>s.CommandCount == 3
                && s[1].CommandType == SparqlUpdateCommandType.Delete && s[1].ToString().Contains("?d0 ?d1 <" + Constants.GeneratedUriPrefix + pid + ">")) 
                ));

            mockQueryProcessor.VerifyAll();
            mockUpdateProcessor.VerifyAll();
        }
    }
}
