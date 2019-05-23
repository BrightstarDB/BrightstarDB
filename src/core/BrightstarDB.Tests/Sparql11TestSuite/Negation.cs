using NUnit.Framework;

namespace BrightstarDB.Tests.Sparql11TestSuite {
	public partial class Negation : SparqlTest {

        public Negation() : base()
        {
            
        }

		[OneTimeSetUp]
		public void SetUp()
		{
			CreateStore();
		    
		}

        [OneTimeTearDown]
        public void TearDown()
        {
			DeleteStore();
            
        }

		#region Test Methods

		[Test]
		public void SubsetsByExclusionNotExists() {
	
					ImportData(@"negation/subsetByExcl.ttl");
		
		
			var result = ExecuteQuery(@"negation/subsetByExcl01.rq");
			CheckResult(result, @"negation/subsetByExcl01.srx", false);

	
		}

		[Test]
		public void SubsetsByExclusionMinus() {
	
					ImportData(@"negation/subsetByExcl.ttl");
		
		
			var result = ExecuteQuery(@"negation/subsetByExcl02.rq");
			CheckResult(result, @"negation/subsetByExcl02.srx", false);

	
		}

		[Test]
		public void MedicalTemporalProximityByExclusionNotExists() {
	
					ImportData(@"negation/temporalProximity01.ttl");
		
		
			var result = ExecuteQuery(@"negation/temporalProximity01.rq");
			CheckResult(result, @"negation/temporalProximity01.srx", false);

	
		}

		[Test]
        [Ignore("Test fails in dotNetRDF")] // Not working in dotNetRdf
		public void MedicalTemporalProximityByExclusionMinus() {
	
					ImportData(@"negation/temporalProximity02.ttl");
		
		
			var result = ExecuteQuery(@"negation/temporalProximity02.rq");
			CheckResult(result, @"negation/temporalProximity02.srx", false);

	
		}

		[Test]
		public void CalculateWhichSetsAreSubsetsOfOthersIncludeASubsetofA() {
	
					ImportData(@"negation/set-data.ttl");
		
		
			var result = ExecuteQuery(@"negation/subset-01.rq");
			CheckResult(result, @"negation/subset-01.srx", false);

	
		}

		[Test]
		public void CalculateWhichSetsAreSubsetsOfOthersExcludeASubsetofA() {
	
					ImportData(@"negation/set-data.ttl");
		
		
			var result = ExecuteQuery(@"negation/subset-02.rq");
			CheckResult(result, @"negation/subset-02.srx", false);

	
		}

		[Test]
		public void CalculateWhichSetsHaveTheSameElements() {
	
					ImportData(@"negation/set-data.ttl");
		
		
			var result = ExecuteQuery(@"negation/set-equals-1.rq");
			CheckResult(result, @"negation/set-equals-1.srx", false);

	
		}

		[Test]
		public void CalculateProperSubset() {
	
					ImportData(@"negation/set-data.ttl");
		
		
			var result = ExecuteQuery(@"negation/subset-03.rq");
			CheckResult(result, @"negation/subset-03.srx", false);

	
		}

		[Test]
		public void PositiveExists1() {
	
					ImportData(@"negation/set-data.ttl");
		
		
			var result = ExecuteQuery(@"negation/exists-01.rq");
			CheckResult(result, @"negation/exists-01.srx", false);

	
		}

		[Test]
		public void PositiveExists2() {
	
					ImportData(@"negation/set-data.ttl");
		
		
			var result = ExecuteQuery(@"negation/exists-02.rq");
			CheckResult(result, @"negation/exists-02.srx", false);

	
		}

		#endregion

		
	}
}