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
	public partial class JsonRes : SparqlTest {

        public JsonRes() : base()
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
		public void Jsonres01JsonResultFormat() {
	
					ImportData(@"json-res/data.ttl");
		
		
			var result = ExecuteQuery(@"json-res/jsonres01.rq");
			CheckResult(result, @"json-res/jsonres01.srj", false);

	
		}

		[TestMethod]
		public void Jsonres02JsonResultFormat() {
	
					ImportData(@"json-res/data.ttl");
		
		
			var result = ExecuteQuery(@"json-res/jsonres02.rq");
			CheckResult(result, @"json-res/jsonres02.srj", false);

	
		}

		[TestMethod]
		public void Jsonres03JsonResultFormat() {
	
					ImportData(@"json-res/data.ttl");
		
		
			var result = ExecuteQuery(@"json-res/jsonres03.rq");
			CheckResult(result, @"json-res/jsonres03.srj", false);

	
		}

		[TestMethod]
		public void Jsonres04JsonResultFormat() {
	
					ImportData(@"json-res/data.ttl");
		
		
			var result = ExecuteQuery(@"json-res/jsonres04.rq");
			CheckResult(result, @"json-res/jsonres04.srj", false);

	
		}

		#endregion

		
	}
}