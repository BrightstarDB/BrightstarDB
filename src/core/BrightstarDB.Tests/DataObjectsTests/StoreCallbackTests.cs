using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using BrightstarDB.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.DataObjectsTests
{
    [TestClass]
    public class StoreCallbackTests
    {
        private readonly string _storeName;
        private readonly string _connectionString;
        private readonly List<IDataObject> _changedItems = new List<IDataObject>();

        public StoreCallbackTests()
        {
            _storeName = "DataObjects_StoreCallbackTests_" + DateTime.Now.Ticks;
            _connectionString = "type=embedded;storesDirectory=c:\\brightstar;storeName=" + _storeName;
        }

        [TestMethod]
        public void TestSaveWorksWithNoCallback()
        {
            var store = GetDataObjectStore();
            var newObject = store.MakeDataObject();
            newObject.SetProperty("http://example.org/p", "Some Value");
            store.SaveChanges();

            var results = store.ExecuteSparql(String.Format("SELECT ?v WHERE {{ <{0}> <http://example.org/p> ?v }}",newObject.Identity ));
            Assert.AreEqual(1, results.ResultDocument.SparqlResultRows().Count());
        }

        [TestMethod]
        public void TestSavingChangesCallback()
        {
            var store = GetDataObjectStore();
            var obj1 = store.MakeDataObject();
            var obj2 = store.MakeDataObject();
            obj1.SetProperty("http://example.org/p", "Object 1");
            obj2.SetProperty("http://example.org/p", "Object 2");
            store.SaveChanges();

            _changedItems.Clear();
            store.SavingChanges += LogChanges;

            obj1  = store.GetDataObject(obj1.Identity);
            obj1.SetProperty("http://example.org/p", "Updated Object 1");
            var obj3 = store.MakeDataObject();
            obj3.SetProperty("http://example.org/p", "Object 3");
            store.SaveChanges();

            Assert.AreEqual(2, _changedItems.Count);
            Assert.IsTrue(_changedItems.Any(x=>x.Identity.Equals(obj1.Identity)));
            Assert.IsTrue(_changedItems.Any(x=>x.Identity.Equals(obj3.Identity)));
            _changedItems.Clear();

        }

        [TestMethod]
        public void TestThrowingCancelsSave()
        {
            var store = GetDataObjectStore();
            store.SavingChanges += ThrowOnSave;
            var x = store.MakeDataObject();
            x.SetProperty("http://example.org/p", "ObjectX");
            try
            {
                store.SaveChanges();
                Assert.Fail("Expected ApplicationException");
            }
            catch (ApplicationException)
            {
                // Expected
            }

            var results = store.ExecuteSparql(String.Format("SELECT ?v WHERE {{ <{0}> <http://example.org/p> ?v }}", x.Identity));
            Assert.AreEqual(0, results.ResultDocument.SparqlResultRows().Count());
        }

        private void ThrowOnSave(object sender, EventArgs e)
        {
            throw new ApplicationException("Oh noes");
        }

        private void LogChanges(object sender, EventArgs e)
        {
            var store = sender as IDataObjectStore;
            Assert.IsNotNull(store);
            foreach(var o in store.TrackedObjects)
            {
                if (o.IsModified) _changedItems.Add(o);
            }
        }

        private IDataObjectStore GetDataObjectStore()
        {
            var context = BrightstarService.GetDataObjectContext(_connectionString);
            if (!context.DoesStoreExist(_storeName)) return context.CreateStore(_storeName);
            return context.OpenStore(_storeName);
        }
    }
}
