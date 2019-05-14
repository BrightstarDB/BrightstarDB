using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace BrightstarDB.Tests.Sparql11TestSuite {
    [TestClass]
	public partial class Subquery : SparqlTest {

        public Subquery() : base()
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
		public void Sq11SubqueryLimitPerResource() {
	
					ImportData(@"subquery/sq11.ttl");
		
		
			var result = ExecuteQuery(@"subquery/sq11.rq");
			CheckResult(result, @"subquery/sq11.srx", false);

	
		}

		[TestMethod]
		public void Sq12SubqueryInConstructWithBuiltIns() {
	
					ImportData(@"subquery/sq12.ttl");
		
		
			var result = ExecuteQuery(@"subquery/sq12.rq");
			CheckResult(result, @"subquery/sq12_out.ttl", false);

	
		}

		[TestMethod]
		public void Sq13SubqueriesDonTInjectBindings() {
	
					ImportData(@"subquery/sq11.ttl");
		
		
			var result = ExecuteQuery(@"subquery/sq11.rq");
			CheckResult(result, @"subquery/sq11.srx", false);

	
		}

		[TestMethod]
		public void Sq04SubqueryWithinGraphPatternDefaultGraphDoesNotApply() {
	
					ImportData(@"subquery/sq04.rdf");
		
					ImportGraph("subquery/sq01.rdf", new Uri(@"file:///D:/Projects/brightstar/working/src/core/NetworkedPlanet.Brightstar.Tests/Data/sparql11_tests/subquery/sq01.rdf"));
		
			var result = ExecuteQuery(@"subquery/sq04.rq");
			CheckResult(result, @"subquery/sq04.srx", false);

	
		}

		[TestMethod]
		public void Sq06SubqueryWithGraphPatternFromNamedApplies() {
	
					ImportData(@"subquery/sq05.rdf");
		
		
			var result = ExecuteQuery(@"subquery/sq06.rq");
			CheckResult(result, @"subquery/sq06.srx", false);

	
		}

		[TestMethod]
		public void Sq08SubqueryWithAggregate() {
	
					ImportData(@"subquery/sq08.rdf");
		
		
			var result = ExecuteQuery(@"subquery/sq08.rq");
			CheckResult(result, @"subquery/sq08.srx", false);

	
		}

		[TestMethod]
		public void Sq09NestedSubqueries() {
	
					ImportData(@"subquery/sq09.rdf");
		
		
			var result = ExecuteQuery(@"subquery/sq09.rq");
			CheckResult(result, @"subquery/sq09.srx", false);

	
		}

		[TestMethod]
		public void Sq10SubqueryWithExists() {
	
					ImportData(@"subquery/sq10.rdf");
		
		
			var result = ExecuteQuery(@"subquery/sq10.rq");
			CheckResult(result, @"subquery/sq10.srx", false);

	
		}

		[TestMethod]
		public void Sq14LimitByResource() {
	
					ImportData(@"subquery/sq14.ttl");
		
		
			var result = ExecuteQuery(@"subquery/sq14.rq");
			CheckResult(result, @"subquery/sq14-out.ttl", false);

	
		}

		#endregion

		
	}
}