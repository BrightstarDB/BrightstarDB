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
	public partial class Grouping : SparqlTest {

        public Grouping() : base()
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
		public void Group1() {
	
					ImportData(@"grouping/group-data-1.ttl");
		
		
			var result = ExecuteQuery(@"grouping/group01.rq");
			CheckResult(result, @"grouping/group01.srx", false);

	
		}

		[TestMethod]
		public void Group2() {
	
					ImportData(@"grouping/group-data-1.ttl");
		
		
			var result = ExecuteQuery(@"grouping/group02.rq");
			CheckResult(result, @"grouping/group02.srx", false);

	
		}

		[TestMethod]
		public void Group3() {
	
					ImportData(@"grouping/group-data-1.ttl");
		
		
			var result = ExecuteQuery(@"grouping/group03.rq");
			CheckResult(result, @"grouping/group03.srx", false);

	
		}

		[TestMethod]
		public void Group4() {
	
					ImportData(@"grouping/group-data-1.ttl");
		
		
			var result = ExecuteQuery(@"grouping/group04.rq");
			CheckResult(result, @"grouping/group04.srx", false);

	
		}

		[TestMethod]
		public void Group5() {
	
					ImportData(@"grouping/group-data-2.ttl");
		
		
			var result = ExecuteQuery(@"grouping/group05.rq");
			CheckResult(result, @"grouping/group05.srx", false);

	
		}

		#endregion

		
	}
}