using NUnit.Framework;

namespace BrightstarDB.Tests.Sparql11TestSuite {
    
	public partial class Bind : SparqlTest {

        public Bind() : base()
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
		public void Bind01Bind() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind01.rq");
			CheckResult(result, @"bind/bind01.srx", false);

	
		}

		[Test]
		public void Bind02Bind() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind02.rq");
			CheckResult(result, @"bind/bind02.srx", false);

	
		}

		[Test]
		public void Bind03Bind() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind03.rq");
			CheckResult(result, @"bind/bind03.srx", false);

	
		}

		[Test]
		public void Bind04Bind() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind04.rq");
			CheckResult(result, @"bind/bind04.srx", false);

	
		}

		[Test]
		public void Bind05Bind() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind05.rq");
			CheckResult(result, @"bind/bind05.srx", false);

	
		}

		[Test]
		public void Bind06Bind() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind06.rq");
			CheckResult(result, @"bind/bind06.srx", false);

	
		}

		[Test]
		public void Bind07Bind() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind07.rq");
			CheckResult(result, @"bind/bind07.srx", false);

	
		}

		[Test]
		public void Bind08Bind() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind08.rq");
			CheckResult(result, @"bind/bind08.srx", false);

	
		}

		[Test]
		public void Bind10BindScopingVariableInFilterNotInScope() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind10.rq");
			CheckResult(result, @"bind/bind10.srx", false);

	
		}

		[Test]
		public void Bind11BindScopingVariableInFilterInScope() {
	
					ImportData(@"bind/data.ttl");
		
		
			var result = ExecuteQuery(@"bind/bind11.rq");
			CheckResult(result, @"bind/bind11.srx", false);

	
		}

		#endregion

		
	}
}