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
	public partial class Bind : SparqlTest {

        public Bind() : base()
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
		public void Bind01Bind() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind01.rq");
			CheckResult(result, @"bind/bind01.srx", false);

	
		}

		[TestMethod]
		public void Bind02Bind() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind02.rq");
			CheckResult(result, @"bind/bind02.srx", false);

	
		}

		[TestMethod]
		public void Bind03Bind() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind03.rq");
			CheckResult(result, @"bind/bind03.srx", false);

	
		}

		[TestMethod]
		public void Bind04Bind() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind04.rq");
			CheckResult(result, @"bind/bind04.srx", false);

	
		}

		[TestMethod]
		public void Bind05Bind() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind05.rq");
			CheckResult(result, @"bind/bind05.srx", false);

	
		}

		[TestMethod]
		public void Bind06Bind() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind06.rq");
			CheckResult(result, @"bind/bind06.srx", false);

	
		}

		[TestMethod]
		public void Bind07Bind() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind07.rq");
			CheckResult(result, @"bind/bind07.srx", false);

	
		}

		[TestMethod]
		public void Bind08Bind() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind08.rq");
			CheckResult(result, @"bind/bind08.srx", false);

	
		}

		[TestMethod]
		public void Bind10BindScopingVariableInFilterNotInScope() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind10.rq");
			CheckResult(result, @"bind/bind10.srx", false);

	
		}

		[TestMethod]
		public void Bind11BindScopingVariableInFilterInScope() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind11.rq");
			CheckResult(result, @"bind/bind11.srx", false);

	
		}

		#endregion

		
	}
}