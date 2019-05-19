using NUnit.Framework;
using System;

namespace BrightstarDB.Tests.Sparql11TestSuite {
    
	public partial class BasicUpdate : SparqlTest {

        public BasicUpdate() : base()
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
		public void SimpleInsertData1() {
			ExecuteUpdate(@"basic-update/insert-data-spo1.ru");
					ValidateUnamedGraph(@"basic-update/spo.ttl");
			
		}

		[Test]
		public void SimpleInsertDataNamed1() {
			ExecuteUpdate(@"basic-update/insert-data-named1.ru");
					ValidateGraph(@"basic-update/spo.ttl", new Uri(@"http://example.org/g1"));
			
		}

		[Test]
		public void SimpleInsertDataNamed2() {
				ImportGraph(@"basic-update/spo.ttl", new Uri(@"http://example.org/g1"));
				ExecuteUpdate(@"basic-update/insert-data-named2.ru");
					ValidateGraph(@"basic-update/spo2.ttl", new Uri(@"http://example.org/g1"));
			
		}

		[Test]
		public void SimpleInsertDataNamed3() {
				ImportGraph(@"basic-update/spo.ttl", new Uri(@"http://example.org/g1"));
				ExecuteUpdate(@"basic-update/insert-data-named1.ru");
					ValidateGraph(@"basic-update/spo.ttl", new Uri(@"http://example.org/g1"));
			
		}

		[Test]
		public void Insert01() {
				ImportData(@"basic-update/insert-01-pre.ttl");
				ExecuteUpdate(@"basic-update/insert-01.ru");
					ValidateUnamedGraph(@"basic-update/insert-01-post.ttl");
			
		}

		[Test]
		public void Insert02() {
				ImportData(@"basic-update/insert-02-pre.ttl");
				ExecuteUpdate(@"basic-update/insert-02.ru");
					ValidateUnamedGraph(@"basic-update/insert-02-post.ttl");
					ValidateGraph(@"basic-update/insert-02-g1-post.ttl", new Uri(@"http://example.org/g1"));
			
		}

		[Test]
		public void Insert03() {
				ImportData(@"basic-update/insert-03-pre.ttl");
					ImportGraph(@"basic-update/insert-03-g1-pre.ttl", new Uri(@"http://example.org/g1"));
				ExecuteUpdate(@"basic-update/insert-03.ru");
					ValidateUnamedGraph(@"basic-update/insert-03-post.ttl");
					ValidateGraph(@"basic-update/insert-03-g1-post.ttl", new Uri(@"http://example.org/g1"));
			
		}

		[Test]
		public void Insert04() {
				ImportData(@"basic-update/insert-04-pre.ttl");
					ImportGraph(@"basic-update/insert-04-g1-pre.ttl", new Uri(@"http://example.org/g1"));
				ExecuteUpdate(@"basic-update/insert-04.ru");
					ValidateUnamedGraph(@"basic-update/insert-04-post.ttl");
					ValidateGraph(@"basic-update/insert-04-g1-post.ttl", new Uri(@"http://example.org/g1"));
			
		}

		[Test]
		public void InsertUsing01() {
				ImportData(@"basic-update/insert-using-01-pre.ttl");
					ImportGraph(@"basic-update/insert-using-01-g1-pre.ttl", new Uri(@"http://example.org/g1"));
					ImportGraph(@"basic-update/insert-using-01-g2-pre.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"basic-update/insert-using-01.ru");
					ValidateUnamedGraph(@"basic-update/insert-using-01-post.ttl");
					ValidateGraph(@"basic-update/insert-using-01-g1-post.ttl", new Uri(@"http://example.org/g1"));
					ValidateGraph(@"basic-update/insert-using-01-g2-post.ttl", new Uri(@"http://example.org/g2"));
			
		}

		#endregion

		
	}
}