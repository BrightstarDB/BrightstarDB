using NUnit.Framework;

namespace BrightstarDB.Tests.Sparql11TestSuite {
    
	public partial class Copy : SparqlTest {

        public Copy() : base()
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

		#endregion

		
	}
}