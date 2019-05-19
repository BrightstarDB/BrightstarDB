using NUnit.Framework;
using System;

namespace BrightstarDB.Tests.Sparql11TestSuite {
    
	public partial class Clear : SparqlTest {

        public Clear() : base()
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
		public void ClearDefault() {
				ImportData(@"clear/clear-default.ttl");
					ImportGraph(@"clear/clear-g1.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"clear/clear-g2.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"clear/clear-default-01.ru");
					ValidateUnamedGraph(@"clear/empty.ttl");
					ValidateGraph(@"clear/clear-g1.ttl", new Uri(@"http://example.org/g1"));
					ValidateGraph(@"clear/clear-g2.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[Test]
		public void ClearGraph() {
				ImportData(@"clear/clear-default.ttl");
					ImportGraph(@"clear/clear-g1.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"clear/clear-g2.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"clear/clear-graph-01.ru");
					ValidateUnamedGraph(@"clear/clear-default.ttl");
					ValidateGraph(@"clear/empty.ttl", new Uri(@"http://example.org/g1"));
					ValidateGraph(@"clear/clear-g2.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[Test]
		public void ClearNamed() {
				ImportData(@"clear/clear-default.ttl");
					ImportGraph(@"clear/clear-g1.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"clear/clear-g2.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"clear/clear-named-01.ru");
					ValidateUnamedGraph(@"clear/clear-default.ttl");
					ValidateGraph(@"clear/empty.ttl", new Uri(@"http://example.org/g1"));
					ValidateGraph(@"clear/empty.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[Test]
		public void ClearAll() {
				ImportData(@"clear/clear-default.ttl");
					ImportGraph(@"clear/clear-g1.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"clear/clear-g2.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"clear/clear-all-01.ru");
					ValidateUnamedGraph(@"clear/empty.ttl");
					ValidateGraph(@"clear/empty.ttl", new Uri(@"http://example.org/g1"));
					ValidateGraph(@"clear/empty.ttl", new Uri(@"http://example.org/g2"));
			
		}

		#endregion

		
	}
}