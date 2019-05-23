using NUnit.Framework;

namespace BrightstarDB.Tests.Sparql11TestSuite {

    [Ignore("Server does not currently provide SPARQL JSON result sets")] // Ignore for now because we are not generating JSON results sets
	public partial class JsonRes : SparqlTest {

        public JsonRes() : base()
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
		public void Jsonres01JsonResultFormat() {
	
					ImportData(@"json-res/data.ttl");
		
		
			var result = ExecuteQuery(@"json-res/jsonres01.rq");
			CheckResult(result, @"json-res/jsonres01.srj", false);

	
		}

		[Test]
		public void Jsonres02JsonResultFormat() {
	
					ImportData(@"json-res/data.ttl");
		
		
			var result = ExecuteQuery(@"json-res/jsonres02.rq");
			CheckResult(result, @"json-res/jsonres02.srj", false);

	
		}

		[Test]
		public void Jsonres03JsonResultFormat() {
	
					ImportData(@"json-res/data.ttl");
		
		
			var result = ExecuteQuery(@"json-res/jsonres03.rq");
			CheckResult(result, @"json-res/jsonres03.srj", false);

	
		}

		[Test]
		public void Jsonres04JsonResultFormat() {
	
					ImportData(@"json-res/data.ttl");
		
		
			var result = ExecuteQuery(@"json-res/jsonres04.rq");
			CheckResult(result, @"json-res/jsonres04.srj", false);

	
		}

		#endregion

		
	}
}