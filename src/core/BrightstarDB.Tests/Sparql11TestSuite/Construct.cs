using NUnit.Framework;

namespace BrightstarDB.Tests.Sparql11TestSuite {
    
	public partial class Construct : SparqlTest {

        public Construct() : base()
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
		public void Constructwhere01ConstructWhere() {
	
					ImportData(@"construct/data.ttl");
		
		
			var result = ExecuteQuery(@"construct/constructwhere01.rq");
			CheckResult(result, @"construct/constructwhere01result.ttl", false);

	
		}

		[Test]
		public void Constructwhere02ConstructWhere() {
	
					ImportData(@"construct/data.ttl");
		
		
			var result = ExecuteQuery(@"construct/constructwhere02.rq");
			CheckResult(result, @"construct/constructwhere02result.ttl", false);

	
		}

		[Test]
		public void Constructwhere03ConstructWhere() {
	
					ImportData(@"construct/data.ttl");
		
		
			var result = ExecuteQuery(@"construct/constructwhere03.rq");
			CheckResult(result, @"construct/constructwhere03result.ttl", false);

	
		}

		#endregion

		
	}
}