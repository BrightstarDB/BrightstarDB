using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.Sparql11TestSuite {
    [TestClass]
	public partial class SyntaxFed : SparqlTest {

        public SyntaxFed() : base()
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