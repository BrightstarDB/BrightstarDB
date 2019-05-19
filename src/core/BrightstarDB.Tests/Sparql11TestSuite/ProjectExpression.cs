using NUnit.Framework;

namespace BrightstarDB.Tests.Sparql11TestSuite {

	public partial class ProjectExpression : SparqlTest {

        public ProjectExpression() : base()
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
		public void ExpressionIsEquality() {
	
					ImportData(@"project-expression/projexp01.ttl");
		
		
			var result = ExecuteQuery(@"project-expression/projexp01.rq");
			CheckResult(result, @"project-expression/projexp01.srx", false);

	
		}

		[Test]
		public void ExpressionRaiseAnError() {
	
					ImportData(@"project-expression/projexp02.ttl");
		
		
			var result = ExecuteQuery(@"project-expression/projexp02.rq");
			CheckResult(result, @"project-expression/projexp02.srx", false);

	
		}

		[Test]
		public void ReuseAProjectExpressionVariableInSelect() {
	
					ImportData(@"project-expression/projexp03.ttl");
		
		
			var result = ExecuteQuery(@"project-expression/projexp03.rq");
			CheckResult(result, @"project-expression/projexp03.srx", false);

	
		}

		[Test]
		public void ReuseAProjectExpressionVariableInOrderBy() {
	
					ImportData(@"project-expression/projexp04.ttl");
		
		
			var result = ExecuteQuery(@"project-expression/projexp04.rq");
			CheckResult(result, @"project-expression/projexp04.srx", false);

	
		}

		[Test]
		public void ExpressionMayReturnNoValue() {
	
					ImportData(@"project-expression/projexp05.ttl");
		
		
			var result = ExecuteQuery(@"project-expression/projexp05.rq");
			CheckResult(result, @"project-expression/projexp05.srx", false);

	
		}

		[Test]
		public void ExpressionHasUndefinedVariable() {
	
					ImportData(@"project-expression/projexp06.ttl");
		
		
			var result = ExecuteQuery(@"project-expression/projexp06.rq");
			CheckResult(result, @"project-expression/projexp06.srx", false);

	
		}

		[Test]
		public void ExpressionHasVariableThatMayBeUnbound() {
	
					ImportData(@"project-expression/projexp07.ttl");
		
		
			var result = ExecuteQuery(@"project-expression/projexp07.rq");
			CheckResult(result, @"project-expression/projexp07.srx", false);

	
		}

		#endregion

		
	}
}