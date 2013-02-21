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
	public partial class Service : SparqlTest {

        public Service() : base()
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
		public void ServiceTest1() {
	
					ImportData(@"service/data01.ttl");
		
		
			var result = ExecuteQuery(@"service/service01.rq");
			CheckResult(result, @"service/service01.srx", false);

	
		}

		[TestMethod]
		public void ServiceTest4() {
	
					ImportData(@"service/data04.ttl");
		
		
			var result = ExecuteQuery(@"service/service04.rq");
			CheckResult(result, @"service/service04.srx", false);

	
		}

		[TestMethod]
		public void ServiceTest5() {
	
					ImportData(@"service/data05.ttl");
		
		
			var result = ExecuteQuery(@"service/service05.rq");
			CheckResult(result, @"service/service05.srx", false);

	
		}

		[TestMethod]
		public void ServiceTest7() {
	
					ImportData(@"service/data07.ttl");
		
		
			var result = ExecuteQuery(@"service/service07.rq");
			CheckResult(result, @"service/service07.srx", false);

	
		}

		#endregion

		
	}
}