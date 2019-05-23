using NUnit.Framework;

namespace BrightstarDB.Tests.Sparql11TestSuite {
    
    [Ignore("Ignore for now because we are not generating CSV/TSV results")]
	public partial class CsvTsvResults : SparqlTest {

        public CsvTsvResults() : base()
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
		public void Csv01CsvResultFormat() {
	
					ImportData(@"csv-tsv-results/data.ttl");
		
		
			var result = ExecuteQuery(@"csv-tsv-results/csvtsv01.rq");
			CheckResult(result, @"csv-tsv-results/csvtsv01.csv", false);

	
		}

		[Test]
		public void Cvs02CsvResultFormat() {
	
					ImportData(@"csv-tsv-results/data.ttl");
		
		
			var result = ExecuteQuery(@"csv-tsv-results/csvtsv02.rq");
			CheckResult(result, @"csv-tsv-results/csvtsv02.csv", false);

	
		}

		[Test]
		public void Csv03CsvResultFormat() {
	
					ImportData(@"csv-tsv-results/data2.ttl");
		
		
			var result = ExecuteQuery(@"csv-tsv-results/csvtsv01.rq");
			CheckResult(result, @"csv-tsv-results/csvtsv03.csv", false);

	
		}

		[Test]
		public void Tsv01TsvResultFormat() {
	
					ImportData(@"csv-tsv-results/data.ttl");
		
		
			var result = ExecuteQuery(@"csv-tsv-results/csvtsv01.rq");
			CheckResult(result, @"csv-tsv-results/csvtsv01.tsv", false);

	
		}

		[Test]
		public void Tvs02TsvResultFormat() {
	
					ImportData(@"csv-tsv-results/data.ttl");
		
		
			var result = ExecuteQuery(@"csv-tsv-results/csvtsv02.rq");
			CheckResult(result, @"csv-tsv-results/csvtsv02.tsv", false);

	
		}

		[Test]
		public void Tsv03TsvResultFormat() {
	
					ImportData(@"csv-tsv-results/data2.ttl");
		
		
			var result = ExecuteQuery(@"csv-tsv-results/csvtsv01.rq");
			CheckResult(result, @"csv-tsv-results/csvtsv03.tsv", false);

	
		}

		#endregion

		
	}
}