using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.Sparql11TestSuite {
    [TestClass]
	public partial class DeleteInsert : SparqlTest {

        public DeleteInsert() : base()
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
		public void DeleteInsert1() {
				ImportData(@"delete-insert/delete-insert-pre-01.ttl");
				ExecuteUpdate(@"delete-insert/delete-insert-01.ru");
					ValidateUnamedGraph(@"delete-insert/delete-insert-post-01.ttl");
			
		}

		[TestMethod]
		public void DeleteInsert1b() {
				ImportData(@"delete-insert/delete-insert-pre-01.ttl");
				ExecuteUpdate(@"delete-insert/delete-insert-01b.ru");
					ValidateUnamedGraph(@"delete-insert/delete-insert-post-01b.ttl");
			
		}

		[TestMethod]
		public void DeleteInsert1c() {
				ImportData(@"delete-insert/delete-insert-pre-01.ttl");
				ExecuteUpdate(@"delete-insert/delete-insert-01c.ru");
					ValidateUnamedGraph(@"delete-insert/delete-insert-post-01b.ttl");
			
		}

		[TestMethod]
		public void DeleteInsert2() {
				ImportData(@"delete-insert/delete-insert-pre-01.ttl");
				ExecuteUpdate(@"delete-insert/delete-insert-02.ru");
					ValidateUnamedGraph(@"delete-insert/delete-insert-post-02.ttl");
			
		}

		[TestMethod]
		public void DeleteInsert4() {
				ImportData(@"delete-insert/delete-insert-pre-01.ttl");
				ExecuteUpdate(@"delete-insert/delete-insert-04.ru");
					ValidateUnamedGraph(@"delete-insert/delete-insert-post-02.ttl");
			
		}

		[TestMethod]
		public void DeleteInsert4b() {
				ImportData(@"delete-insert/delete-insert-pre-01.ttl");
				ExecuteUpdate(@"delete-insert/delete-insert-04b.ru");
					ValidateUnamedGraph(@"delete-insert/delete-insert-post-02.ttl");
			
		}

		[TestMethod]
		public void DeleteInsert5b() {
				ImportData(@"delete-insert/delete-insert-pre-01.ttl");
				ExecuteUpdate(@"delete-insert/delete-insert-05b.ru");
					ValidateUnamedGraph(@"delete-insert/delete-insert-post-05.ttl");
			
		}

		[TestMethod]
		public void DeleteInsert6b() {
				ImportData(@"delete-insert/delete-insert-pre-06.ttl");
				ExecuteUpdate(@"delete-insert/delete-insert-05b.ru");
					ValidateUnamedGraph(@"delete-insert/delete-insert-pre-06.ttl");
			
		}

		#endregion

		
	}
}