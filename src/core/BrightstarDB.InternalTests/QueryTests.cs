using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using BrightstarDB.Client;
using BrightstarDB.Model;
using BrightstarDB.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VDS.RDF.Parsing;

namespace BrightstarDB.Tests
{
    [TestClass]
    public class QueryTests
    {
        private readonly IStoreManager _storeManager = StoreManagerFactory.GetStoreManager();

        [TestMethod]
        public void TestParser()
        {
            const string exp = "select ?t where { ?t a ?tt }";
            var parser = new SparqlQueryParser();
            var query = parser.ParseFromString(exp);            
            Assert.IsNotNull(query);
        }

        [TestMethod]
        public void TestSimpleQuery()
        {
            var sid = Guid.NewGuid().ToString();
            using (var store = _storeManager.CreateStore(Configuration.StoreLocation + "\\" + sid))
            {
                var t = new Triple
                            {
                                Subject = "http://www.networkedplanet.com/people/10",
                                Predicate = "http://www.networkedplanet.com/model/isa",
                                Object = "http://www.networkedplanet.com/types/person"
                            };
                store.InsertTriple(t);

                store.Commit(Guid.Empty);
            }
            using (var store = _storeManager.OpenStore(Configuration.StoreLocation + "\\" + sid))
            {
                var triples = store.GetResourceStatements("http://www.networkedplanet.com/people/10");
                Assert.AreEqual(1, triples.Count());

                // do query 
                const string query =
                    "select ?t where { ?t <http://www.networkedplanet.com/model/isa> <http://www.networkedplanet.com/types/person> }";
                store.ExecuteSparqlQuery(query, SparqlResultsFormat.Xml);
            }
        }

        [TestMethod]
        public void TestLookupByIdentifierAndType()
        {
            var sid = Guid.NewGuid().ToString();
            using (var store = _storeManager.CreateStore(Configuration.StoreLocation + "\\" + sid))
            {

                var t = new Triple
                            {
                                Subject = "http://www.networkedplanet.com/people/10",
                                Predicate = "http://www.networkedplanet.com/model/isa",
                                Object = "http://www.networkedplanet.com/types/person"
                            };
                store.InsertTriple(t);

                store.Commit(Guid.Empty);
            }

            using (var store = _storeManager.OpenStore(Configuration.StoreLocation + "\\" + sid))
            {
                // do query 
                const string query =
                    "select ?t where { ?t <http://www.networkedplanet.com/model/isa> <http://www.networkedplanet.com/types/person> . FILTER ( ?t = <http://www.networkedplanet.com/people/10> ) }";
                var doc = XDocument.Parse(store.ExecuteSparqlQuery(query, SparqlResultsFormat.Xml));

                Assert.IsTrue(doc.SparqlResultRows().Count() == 1);
                Assert.AreEqual("http://www.networkedplanet.com/people/10",
                                doc.SparqlResultRows().FirstOrDefault().GetColumnValue("t").ToString());
            }
        }

        [TestMethod]
        public void TestJoinTwoSingleVarClauses()
        {
            var sid = Guid.NewGuid().ToString();
            var store = _storeManager.CreateStore(Configuration.StoreLocation + "\\" + sid);
            store.InsertTriple("http://theforce.net/data/entry/1","http://theforce.net/schema/category", "http://theforce.net/data/category/1", false,null, null, Constants.DefaultGraphUri);
            store.InsertTriple("http://theforce.net/data/entry/1", "http://theforce.net/schema/fromPlanet", "http://theforce.net/data/planet/1", false, null, null, Constants.DefaultGraphUri);
            store.InsertTriple("http://theforce.net/data/planet/1", "http://theforce.net/schema/inSector", "http://theforce.net/data/sector/1", false, null, null, Constants.DefaultGraphUri);

            const string query = @"
PREFIX tf: <http://theforce.net/schema/>
SELECT ?entry ?sector WHERE { 
    ?entry tf:category <http://theforce.net/data/category/1> .
    ?entry tf:fromPlanet ?planet .
    ?planet tf:inSector <http://theforce.net/data/sector/1> .
}";
            var results = store.ExecuteSparqlQuery(query, SparqlResultsFormat.Xml);
            var resultsDoc = XDocument.Parse(results);
            var resultsCount = resultsDoc.SparqlResultRows().Count();
            Assert.AreEqual(1, resultsCount);
        }


        [TestMethod]
        public void BugzId5205()
        {
            var client = BrightstarService.GetClient("Type=embedded;storesDirectory=c:\\brightstar");
            string storeName = "WebnodesBrightstarRealisticTest_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            client.CreateStore(storeName);


            var initialData = new StringBuilder();

            initialData.AppendLine("<http://www.examplevocab.com/schema/Department> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://www.w3.org/2000/01/rdf-schema#Class> .");

            //roles
            initialData.AppendLine("<http://www.example.com/jobRole/development> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://www.examplevocab.com/schema/JobRole> . ");
            initialData.AppendLine("<http://www.example.com/jobRole/sales> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://www.examplevocab.com/schema/JobRole> . ");
            initialData.AppendLine("<http://www.example.com/jobRole/marketing> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://www.examplevocab.com/schema/JobRole> . ");
            initialData.AppendLine("<http://www.example.com/jobRole/administration> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://www.examplevocab.com/schema/JobRole> . ");
            initialData.AppendLine("<http://www.example.com/jobRole/management> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://www.examplevocab.com/schema/JobRole> . ");


            //100 employees            
            for (int i = 0; i < 100; i++)
            {
                initialData.AppendLine(string.Format("<http://www.example.com/people/person{0}> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://www.examplevocab.com/schema/Person> .", i));
                initialData.AppendLine(string.Format("<http://www.example.com/people/person{0}> <http://www.examplevocab.com/schema/personHasSkill> <http://www.example.com/skills/cSharp> .", i));
            }

            //10 departments.             
            for (int i = 0; i < 10; i++)
            {
                initialData.AppendLine(string.Format("<http://www.example.com/departments/department{0}> <http://www.examplevocab.com/schema/departmentCountry> <http://www.example.com/countries/country{0}> .", i));  //each department is located inits own country               
            }

            //100 employees, 10 departments = 10 employees pr department
            int e = 0; int d = 0;
            while (e < 100)
            {

                initialData.AppendLine(string.Format("<http://www.example.com/departments/department{0}> <http://www.examplevocab.com/schema/departmentEmploymentRecord> <http://www.example.com/depemplrec/depemplrec{1}> .", d, e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployeeRole> <http://www.example.com/jobRole/development> .", e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployee> <http://www.example.com/people/person{0}> .", e));
                e = e + 1;
                initialData.AppendLine(string.Format("<http://www.example.com/departments/department{0}> <http://www.examplevocab.com/schema/departmentEmploymentRecord> <http://www.example.com/depemplrec/depemplrec{1}> .", d, e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployeeRole> <http://www.example.com/jobRole/development> .", e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployee> <http://www.example.com/people/person{0}> .", e));
                e = e + 1;
                //2 sales guys in each department etc
                initialData.AppendLine(string.Format("<http://www.example.com/departments/department{0}> <http://www.examplevocab.com/schema/departmentEmploymentRecord> <http://www.example.com/depemplrec/depemplrec{1}> .", d, e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployeeRole> <http://www.example.com/jobRole/sales> .", e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployee> <http://www.example.com/people/person{0}> .", e));
                e = e + 1;
                initialData.AppendLine(string.Format("<http://www.example.com/departments/department{0}> <http://www.examplevocab.com/schema/departmentEmploymentRecord> <http://www.example.com/depemplrec/depemplrec{1}> .", d, e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployeeRole> <http://www.example.com/jobRole/sales> .", e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployee> <http://www.example.com/people/person{0}> .", e));
                e = e + 1;
                //etc...
                initialData.AppendLine(string.Format("<http://www.example.com/departments/department{0}> <http://www.examplevocab.com/schema/departmentEmploymentRecord> <http://www.example.com/depemplrec/depemplrec{1}> .", d, e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployeeRole> <http://www.example.com/jobRole/marketing> .", e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployee> <http://www.example.com/people/person{0}> .", e));
                e = e + 1;
                initialData.AppendLine(string.Format("<http://www.example.com/departments/department{0}> <http://www.examplevocab.com/schema/departmentEmploymentRecord> <http://www.example.com/depemplrec/depemplrec{1}> .", d, e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployeeRole> <http://www.example.com/jobRole/marketing> .", e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployee> <http://www.example.com/people/person{0}> .", e));
                e = e + 1;
                initialData.AppendLine(string.Format("<http://www.example.com/departments/department{0}> <http://www.examplevocab.com/schema/departmentEmploymentRecord> <http://www.example.com/depemplrec/depemplrec{1}> .", d, e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployeeRole> <http://www.example.com/jobRole/administration> .", e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployee> <http://www.example.com/people/person{0}> .", e));
                e = e + 1;
                initialData.AppendLine(string.Format("<http://www.example.com/departments/department{0}> <http://www.examplevocab.com/schema/departmentEmploymentRecord> <http://www.example.com/depemplrec/depemplrec{1}> .", d, e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployeeRole> <http://www.example.com/jobRole/administration> .", e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployee> <http://www.example.com/people/person{0}> .", e));
                e = e + 1;
                initialData.AppendLine(string.Format("<http://www.example.com/departments/department{0}> <http://www.examplevocab.com/schema/departmentEmploymentRecord> <http://www.example.com/depemplrec/depemplrec{1}> .", d, e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployeeRole> <http://www.example.com/jobRole/management> .", e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployee> <http://www.example.com/people/person{0}> .", e));
                e = e + 1;
                initialData.AppendLine(string.Format("<http://www.example.com/departments/department{0}> <http://www.examplevocab.com/schema/departmentEmploymentRecord> <http://www.example.com/depemplrec/depemplrec{1}> .", d, e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployeeRole> <http://www.example.com/jobRole/management> .", e));
                initialData.AppendLine(string.Format("<http://www.example.com/depemplrec/depemplrec{0}> <http://www.examplevocab.com/schema/departmentEmployee> <http://www.example.com/people/person{0}> .", e));
                e = e + 1;
                d = d + 1;

            }

            string nTriples = initialData.ToString();
            //insert the dummy data      
            client.ExecuteTransaction(storeName, null, null, nTriples, Constants.DefaultGraphUri);

            //select number of employees with job role = administration
            var result = XDocument.Load(client.ExecuteQuery(storeName, "SELECT count(?employee) as ?ugh WHERE {?employeerecord <http://www.examplevocab.com/schema/departmentEmployeeRole> <http://www.example.com/jobRole/administration> . ?employeerecord <http://www.examplevocab.com/schema/departmentEmployee> ?employee  }  "));
            var ugh = (int)result.SparqlResultRows().First().GetColumnValue("ugh");
            Assert.AreEqual(20, ugh);
        }

        [TestMethod]
        [Ignore]
        public void TestSparqlLimit()
        {
            var sid = Guid.NewGuid().ToString();
            var store = _storeManager.CreateStore(Configuration.StoreLocation + "\\" + sid);
            store.InsertTriple("http://theforce.net/data/entry/1", "http://theforce.net/schema/category", "http://theforce.net/data/category/1", false, null, null, Constants.DefaultGraphUri);
            store.InsertTriple("http://theforce.net/data/entry/1", "http://theforce.net/schema/fromPlanet", "http://theforce.net/data/planet/1", false, null, null, Constants.DefaultGraphUri);
            store.InsertTriple("http://theforce.net/data/entry/2", "http://theforce.net/schema/category", "http://theforce.net/data/category/1", false, null, null, Constants.DefaultGraphUri);
            store.InsertTriple("http://theforce.net/data/entry/2", "http://theforce.net/schema/fromPlanet", "http://theforce.net/data/planet/1", false, null, null, Constants.DefaultGraphUri);

            const string query = @"
PREFIX tf: <http://theforce.net/schema/>
SELECT ?entry ?planet WHERE { 
    ?entry tf:category <http://theforce.net/data/category/1> .
    ?entry tf:fromPlanet ?planet .    
} LIMIT 20";
            var results = store.ExecuteSparqlQuery(query, SparqlResultsFormat.Xml);
            var resultsDoc = XDocument.Parse(results);

            foreach (var sparqlResultRow in resultsDoc.SparqlResultRows())
            {
                Assert.IsNotNull(sparqlResultRow.GetColumnValue("entry"));
                Assert.IsNotNull(sparqlResultRow.GetColumnValue("planet"));
            }
        }

    }
}
