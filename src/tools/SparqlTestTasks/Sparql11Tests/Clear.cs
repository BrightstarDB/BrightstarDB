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
	public partial class Clear : SparqlTest {

        public Clear() : base()
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
		public void ClearDefault() {
				ImportData(@"clear/clear-default.ttl");
					ImportGraph(@"clear/clear-g1.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"clear/clear-g2.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"clear/clear-default-01.ru");
					ValidateUnamedGraph(@"clear/empty.ttl");
					ValidateGraph(@"clear/clear-g1.ttl", new Uri(@"http://example.org/g1"));
					ValidateGraph(@"clear/clear-g2.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[TestMethod]
		public void ClearGraph() {
				ImportData(@"clear/clear-default.ttl");
					ImportGraph(@"clear/clear-g1.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"clear/clear-g2.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"clear/clear-graph-01.ru");
					ValidateUnamedGraph(@"clear/clear-default.ttl");
					ValidateGraph(@"clear/empty.ttl", new Uri(@"http://example.org/g1"));
					ValidateGraph(@"clear/clear-g2.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[TestMethod]
		public void ClearNamed() {
				ImportData(@"clear/clear-default.ttl");
					ImportGraph(@"clear/clear-g1.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"clear/clear-g2.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"clear/clear-named-01.ru");
					ValidateUnamedGraph(@"clear/clear-default.ttl");
					ValidateGraph(@"clear/empty.ttl", new Uri(@"http://example.org/g1"));
					ValidateGraph(@"clear/empty.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[TestMethod]
		public void ClearAll() {
				ImportData(@"clear/clear-default.ttl");
					ImportGraph(@"clear/clear-g1.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"clear/clear-g2.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"clear/clear-all-01.ru");
					ValidateUnamedGraph(@"clear/empty.ttl");
					ValidateGraph(@"clear/empty.ttl", new Uri(@"http://example.org/g1"));
					ValidateGraph(@"clear/empty.ttl", new Uri(@"http://example.org/g2"));
			
		}

		#endregion

		
	}
}