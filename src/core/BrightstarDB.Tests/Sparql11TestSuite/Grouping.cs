using NUnit.Framework;

namespace BrightstarDB.Tests.Sparql11TestSuite {

	public partial class Grouping : SparqlTest {

        public Grouping() : base()
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
		public void Group1() {
	
					ImportData(@"grouping/group-data-1.ttl");
		
		
			var result = ExecuteQuery(@"grouping/group01.rq");
			CheckResult(result, @"grouping/group01.srx", false);

	
		}

		[Test]
		public void Group2() {
	
					ImportData(@"grouping/group-data-1.ttl");
		
		
			var result = ExecuteQuery(@"grouping/group02.rq");
			CheckResult(result, @"grouping/group02.srx", false);

	
		}

		[Test]
		public void Group3() {
	
					ImportData(@"grouping/group-data-1.ttl");
		
		
			var result = ExecuteQuery(@"grouping/group03.rq");
			CheckResult(result, @"grouping/group03.srx", false);

	
		}

		[Test]
		public void Group4() {
	
					ImportData(@"grouping/group-data-1.ttl");
		
		
			var result = ExecuteQuery(@"grouping/group04.rq");
			CheckResult(result, @"grouping/group04.srx", false);

	
		}

		[Test]
		public void Group5() {
	
					ImportData(@"grouping/group-data-2.ttl");
		
		
			var result = ExecuteQuery(@"grouping/group05.rq");
			CheckResult(result, @"grouping/group05.srx", false);

	
		}

		#endregion

		
	}
}