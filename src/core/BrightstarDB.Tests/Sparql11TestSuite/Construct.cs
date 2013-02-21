using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.Sparql11TestSuite {
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