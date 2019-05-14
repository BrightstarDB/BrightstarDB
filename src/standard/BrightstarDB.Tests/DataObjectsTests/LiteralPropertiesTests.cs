using System;
using BrightstarDB.Client;
using NUnit.Framework;

namespace BrightstarDB.Tests.DataObjectsTests
{
    [TestFixture]
    public class LiteralPropertiesTests
    {
        [Test]
        public void TestOverwriteSingleProperty()
        {
            BrightstarDB.Configuration.PageCacheSize = 0; // Force non-new pages to be evicted from the cache
            string storeName = "OverwriteSingleProperty_" + DateTime.Now.Ticks;
            var store = GetDataObjectStore(storeName);

            var p = store.MakeDataObject("http://example.org/property/p");
            var o1 = store.MakeDataObject("http://example.org/object/1");
            var o2 = store.MakeDataObject("http://example.org/object/2");
            var o3 = store.MakeDataObject("http://example.org/object/3");

            o1.SetProperty(p, "Value 1");
            o2.SetProperty(p, "Value 2");
            o3.SetProperty(p, "Value 3");
            Assert.AreEqual("Value 1", o1.GetPropertyValue(p));
            Assert.AreEqual("Value 2", o2.GetPropertyValue(p));
            Assert.AreEqual("Value 3", o3.GetPropertyValue(p));

            o1.SetProperty(p, "Value 4");

            Assert.AreEqual("Value 4", o1.GetPropertyValue(p));
            Assert.AreEqual("Value 2", o2.GetPropertyValue(p));
            Assert.AreEqual("Value 3", o3.GetPropertyValue(p));

            store.SaveChanges();

            store = GetDataObjectStore(storeName);
            o1 = store.GetDataObject("http://example.org/object/1");
            o2 = store.GetDataObject("http://example.org/object/2");
            o3 = store.GetDataObject("http://example.org/object/3");

            Assert.AreEqual("Value 4", o1.GetPropertyValue(p));
            Assert.AreEqual("Value 2", o2.GetPropertyValue(p));
            Assert.AreEqual("Value 3", o3.GetPropertyValue(p));

        }

        private static IDataObjectStore GetDataObjectStore(string storeName)
        {
            var context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation));
            if (!context.DoesStoreExist(storeName))
            {
                return context.CreateStore(storeName);
            }
            return context.OpenStore(storeName);
        }
    }
}
