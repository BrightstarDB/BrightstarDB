using NUnit.Framework;

namespace BrightstarDB.Tests.Sparql11TestSuite {

	public partial class PropertyPath : SparqlTest {

        public PropertyPath() : base()
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
		public void Pp01SimplePath() {
	
					ImportData(@"property-path/pp01.ttl");
		
		
			var result = ExecuteQuery(@"property-path/pp01.rq");
			CheckResult(result, @"property-path/pp01.srx", false);

	
		}

		[Test]
		public void Pp02StarPath() {
	
					ImportData(@"property-path/pp01.ttl");
		
		
			var result = ExecuteQuery(@"property-path/pp02.rq");
			CheckResult(result, @"property-path/pp02.srx", false);

	
		}

		[Test]
		public void Pp03SimplePathWithLoop() {
	
					ImportData(@"property-path/pp03.ttl");
		
		
			var result = ExecuteQuery(@"property-path/pp03.rq");
			CheckResult(result, @"property-path/pp03.srx", false);

	
		}

		[Test]
		public void Pp04VariableLengthPathWithLoop() {
	
					ImportData(@"property-path/pp03.ttl");
		
		
			var result = ExecuteQuery(@"property-path/pp04.rq");
			CheckResult(result, @"property-path/pp04.srx", false);

	
		}

		[Test]
		public void Pp05ZeroLengthPath() {
	
					ImportData(@"property-path/pp05.ttl");
		
		
			var result = ExecuteQuery(@"property-path/pp05.rq");
			CheckResult(result, @"property-path/pp05.srx", false);

	
		}

		[Test]
		public void Pp08ReversePath() {
	
					ImportData(@"property-path/pp08.ttl");
		
		
			var result = ExecuteQuery(@"property-path/pp08.rq");
			CheckResult(result, @"property-path/pp08.srx", false);

	
		}

		[Test]
		public void Pp09ReverseSequencePath() {
	
					ImportData(@"property-path/pp09.ttl");
		
		
			var result = ExecuteQuery(@"property-path/pp09.rq");
			CheckResult(result, @"property-path/pp09.srx", false);

	
		}

		[Test]
		public void Pp10PathWithNegation() {
	
					ImportData(@"property-path/pp10.ttl");
		
		
			var result = ExecuteQuery(@"property-path/pp10.rq");
			CheckResult(result, @"property-path/pp10.srx", false);

	
		}

		[Test]
		public void Pp11SimplePathAndTwoPathsToSameTargetNode() {
	
					ImportData(@"property-path/pp11.ttl");
		
		
			var result = ExecuteQuery(@"property-path/pp11.rq");
			CheckResult(result, @"property-path/pp11.srx", false);

	
		}

		[Test]
		public void Pp12VariableLengthPathAndTwoPathsToSameTargetNode() {
	
					ImportData(@"property-path/pp11.ttl");
		
		
			var result = ExecuteQuery(@"property-path/pp12.rq");
			CheckResult(result, @"property-path/pp12.srx", false);

	
		}

		[Test]
		public void Pp13ZeroLengthPathsWithLiterals() {
	
					ImportData(@"property-path/pp13.ttl");
		
		
			var result = ExecuteQuery(@"property-path/pp13.rq");
			CheckResult(result, @"property-path/pp13.srx", false);

	
		}

		[Test]
		public void Pp14StarPathOverFoafKnows() {
	
					ImportData(@"property-path/pp14.ttl");
		
		
			var result = ExecuteQuery(@"property-path/pp14.rq");
			CheckResult(result, @"property-path/pp14.srx", false);

	
		}

		[Test]
		public void Pp15ZeroLengthPathsOnAnEmptyGraph() {
	
					ImportData(@"property-path/empty.ttl");
		
		
			var result = ExecuteQuery(@"property-path/pp15.rq");
			CheckResult(result, @"property-path/pp15.srx", false);

	
		}

		[Test]
		public void Pp16DuplicatePathsAndCyclesThroughFoafKnows_Asterix_() {
	
					ImportData(@"property-path/pp16.ttl");
		
		
			var result = ExecuteQuery(@"property-path/pp14.rq");
			CheckResult(result, @"property-path/pp16.srx", false);

	
		}

		[Test]
		public void Pp20DiamondP2() {
	
					ImportData(@"property-path/data-diamond.ttl");
		
		
			var result = ExecuteQuery(@"property-path/path-2-1.rq");
			CheckResult(result, @"property-path/diamond-1.srx", false);

	
		}

		[Test]
		public void Pp21DiamondP_Plus_() {
	
					ImportData(@"property-path/data-diamond.ttl");
		
		
			var result = ExecuteQuery(@"property-path/path-2-2.rq");
			CheckResult(result, @"property-path/diamond-2.srx", false);

	
		}

		[Test]
		public void Pp22DiamondWithTailP3() {
	
					ImportData(@"property-path/data-diamond-tail.ttl");
		
		
			var result = ExecuteQuery(@"property-path/path-2-3.rq");
			CheckResult(result, @"property-path/diamond-tail-1.srx", false);

	
		}

		[Test]
		public void Pp23DiamondWithTailP_Plus_() {
	
					ImportData(@"property-path/data-diamond-tail.ttl");
		
		
			var result = ExecuteQuery(@"property-path/path-2-2.rq");
			CheckResult(result, @"property-path/diamond-tail-2.srx", false);

	
		}

		[Test]
		public void Pp24DiamondWithLoopP2() {
	
					ImportData(@"property-path/data-diamond-loop.ttl");
		
		
			var result = ExecuteQuery(@"property-path/path-2-1.rq");
			CheckResult(result, @"property-path/diamond-loop-1.srx", false);

	
		}

		[Test]
		public void Pp25DiamondWithLoopP_Plus_() {
	
					ImportData(@"property-path/data-diamond-loop.ttl");
		
		
			var result = ExecuteQuery(@"property-path/path-2-2.rq");
			CheckResult(result, @"property-path/diamond-loop-2.srx", false);

	
		}

		[Test]
		public void Pp26DiamondWithLoopP24() {
	
					ImportData(@"property-path/data-diamond-loop.ttl");
		
		
			var result = ExecuteQuery(@"property-path/path-3-1.rq");
			CheckResult(result, @"property-path/diamond-loop-3.srx", false);

	
		}

		[Test]
		public void Pp27DiamondWithLoopP3() {
	
					ImportData(@"property-path/data-diamond-loop.ttl");
		
		
			var result = ExecuteQuery(@"property-path/path-3-2.rq");
			CheckResult(result, @"property-path/diamond-loop-4.srx", false);

	
		}

		[Test]
		public void Pp28DiamondWithLoopPP_QuestionMark_() {
	
					ImportData(@"property-path/data-diamond-loop.ttl");
		
		
			var result = ExecuteQuery(@"property-path/path-3-3.rq");
			CheckResult(result, @"property-path/diamond-loop-5.srx", false);

	
		}

		[Test]
		public void Pp29DiamondWithLoopP2() {
	
					ImportData(@"property-path/data-diamond-loop.ttl");
		
		
			var result = ExecuteQuery(@"property-path/path-3-4.rq");
			CheckResult(result, @"property-path/diamond-loop-6.srx", false);

	
		}

		[Test]
		public void Pp30OperatorPrecedence1() {
	
					ImportData(@"property-path/path-p1.ttl");
		
		
			var result = ExecuteQuery(@"property-path/path-p1.rq");
			CheckResult(result, @"property-path/path-p1.srx", false);

	
		}

		[Test]
		public void Pp31OperatorPrecedence2() {
	
					ImportData(@"property-path/path-p1.ttl");
		
		
			var result = ExecuteQuery(@"property-path/path-p2.rq");
			CheckResult(result, @"property-path/path-p2.srx", false);

	
		}

		[Test]
		public void Pp32OperatorPrecedence3() {
	
					ImportData(@"property-path/path-p3.ttl");
		
		
			var result = ExecuteQuery(@"property-path/path-p3.rq");
			CheckResult(result, @"property-path/path-p3.srx", false);

	
		}

		[Test]
		public void Pp33OperatorPrecedence4() {
	
					ImportData(@"property-path/path-p3.ttl");
		
		
			var result = ExecuteQuery(@"property-path/path-p4.rq");
			CheckResult(result, @"property-path/path-p4.srx", false);

	
		}

		#endregion

		
	}
}