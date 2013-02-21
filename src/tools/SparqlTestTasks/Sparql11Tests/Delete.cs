using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using NetworkedPlanet.Brightstar;
using NetworkedPlanet.Brightstar.Storage;
using NetworkedPlanet.Rdf;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using System.Linq;

namespace NetworkedPlanet.Brightstar.Tests.Sparql11TestSuite {
    [TestClass]
	public partial class Delete : SparqlTest {

        public Delete() : base()
        {
            
        }

		[TestInitialize]
		public void SetUp()
		{
			CreateStore();
		    
		}

        [TestCleanup]
        public void TearDown()
        {
			DeleteStore();
            
        }

		#region Test Methods

		[TestMethod]
		public void SimpleDelete1() {
				ImportData(@"delete/delete-pre-01.ttl");
				ExecuteUpdate(@"delete/delete-01.ru");
					ValidateUnamedGraph(@"delete/delete-post-01s.ttl");
			
		}

		[TestMethod]
		public void SimpleDelete2() {
				ImportGraph(@"delete/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
				ExecuteUpdate(@"delete/delete-02.ru");
					ValidateGraph(@"delete/delete-post-01s.ttl", new Uri(@"http://example.org/g1"));
			
		}

		[TestMethod]
		public void SimpleDelete3() {
				ImportData(@"delete/delete-pre-01.ttl");
				ExecuteUpdate(@"delete/delete-03.ru");
					ValidateUnamedGraph(@"delete/delete-post-01f.ttl");
			
		}

		[TestMethod]
		public void SimpleDelete4() {
				ImportGraph(@"delete/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
				ExecuteUpdate(@"delete/delete-04.ru");
					ValidateGraph(@"delete/delete-post-01f.ttl", new Uri(@"http://example.org/g1"));
			
		}

		[TestMethod]
		public void GraphSpecificDelete1() {
				ImportData(@"delete/delete-pre-01.ttl");
					ImportGraph(@"delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
					ImportGraph(@"delete/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
				ExecuteUpdate(@"delete/delete-05.ru");
					ValidateUnamedGraph(@"delete/delete-post-01s.ttl");
					ValidateGraph(@"delete/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));
					ValidateGraph(@"delete/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));
			
		}

		[TestMethod]
		public void GraphSpecificDelete2() {
				ImportData(@"delete/delete-pre-01.ttl");
					ImportGraph(@"delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
					ImportGraph(@"delete/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
				ExecuteUpdate(@"delete/delete-06.ru");
					ValidateUnamedGraph(@"delete/delete-post-01f.ttl");
					ValidateGraph(@"delete/delete-post-02s.ttl", new Uri(@"http://example.org/g2"));
					ValidateGraph(@"delete/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));
			
		}

		[TestMethod]
		public void SimpleDelete7() {
				ImportData(@"delete/delete-pre-01.ttl");
				ExecuteUpdate(@"delete/delete-07.ru");
					ValidateUnamedGraph(@"delete/delete-post-01f.ttl");
			
		}

		[TestMethod]
		public void SimpleDelete1With() {
				ImportGraph(@"delete/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
				ExecuteUpdate(@"delete/delete-with-01.ru");
					ValidateGraph(@"delete/delete-post-01s.ttl", new Uri(@"http://example.org/g1"));
			
		}

		[TestMethod]
		public void SimpleDelete2With() {
				ImportGraph(@"delete/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"delete/delete-with-02.ru");
					ValidateGraph(@"delete/delete-post-01s.ttl", new Uri(@"http://example.org/g1"));
					ValidateGraph(@"delete/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[TestMethod]
		public void SimpleDelete3With() {
				ImportGraph(@"delete/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
				ExecuteUpdate(@"delete/delete-with-03.ru");
					ValidateGraph(@"delete/delete-post-01f.ttl", new Uri(@"http://example.org/g1"));
			
		}

		[TestMethod]
		public void SimpleDelete4With() {
				ImportGraph(@"delete/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"delete/delete-with-04.ru");
					ValidateGraph(@"delete/delete-post-01f.ttl", new Uri(@"http://example.org/g1"));
					ValidateGraph(@"delete/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[TestMethod]
		public void GraphSpecificDelete1With() {
				ImportGraph(@"delete/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
					ImportGraph(@"delete/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
				ExecuteUpdate(@"delete/delete-with-05.ru");
					ValidateGraph(@"delete/delete-post-01s2.ttl", new Uri(@"http://example.org/g1"));
					ValidateGraph(@"delete/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));
					ValidateGraph(@"delete/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));
			
		}

		[TestMethod]
		public void GraphSpecificDelete2With() {
				ImportData(@"delete/delete-pre-01.ttl");
					ImportGraph(@"delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
					ImportGraph(@"delete/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
				ExecuteUpdate(@"delete/delete-with-06.ru");
					ValidateUnamedGraph(@"delete/delete-post-01f.ttl");
					ValidateGraph(@"delete/delete-post-02s.ttl", new Uri(@"http://example.org/g2"));
					ValidateGraph(@"delete/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));
			
		}

		[TestMethod]
		public void SimpleDelete1Using() {
				ImportData(@"delete/delete-pre-01.ttl");
					ImportGraph(@"delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"delete/delete-using-01.ru");
					ValidateUnamedGraph(@"delete/delete-post-01s.ttl");
					ValidateGraph(@"delete/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[TestMethod]
		public void SimpleDelete2Using() {
				ImportData(@"delete/delete-pre-01.ttl");
					ImportGraph(@"delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
					ImportGraph(@"delete/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
				ExecuteUpdate(@"delete/delete-using-02.ru");
					ValidateUnamedGraph(@"delete/delete-post-01s.ttl");
					ValidateGraph(@"delete/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));
					ValidateGraph(@"delete/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));
			
		}

		[TestMethod]
		public void SimpleDelete3Using() {
				ImportData(@"delete/delete-pre-01.ttl");
					ImportGraph(@"delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"delete/delete-using-03.ru");
					ValidateUnamedGraph(@"delete/delete-post-01f.ttl");
					ValidateGraph(@"delete/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[TestMethod]
		public void SimpleDelete4Using() {
				ImportData(@"delete/delete-pre-03.ttl");
					ImportGraph(@"delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
					ImportGraph(@"delete/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
				ExecuteUpdate(@"delete/delete-using-04.ru");
					ValidateUnamedGraph(@"delete/delete-post-03f.ttl");
					ValidateGraph(@"delete/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));
					ValidateGraph(@"delete/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));
			
		}

		[TestMethod]
		public void GraphSpecificDelete1Using() {
				ImportGraph(@"delete/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
					ImportGraph(@"delete/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
				ExecuteUpdate(@"delete/delete-using-05.ru");
					ValidateGraph(@"delete/delete-post-01s2.ttl", new Uri(@"http://example.org/g1"));
					ValidateGraph(@"delete/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));
					ValidateGraph(@"delete/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));
			
		}

		[TestMethod]
		public void GraphSpecificDelete2Using() {
				ImportGraph(@"delete/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
					ImportGraph(@"delete/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
				ExecuteUpdate(@"delete/delete-using-06.ru");
					ValidateGraph(@"delete/delete-post-01f.ttl", new Uri(@"http://example.org/g1"));
					ValidateGraph(@"delete/delete-post-02s.ttl", new Uri(@"http://example.org/g2"));
					ValidateGraph(@"delete/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));
			
		}

		#endregion

		
	}
}