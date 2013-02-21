using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace BrightstarDB.Tests.Sparql11TestSuite {
    [TestClass]
	public partial class DeleteData : SparqlTest {

        public DeleteData() : base()
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
		public void SimpleDeleteData1() {
				ImportData(@"delete-data/delete-pre-01.ttl");
				ExecuteUpdate(@"delete-data/delete-data-01.ru");
					ValidateUnamedGraph(@"delete-data/delete-post-01s.ttl");
			
		}

		[TestMethod]
		public void SimpleDeleteData2() {
				ImportGraph(@"delete-data/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
				ExecuteUpdate(@"delete-data/delete-data-02.ru");
					ValidateGraph(@"delete-data/delete-post-01s.ttl", new Uri(@"http://example.org/g1"));
			
		}

		[TestMethod]
		public void SimpleDeleteData3() {
				ImportData(@"delete-data/delete-pre-01.ttl");
				ExecuteUpdate(@"delete-data/delete-data-03.ru");
					ValidateUnamedGraph(@"delete-data/delete-post-01f.ttl");
			
		}

		[TestMethod]
		public void SimpleDeleteData4() {
				ImportGraph(@"delete-data/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
				ExecuteUpdate(@"delete-data/delete-data-04.ru");
					ValidateGraph(@"delete-data/delete-post-01f.ttl", new Uri(@"http://example.org/g1"));
			
		}

		[TestMethod]
		public void GraphSpecificDeleteData1() {
				ImportData(@"delete-data/delete-pre-01.ttl");
					ImportGraph(@"delete-data/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
					ImportGraph(@"delete-data/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
				ExecuteUpdate(@"delete-data/delete-data-05.ru");
					ValidateUnamedGraph(@"delete-data/delete-post-01s.ttl");
					ValidateGraph(@"delete-data/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));
					ValidateGraph(@"delete-data/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));
			
		}

		[TestMethod]
		public void GraphSpecificDeleteData2() {
				ImportData(@"delete-data/delete-pre-01.ttl");
					ImportGraph(@"delete-data/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
					ImportGraph(@"delete-data/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
				ExecuteUpdate(@"delete-data/delete-data-06.ru");
					ValidateUnamedGraph(@"delete-data/delete-post-01f.ttl");
					ValidateGraph(@"delete-data/delete-post-02s.ttl", new Uri(@"http://example.org/g2"));
					ValidateGraph(@"delete-data/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));
			
		}

		#endregion

		
	}
}