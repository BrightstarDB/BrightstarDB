using NUnit.Framework;

namespace BrightstarDB.Tests.Sparql11TestSuite {

	public partial class DeleteInsert : SparqlTest {

        public DeleteInsert() : base()
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
		public void DeleteInsert1() {
				ImportData(@"delete-insert/delete-insert-pre-01.ttl");
				ExecuteUpdate(@"delete-insert/delete-insert-01.ru");
					ValidateUnamedGraph(@"delete-insert/delete-insert-post-01.ttl");
			
		}

		[Test]
		public void DeleteInsert1b() {
				ImportData(@"delete-insert/delete-insert-pre-01.ttl");
				ExecuteUpdate(@"delete-insert/delete-insert-01b.ru");
					ValidateUnamedGraph(@"delete-insert/delete-insert-post-01b.ttl");
			
		}

		[Test]
		public void DeleteInsert1c() {
				ImportData(@"delete-insert/delete-insert-pre-01.ttl");
				ExecuteUpdate(@"delete-insert/delete-insert-01c.ru");
					ValidateUnamedGraph(@"delete-insert/delete-insert-post-01b.ttl");
			
		}

		[Test]
		public void DeleteInsert2() {
				ImportData(@"delete-insert/delete-insert-pre-01.ttl");
				ExecuteUpdate(@"delete-insert/delete-insert-02.ru");
					ValidateUnamedGraph(@"delete-insert/delete-insert-post-02.ttl");
			
		}

		[Test]
		public void DeleteInsert4() {
				ImportData(@"delete-insert/delete-insert-pre-01.ttl");
				ExecuteUpdate(@"delete-insert/delete-insert-04.ru");
					ValidateUnamedGraph(@"delete-insert/delete-insert-post-02.ttl");
			
		}

		[Test]
		public void DeleteInsert4b() {
				ImportData(@"delete-insert/delete-insert-pre-01.ttl");
				ExecuteUpdate(@"delete-insert/delete-insert-04b.ru");
					ValidateUnamedGraph(@"delete-insert/delete-insert-post-02.ttl");
			
		}

		[Test]
		public void DeleteInsert5b() {
				ImportData(@"delete-insert/delete-insert-pre-01.ttl");
				ExecuteUpdate(@"delete-insert/delete-insert-05b.ru");
					ValidateUnamedGraph(@"delete-insert/delete-insert-post-05.ttl");
			
		}

		[Test]
		public void DeleteInsert6b() {
				ImportData(@"delete-insert/delete-insert-pre-06.ttl");
				ExecuteUpdate(@"delete-insert/delete-insert-05b.ru");
					ValidateUnamedGraph(@"delete-insert/delete-insert-pre-06.ttl");
			
		}

		#endregion

		
	}
}