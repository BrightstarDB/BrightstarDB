using NUnit.Framework;
using System;

namespace BrightstarDB.Tests.Sparql11TestSuite {
    
	public partial class Subquery : SparqlTest {

        public Subquery() : base()
        {
            
        }

		[SetUp]
		public void SetUp()
		{
			CreateStore();
		    
		}

        [TearDown]
        public void TearDown()
        {
			DeleteStore();
            
        }

		#region Test Methods

		[Test]
		public void Sq11SubqueryLimitPerResource() {
	
					ImportData(@"subquery/sq11.ttl");
		
		
			var result = ExecuteQuery(@"subquery/sq11.rq");
			CheckResult(result, @"subquery/sq11.srx", false);

	
		}

		[Test]
		public void Sq12SubqueryInConstructWithBuiltIns() {
	
					ImportData(@"subquery/sq12.ttl");
		
		
			var result = ExecuteQuery(@"subquery/sq12.rq");
			CheckResult(result, @"subquery/sq12_out.ttl", false);

	
		}

		[Test]
		public void Sq13SubqueriesDonTInjectBindings() {
	
					ImportData(@"subquery/sq11.ttl");
		
		
			var result = ExecuteQuery(@"subquery/sq11.rq");
			CheckResult(result, @"subquery/sq11.srx", false);

	
		}

		[Test]
		public void Sq04SubqueryWithinGraphPatternDefaultGraphDoesNotApply() {
	
					ImportData(@"subquery/sq04.rdf");
		
					ImportGraph("subquery/sq01.rdf", new Uri(@"file:///D:/Projects/brightstar/working/src/core/NetworkedPlanet.Brightstar.Tests/Data/sparql11_tests/subquery/sq01.rdf"));
		
			var result = ExecuteQuery(@"subquery/sq04.rq");
			CheckResult(result, @"subquery/sq04.srx", false);

	
		}

		[Test]
		public void Sq06SubqueryWithGraphPatternFromNamedApplies() {
	
					ImportData(@"subquery/sq05.rdf");
		
		
			var result = ExecuteQuery(@"subquery/sq06.rq");
			CheckResult(result, @"subquery/sq06.srx", false);

	
		}

		[Test]
		public void Sq08SubqueryWithAggregate() {
	
					ImportData(@"subquery/sq08.rdf");
		
		
			var result = ExecuteQuery(@"subquery/sq08.rq");
			CheckResult(result, @"subquery/sq08.srx", false);

	
		}

		[Test]
		public void Sq09NestedSubqueries() {
	
					ImportData(@"subquery/sq09.rdf");
		
		
			var result = ExecuteQuery(@"subquery/sq09.rq");
			CheckResult(result, @"subquery/sq09.srx", false);

	
		}

		[Test]
		public void Sq10SubqueryWithExists() {
	
					ImportData(@"subquery/sq10.rdf");
		
		
			var result = ExecuteQuery(@"subquery/sq10.rq");
			CheckResult(result, @"subquery/sq10.srx", false);

	
		}

		[Test]
		public void Sq14LimitByResource() {
	
					ImportData(@"subquery/sq14.ttl");
		
		
			var result = ExecuteQuery(@"subquery/sq14.rq");
			CheckResult(result, @"subquery/sq14-out.ttl", false);

	
		}

		#endregion

		
	}
}