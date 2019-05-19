using NUnit.Framework;
using System;

namespace BrightstarDB.Tests.Sparql11TestSuite {

	public partial class Exists : SparqlTest {

        public Exists() : base()
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
		public void ExistsWithinGraphPattern() {
	
					ImportData(@"exists/exists01.ttl");

                    ImportGraph("exists/exists02.ttl", new Uri(@"http://example.org/exists02.ttl"));
		
			var result = ExecuteQuery(@"exists/exists03.rq");
			CheckResult(result, @"exists/exists03.srx", false);

	
		}

		[Test]
		public void ExistsWithOneConstant() {
	
					ImportData(@"exists/exists01.ttl");
		
		
			var result = ExecuteQuery(@"exists/exists01.rq");
			CheckResult(result, @"exists/exists01.srx", false);

	
		}

		[Test]
		public void ExistsWithGroundTriple() {
	
					ImportData(@"exists/exists01.ttl");
		
		
			var result = ExecuteQuery(@"exists/exists02.rq");
			CheckResult(result, @"exists/exists02.srx", false);

	
		}

		[Test]
		public void NestedPositiveExists() {
	
					ImportData(@"exists/exists01.ttl");
		
		
			var result = ExecuteQuery(@"exists/exists04.rq");
			CheckResult(result, @"exists/exists04.srx", false);

	
		}

		[Test]
		public void NestedNegativeExistsInPositiveExists() {
	
					ImportData(@"exists/exists01.ttl");
		
		
			var result = ExecuteQuery(@"exists/exists05.rq");
			CheckResult(result, @"exists/exists05.srx", false);

	
		}

		#endregion

		
	}
}