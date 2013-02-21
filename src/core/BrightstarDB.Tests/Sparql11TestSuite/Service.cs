using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.Sparql11TestSuite {
    [TestClass]
    [Ignore] // Added ignore because the SERVICE tests make bad assumptions about the remote endpoint
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