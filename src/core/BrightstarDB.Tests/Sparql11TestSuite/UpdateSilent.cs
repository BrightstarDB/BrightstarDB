using System;
using NUnit.Framework;

namespace BrightstarDB.Tests.Sparql11TestSuite {
	public class UpdateSilent : SparqlTest {
        
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
		public void LoadSilent() {
			ExecuteUpdate(@"update-silent/load-silent.ru");
			
		}

		[Test]
		public void LoadSilentInto() {
			ExecuteUpdate(@"update-silent/load-silent-into.ru");
			
		}

		[Test]
		public void ClearSilentGraphIri() {
				ImportData(@"update-silent/spo.ttl");
				ExecuteUpdate(@"update-silent/clear-silent.ru");
					ValidateUnamedGraph(@"update-silent/spo.ttl");
			
		}

		[Test]
		public void ClearSilentDefault() {
			ExecuteUpdate(@"update-silent/clear-default-silent.ru");
			
		}

		[Test]
		public void CreateSilentIri() {
				ImportGraph(@"update-silent/spo.ttl", new Uri(@"http://example.org/g1"));
				ExecuteUpdate(@"update-silent/create-silent.ru");
					ValidateGraph(@"update-silent/spo.ttl", new Uri(@"http://example.org/g1"));
			
		}

		[Test]
		public void DropSilentGraphIri() {
				ImportData(@"update-silent/spo.ttl");
				ExecuteUpdate(@"update-silent/drop-silent.ru");
					ValidateUnamedGraph(@"update-silent/spo.ttl");
			
		}

		[Test]
		public void DropSilentDefault() {
			ExecuteUpdate(@"update-silent/drop-default-silent.ru");
			
		}

		[Test]
		public void CopySilent() {
				ImportGraph(@"update-silent/spo.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"update-silent/copy-silent.ru");
					ValidateGraph(@"update-silent/spo.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[Test]
		public void CopySilentToDefault() {
			ExecuteUpdate(@"update-silent/copy-to-default-silent.ru");
			
		}

		[Test]
		public void MoveSilent() {
				ImportGraph(@"update-silent/spo.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"update-silent/move-silent.ru");
					ValidateGraph(@"update-silent/spo.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[Test]
		public void MoveSilentToDefault() {
			ExecuteUpdate(@"update-silent/move-to-default-silent.ru");
			
		}

		[Test]
		public void AddSilent() {
				ImportGraph(@"update-silent/spo.ttl", new Uri(@"http://example.org/g2"));
				ExecuteUpdate(@"update-silent/add-silent.ru");
					ValidateGraph(@"update-silent/spo.ttl", new Uri(@"http://example.org/g2"));
			
		}

		[Test]
		public void AddSilentToDefault() {
			ExecuteUpdate(@"update-silent/add-to-default-silent.ru");
			
		}

		#endregion

		
	}
}