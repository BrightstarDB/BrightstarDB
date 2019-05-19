using NUnit.Framework;

namespace BrightstarDB.Tests.Sparql11TestSuite {
    
	public partial class Aggregates : SparqlTest {

        public Aggregates() : base()
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
		public void Count1() {
	
					ImportData(@"aggregates/agg01.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg01.rq");
			CheckResult(result, @"aggregates/agg01.srx", false);

	
		}

		[Test]
		public void Count2() {
	
					ImportData(@"aggregates/agg01.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg02.rq");
			CheckResult(result, @"aggregates/agg02.srx", false);

	
		}

		[Test]
		public void Count3() {
	
					ImportData(@"aggregates/agg01.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg03.rq");
			CheckResult(result, @"aggregates/agg03.srx", false);

	
		}

		[Test]
		public void Count4() {
	
					ImportData(@"aggregates/agg01.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg04.rq");
			CheckResult(result, @"aggregates/agg04.srx", false);

	
		}

		[Test]
		public void Count5() {
	
					ImportData(@"aggregates/agg01.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg05.rq");
			CheckResult(result, @"aggregates/agg05.srx", false);

	
		}

		[Test]
		public void Count6() {
	
					ImportData(@"aggregates/agg01.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg06.rq");
			CheckResult(result, @"aggregates/agg06.srx", false);

	
		}

		[Test]
		public void Count7() {
	
					ImportData(@"aggregates/agg01.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg07.rq");
			CheckResult(result, @"aggregates/agg07.srx", false);

	
		}

		[Test]
		public void Count8b() {
	
					ImportData(@"aggregates/agg08.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg08b.rq");
			CheckResult(result, @"aggregates/agg08b.srx", false);

	
		}

		[Test]
		public void ErrorInAvg() {
	
					ImportData(@"aggregates/agg-err-01.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg-err-01.rq");
			CheckResult(result, @"aggregates/agg-err-01.srx", false);

	
		}

		[Test]
        [Ignore("Not currently working in dotNetRdf")]
		public void ProtectFromErrorInAvg() {
	
					ImportData(@"aggregates/agg-err-02.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg-err-02.rq");
			CheckResult(result, @"aggregates/agg-err-02.srx", false);

	
		}

		[Test]
		public void Group_concat1() {
	
					ImportData(@"aggregates/agg-groupconcat-1.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg-groupconcat-1.rq");
			CheckResult(result, @"aggregates/agg-groupconcat-1.srx", false);

	
		}

		[Test]
		public void Group_concat2() {
	
					ImportData(@"aggregates/agg-groupconcat-1.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg-groupconcat-2.rq");
			CheckResult(result, @"aggregates/agg-groupconcat-2.srx", false);

	
		}

		[Test]
		public void Group_concatWithSeparator() {
	
					ImportData(@"aggregates/agg-groupconcat-1.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg-groupconcat-3.rq");
			CheckResult(result, @"aggregates/agg-groupconcat-3.srx", false);

	
		}

		[Test]
		public void Avg() {
	
					ImportData(@"aggregates/agg-numeric.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg-avg-01.rq");
			CheckResult(result, @"aggregates/agg-avg-01.srx", false);

	
		}

		[Test]
        [Ignore("Not working in dotNetRdf")]
		public void AvgWithGroupBy() {
	
					ImportData(@"aggregates/agg-numeric2.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg-avg-02.rq");
			CheckResult(result, @"aggregates/agg-avg-02.srx", false);

	
		}

		[Test]
		public void Min() {
	
					ImportData(@"aggregates/agg-numeric.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg-min-01.rq");
			CheckResult(result, @"aggregates/agg-min-01.srx", false);

	
		}

		[Test]
        [Ignore("Not working in dotNetRdf")]
		public void MinWithGroupBy() {
	
					ImportData(@"aggregates/agg-numeric.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg-min-02.rq");
			CheckResult(result, @"aggregates/agg-min-02.srx", false);

	
		}

		[Test]
		public void Max() {
	
					ImportData(@"aggregates/agg-numeric.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg-max-01.rq");
			CheckResult(result, @"aggregates/agg-max-01.srx", false);

	
		}

		[Test]
		public void MaxWithGroupBy() {
	
					ImportData(@"aggregates/agg-numeric.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg-max-02.rq");
			CheckResult(result, @"aggregates/agg-max-02.srx", false);

	
		}

		[Test]
		public void Sum() {
	
					ImportData(@"aggregates/agg-numeric.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg-sum-01.rq");
			CheckResult(result, @"aggregates/agg-sum-01.srx", false);

	
		}

		[Test]
        [Ignore("Not working in dotNetRdf")]
		public void SumWithGroupBy() {
	
					ImportData(@"aggregates/agg-numeric2.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg-sum-02.rq");
			CheckResult(result, @"aggregates/agg-sum-02.srx", false);

	
		}

		[Test]
		public void Sample() {
	
					ImportData(@"aggregates/agg-numeric.ttl");
		
		
			var result = ExecuteQuery(@"aggregates/agg-sample-01.rq");
			CheckResult(result, @"aggregates/agg-sample-01.srx", false);

	
		}

		#endregion

		
	}
}