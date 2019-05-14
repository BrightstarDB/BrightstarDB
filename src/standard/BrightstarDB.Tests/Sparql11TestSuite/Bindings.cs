using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.Sparql11TestSuite {
    [TestClass]
	public partial class Bindings : SparqlTest {

        public Bindings() : base()
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
		public void Unamedtest_1() {
	
					ImportData(@"bindings/data01.ttl");
		
		
			var result = ExecuteQuery(@"bindings/bindings01.rq");
			CheckResult(result, @"bindings/bindings01.srx", false);

	
		}

		[TestMethod]
		public void Unamedtest_2() {
	
					ImportData(@"bindings/data02.ttl");
		
		
			var result = ExecuteQuery(@"bindings/bindings02.rq");
			CheckResult(result, @"bindings/bindings02.srx", false);

	
		}

		[TestMethod]
		public void Unamedtest_3() {
	
					ImportData(@"bindings/data03.ttl");
		
		
			var result = ExecuteQuery(@"bindings/bindings03.rq");
			CheckResult(result, @"bindings/bindings03.srx", false);

	
		}

		[TestMethod]
		public void Unamedtest_4() {
	
					ImportData(@"bindings/data04.ttl");
		
		
			var result = ExecuteQuery(@"bindings/bindings04.rq");
			CheckResult(result, @"bindings/bindings04.srx", false);

	
		}

		[TestMethod]
		public void Unamedtest_5() {
	
					ImportData(@"bindings/data05.ttl");
		
		
			var result = ExecuteQuery(@"bindings/bindings05.rq");
			CheckResult(result, @"bindings/bindings05.srx", false);

	
		}

		[TestMethod]
		public void Unamedtest_6() {
	
					ImportData(@"bindings/data06.ttl");
		
		
			var result = ExecuteQuery(@"bindings/bindings06.rq");
			CheckResult(result, @"bindings/bindings06.srx", false);

	
		}

		[TestMethod]
		public void Unamedtest_7() {
	
					ImportData(@"bindings/data07.ttl");
		
		
			var result = ExecuteQuery(@"bindings/bindings07.rq");
			CheckResult(result, @"bindings/bindings07.srx", false);

	
		}

		[TestMethod]
		public void Unamedtest_8() {
	
					ImportData(@"bindings/data08.ttl");
		
		
			var result = ExecuteQuery(@"bindings/bindings08.rq");
			CheckResult(result, @"bindings/bindings08.srx", false);

	
		}

		#endregion

		
	}
}