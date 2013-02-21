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
	public partial class Exists : SparqlTest {

        public Exists() : base()
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
		public void ExistsWithinGraphPattern() {
	
					ImportData(@"exists/exists01.ttl");
		
					ImportGraph("exists/exists02.ttl", new Uri(@"file:///D:/Projects/brightstar/working/src/core/NetworkedPlanet.Brightstar.Tests/Data/sparql11_tests/exists/exists02.ttl"));
		
			var result = ExecuteQuery(@"exists/exists03.rq");
			CheckResult(result, @"exists/exists03.srx", false);

	
		}

		[TestMethod]
		public void ExistsWithOneConstant() {
	
					ImportData(@"exists/exists01.ttl");
		
		
			var result = ExecuteQuery(@"exists/exists01.rq");
			CheckResult(result, @"exists/exists01.srx", false);

	
		}

		[TestMethod]
		public void ExistsWithGroundTriple() {
	
					ImportData(@"exists/exists01.ttl");
		
		
			var result = ExecuteQuery(@"exists/exists02.rq");
			CheckResult(result, @"exists/exists02.srx", false);

	
		}

		[TestMethod]
		public void NestedPositiveExists() {
	
					ImportData(@"exists/exists01.ttl");
		
		
			var result = ExecuteQuery(@"exists/exists04.rq");
			CheckResult(result, @"exists/exists04.srx", false);

	
		}

		[TestMethod]
		public void NestedNegativeExistsInPositiveExists() {
	
					ImportData(@"exists/exists01.ttl");
		
		
			var result = ExecuteQuery(@"exists/exists05.rq");
			CheckResult(result, @"exists/exists05.srx", false);

	
		}

		#endregion

		
	}
}