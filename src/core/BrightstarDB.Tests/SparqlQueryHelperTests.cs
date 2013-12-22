using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.Utils;
using NUnit.Framework;
using VDS.RDF.Parsing;

namespace BrightstarDB.Tests
{
    [TestFixture]
    public class SparqlQueryHelperTests
    {
        [Test]
        public void TestEvaluateSelect()
        {
            Assert.AreEqual(SerializableModel.SparqlResultSet,
                SparqlQueryHelper.GetResultModel("SELECT ?x WHERE { ?x a <http://xmlns.com/foaf/0.1/Person> }"));
        }

        [Test]
        public void TestEvaluateDescribe()
        {
            Assert.AreEqual(SerializableModel.RdfGraph,
                SparqlQueryHelper.GetResultModel("DESCRIBE ?x WHERE { ?x a <http://xmlns.com/foaf/0.1/Person> }"));
        }

        [Test]
        public void TestEvaluateConstruct()
        {
            Assert.AreEqual(SerializableModel.RdfGraph,
                SparqlQueryHelper.GetResultModel("CONSTRUCT { ?x ?p ?o } WHERE { ?x a <http://xmlns.com/foaf/0.1/Person> . ?x ?p ?o }"));
        }

        [Test]
        [ExpectedException(typeof (RdfParseException))]
        public void TestEvaluateInvalidQueryThrowsRdfParseException()
        {
            SparqlQueryHelper.GetResultModel("BAD SPARQL QUERY");
        }
    }
}
