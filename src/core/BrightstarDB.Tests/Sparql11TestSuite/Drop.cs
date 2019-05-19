using NUnit.Framework;
using System;

namespace BrightstarDB.Tests.Sparql11TestSuite {

	public partial class Drop : SparqlTest {

        public Drop() : base()
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
		public void DropDefault() {
				ImportData(@"drop/drop-default.ttl");
					ImportGraph(@"drop/drop-g1.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"drop/drop-g2.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"drop/drop-default-01.ru");
					ValidateGraph(@"drop/drop-g1.ttl", new Uri(@"http://example.org/g1"));
					ValidateGraph(@"drop/drop-g2.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[Test]
		public void DropGraph() {
				ImportData(@"drop/drop-default.ttl");
					ImportGraph(@"drop/drop-g1.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"drop/drop-g2.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"drop/drop-graph-01.ru");
					ValidateUnamedGraph(@"drop/drop-default.ttl");
					ValidateGraph(@"drop/drop-g2.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[Test]
		public void DropNamed() {
				ImportData(@"drop/drop-default.ttl");
					ImportGraph(@"drop/drop-g1.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"drop/drop-g2.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"drop/drop-named-01.ru");
					ValidateUnamedGraph(@"drop/drop-default.ttl");
			
		}

		[Test]
		public void DropAll() {
				ImportData(@"drop/drop-default.ttl");
					ImportGraph(@"drop/drop-g1.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"drop/drop-g2.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"drop/drop-all-01.ru");
			
		}

		#endregion

		
	}
}