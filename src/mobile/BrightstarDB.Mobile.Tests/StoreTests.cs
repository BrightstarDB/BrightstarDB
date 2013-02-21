using BrightstarDB.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Mobile.Tests
{
    [TestClass]
    public class StoreTests
    {
       
        [TestMethod]
        public void TestCreateStore()
        {
            var storeManager = StoreManagerFactory.GetStoreManager();
            var store = storeManager.CreateStore("TestCreateStore");
            store.Dispose();
        }
    }
}
