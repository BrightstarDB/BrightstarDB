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
	public partial class Construct : SparqlTest {

        public Construct() : base()
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
		public void Constructwhere01ConstructWhere() {
	
					ImportData(@"construct/data.ttl");
		
		
			var result = ExecuteQuery(@"construct/constructwhere01.rq");
			CheckResult(result, @"construct/constructwhere01result.ttl", false);

	
		}

		[TestMethod]
		public void Constructwhere02ConstructWhere() {
	
					ImportData(@"construct/data.ttl");
		
		
			var result = ExecuteQuery(@"construct/constructwhere02.rq");
			CheckResult(result, @"construct/constructwhere02result.ttl", false);

	
		}

		[TestMethod]
		public void Constructwhere03ConstructWhere() {
	
					ImportData(@"construct/data.ttl");
		
		
			var result = ExecuteQuery(@"construct/constructwhere03.rq");
			CheckResult(result, @"construct/constructwhere03result.ttl", false);

	
		}

		#endregion

		
	}
}