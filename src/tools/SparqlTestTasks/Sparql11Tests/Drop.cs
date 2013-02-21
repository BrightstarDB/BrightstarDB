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
	public partial class Drop : SparqlTest {

        public Drop() : base()
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
		public void DropDefault() {
				ImportData(@"drop/drop-default.ttl");
					ImportGraph(@"drop/drop-g1.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"drop/drop-g2.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"drop/drop-default-01.ru");
					ValidateGraph(@"drop/drop-g1.ttl", new Uri(@"http://example.org/g1"));
					ValidateGraph(@"drop/drop-g2.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[TestMethod]
		public void DropGraph() {
				ImportData(@"drop/drop-default.ttl");
					ImportGraph(@"drop/drop-g1.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"drop/drop-g2.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"drop/drop-graph-01.ru");
					ValidateUnamedGraph(@"drop/drop-default.ttl");
					ValidateGraph(@"drop/drop-g2.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[TestMethod]
		public void DropNamed() {
				ImportData(@"drop/drop-default.ttl");
					ImportGraph(@"drop/drop-g1.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"drop/drop-g2.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"drop/drop-named-01.ru");
					ValidateUnamedGraph(@"drop/drop-default.ttl");
			
		}

		[TestMethod]
		public void DropAll() {
				ImportData(@"drop/drop-default.ttl");
					ImportGraph(@"drop/drop-g1.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"drop/drop-g2.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"drop/drop-all-01.ru");
			
		}

		#endregion

		
	}
}