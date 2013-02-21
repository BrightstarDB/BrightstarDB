using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BrightstarDB.Client;
namespace SparqlServiceTests
{
    [TestClass]
    public class TestSparqlQuery
    {
        public TestSparqlQuery()
        {
            var client =
                BrightstarDB.Client.BrightstarService.GetClient("type=http;endpoint=http://localhost:8090/brightstar");
            if (client.DoesStoreExist("SparqlServiceTest"))
            {
                client.DeleteStore("SparqlServiceTest");
            }
            client.CreateStore("SparqlServiceTest");

            var result = client.ExecuteTransaction("SparqlServiceTest", null, null,
                                      "<http://brightstardb.com/people/kal> <http://brightstardb.com/types/hasSkill> <http://brightstardb.com/skills/csharp> .");
            Assert.IsTrue(result.JobCompletedOk);
        }

        [TestMethod]
        public void TestGetXmlResponse()
        {
            var request =
                WebRequest.Create("http://localhost:56692/SparqlServiceTest/sparql?query=SELECT * WHERE {?s ?p ?o}") as
                HttpWebRequest;
            request.Accept = "application/xml";
            var response = request.GetResponse();
            using (var responseStream= response.GetResponseStream())
            {
                var responseDoc = XDocument.Load(responseStream);
                Assert.AreEqual(1, responseDoc.SparqlResultRows().Count());
            }
        }

        [TestMethod]
        public void TestGetJsonResponse()
        {
            var request =
                WebRequest.Create("http://localhost:56692/SparqlServiceTest/sparql?query=SELECT * WHERE {?s ?p ?o}") as
                HttpWebRequest;
            request.Accept = "application/sparql-results+json";
            var response = request.GetResponse();
            string szResult;
            using(var responseStream = response.GetResponseStream())
            {
                var tr = new StreamReader(responseStream);
                szResult = tr.ReadToEnd();
            }
            var jss = new JavaScriptSerializer();
            jss.RegisterConverters(new JavaScriptConverter[]{new DynamicJsonConverter(), });
                var jsonResult = jss.Deserialize(szResult, typeof (object)) as dynamic;
                Assert.IsNotNull(jsonResult.head);
                Assert.IsNotNull(jsonResult.results);
            
        }
    }
}
