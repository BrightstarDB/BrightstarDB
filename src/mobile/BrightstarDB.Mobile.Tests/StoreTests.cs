using System;
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
            var store = storeManager.CreateStore("TestCreateStore_" + DateTime.Now.Ticks);
            store.Dispose();
        }

        [TestMethod]
        [ExpectedException(typeof(StoreManagerException))]
        public void TestCannotCreateIfStoreExists()
        {
            var storeName = "TestCannotCreateIfStoreExists_" + DateTime.Now.Ticks;
            var storeManager = StoreManagerFactory.GetStoreManager();
            var store = storeManager.CreateStore(storeName);
            store.Dispose();

            store = storeManager.CreateStore(storeName);
        }
    }
}
