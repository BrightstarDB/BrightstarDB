using BrightstarDB.Storage;
using BrightstarDB.Storage.BTreeStore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests
{
    [TestClass]
    public class ObjectLocationManagerTests
    {

        [TestMethod]
        public void TestInitialInsert()
        {
            var objectLocationManager = new ObjectLocationManager();
            objectLocationManager.SetObjectOffset(1, 500, 1, 1);
        }

        [TestMethod]
        public void TestInitialLookup()
        {
            var objectLocationManager = new ObjectLocationManager();
            objectLocationManager.SetObjectOffset(1, 500, 1, 1);
            var offset = objectLocationManager.GetObjectOffset(1);
            Assert.AreEqual(500u, offset);
        }

        [TestMethod]
        public void TestSetExistingValueLookup()
        {
            var objectLocationManager = new ObjectLocationManager();
            objectLocationManager.SetObjectOffset(1, 500, 1, 1);
            var offset = objectLocationManager.GetObjectOffset(1);
            Assert.AreEqual(500u, offset);

            objectLocationManager.SetObjectOffset(1, 1001, 1, 1);
            offset = objectLocationManager.GetObjectOffset(1);
            Assert.AreEqual(1001u, offset);
        }

        [TestMethod]
        public void Test1001Inserts()
        {
            var objectLocationManager = new ObjectLocationManager();
            for (ulong i=0;i < 1001; i++)
            {
                objectLocationManager.SetObjectOffset(i, 1000 + i, 1, 1);
            }            

            // check all OK
            for (ulong i = 0; i < 1001; i++)
            {
                var offset = objectLocationManager.GetObjectOffset(i);
                Assert.AreEqual(1000 + i, offset);
            }     
       
            Assert.AreEqual(2, objectLocationManager.NumberOfContainers);
        }

        [TestMethod]
        public void Test1001InsertsAndSomeUpdates()
        {
            var objectLocationManager = new ObjectLocationManager();
            for (ulong i = 0; i < 1001; i++)
            {
                objectLocationManager.SetObjectOffset(i, 1000 + i, 1, 1);
            }

            // check all OK
            for (ulong i = 0; i < 1001; i++)
            {
                var offset = objectLocationManager.GetObjectOffset(i);
                Assert.AreEqual(1000 + i, offset);
            }

            // update some value
            objectLocationManager.SetObjectOffset(1, 500, 1, 1);
            objectLocationManager.SetObjectOffset(1000, 500, 1, 1);
            objectLocationManager.SetObjectOffset(1001, 500, 1, 1);   
        


            Assert.AreEqual(2, objectLocationManager.NumberOfContainers);
        }
    }
}
