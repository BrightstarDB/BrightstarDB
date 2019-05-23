using NUnit.Framework;
using System;

namespace BrightstarDB.Tests.Sparql11TestSuite {

	public partial class DeleteWhere : SparqlTest {

        public DeleteWhere() : base()
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
		public void SimpleDeleteWhere1() {
				ImportData(@"delete-where/delete-pre-01.ttl");
				ExecuteUpdate(@"delete-where/delete-where-01.ru");
					ValidateUnamedGraph(@"delete-where/delete-post-01s.ttl");
			
		}

		[Test]
		public void SimpleDeleteWhere2() {
				ImportGraph(@"delete-where/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
				ExecuteUpdate(@"delete-where/delete-where-02.ru");
					ValidateGraph(@"delete-where/delete-post-01s.ttl", new Uri(@"http://example.org/g1"));
			
		}

		[Test]
		public void SimpleDeleteWhere3() {
				ImportData(@"delete-where/delete-pre-01.ttl");
				ExecuteUpdate(@"delete-where/delete-where-03.ru");
					ValidateUnamedGraph(@"delete-where/delete-post-01f.ttl");
			
		}

		[Test]
		public void SimpleDeleteWhere4() {
				ImportGraph(@"delete-where/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
				ExecuteUpdate(@"delete-where/delete-where-04.ru");
					ValidateGraph(@"delete-where/delete-post-01f.ttl", new Uri(@"http://example.org/g1"));
			
		}

		[Test]
		public void GraphSpecificDeleteWhere1() {
				ImportData(@"delete-where/delete-pre-01.ttl");
					ImportGraph(@"delete-where/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
					ImportGraph(@"delete-where/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
				ExecuteUpdate(@"delete-where/delete-where-05.ru");
					ValidateUnamedGraph(@"delete-where/delete-post-01s.ttl");
					ValidateGraph(@"delete-where/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));
					ValidateGraph(@"delete-where/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));
			
		}

		[Test]
		public void GraphSpecificDeleteWhere2() {
				ImportData(@"delete-where/delete-pre-01.ttl");
					ImportGraph(@"delete-where/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
					ImportGraph(@"delete-where/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
				ExecuteUpdate(@"delete-where/delete-where-06.ru");
					ValidateUnamedGraph(@"delete-where/delete-post-01f.ttl");
					ValidateGraph(@"delete-where/delete-post-02s.ttl", new Uri(@"http://example.org/g2"));
					ValidateGraph(@"delete-where/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));
			
		}

		#endregion

		
	}
}