using NUnit.Framework;

namespace BrightstarDB.Tests.Sparql11TestSuite {

	public partial class Move : SparqlTest {

        public Move() : base()
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