using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.Sparql11TestSuite {
    [TestClass]
	public partial class Copy : SparqlTest {

        public Copy() : base()
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

		#endregion

		
	}
}