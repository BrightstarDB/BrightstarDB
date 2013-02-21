using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.Sparql11TestSuite {
    [TestClass]
    [Ignore] // Ignore for now because we are not generating JSON results sets
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