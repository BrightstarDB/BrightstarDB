using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace BrightstarDB.Tests.Sparql11TestSuite {
    [TestClass]
	public partial class UpdateSilent : SparqlTest {

        public UpdateSilent() : base()
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
		public void LoadSilent() {
			ExecuteUpdate(@"update-silent/load-silent.ru");
			
		}

		[TestMethod]
		public void LoadSilentInto() {
			ExecuteUpdate(@"update-silent/load-silent-into.ru");
			
		}

		[TestMethod]
		public void ClearSilentGraphIri() {
				ImportData(@"update-silent/spo.ttl");
				ExecuteUpdate(@"update-silent/clear-silent.ru");
					ValidateUnamedGraph(@"update-silent/spo.ttl");
			
		}

		[TestMethod]
		public void ClearSilentDefault() {
			ExecuteUpdate(@"update-silent/clear-default-silent.ru");
			
		}

		[TestMethod]
		public void CreateSilentIri() {
				ImportGraph(@"update-silent/spo.ttl", new Uri(@"http://example.org/g1"));
				ExecuteUpdate(@"update-silent/create-silent.ru");
					ValidateGraph(@"update-silent/spo.ttl", new Uri(@"http://example.org/g1"));
			
		}

		[TestMethod]
		public void DropSilentGraphIri() {
				ImportData(@"update-silent/spo.ttl");
				ExecuteUpdate(@"update-silent/drop-silent.ru");
					ValidateUnamedGraph(@"update-silent/spo.ttl");
			
		}

		[TestMethod]
		public void DropSilentDefault() {
			ExecuteUpdate(@"update-silent/drop-default-silent.ru");
			
		}

		[TestMethod]
		public void CopySilent() {
				ImportGraph(@"update-silent/spo.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"update-silent/copy-silent.ru");
					ValidateGraph(@"update-silent/spo.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[TestMethod]
		public void CopySilentToDefault() {
			ExecuteUpdate(@"update-silent/copy-to-default-silent.ru");
			
		}

		[TestMethod]
		public void MoveSilent() {
				ImportGraph(@"update-silent/spo.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"update-silent/move-silent.ru");
					ValidateGraph(@"update-silent/spo.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[TestMethod]
		public void MoveSilentToDefault() {
			ExecuteUpdate(@"update-silent/move-to-default-silent.ru");
			
		}

		[TestMethod]
		public void AddSilent() {
				ImportGraph(@"update-silent/spo.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"update-silent/add-silent.ru");
					ValidateGraph(@"update-silent/spo.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[TestMethod]
		public void AddSilentToDefault() {
			ExecuteUpdate(@"update-silent/add-to-default-silent.ru");
			
		}

		#endregion

		
	}
}