#if !PORTABLE // Portable version of BrightstarDB does not contain BrightstarDB.Dynamic
using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.Dynamic;
using NUnit.Framework;

namespace BrightstarDB.Tests
{
    [TestFixture]
    public class DynamicObjectTests
    {
        private IDataObjectContext GetContext(string type)
        {
            switch (type)
            {
                case "http":
                    return BrightstarService.GetDataObjectContext(
                        "Type=http;endpoint=http://localhost:8090/brightstar;StoreName=DynamicObjectTests" + Guid.NewGuid());
                case "embedded":
                    return BrightstarService.GetDataObjectContext(
                        "Type=embedded;StoresDirectory=c:\\brightstar;StoreName=DynamicObjectTests-" + Guid.NewGuid());
            }
            return null;
        }

        private BrightstarDynamicContext GetDynamicContext()
        {
            return new BrightstarDynamicContext(GetContext("embedded"));
        } 

        [Test]
        public void TestCreateDynamicContext()
        {
            var dc = GetDynamicContext();
            Assert.IsNotNull(dc);
        }

        [Test]
        public void TestCreateDynamicObject()
        {
            var storeId = Guid.NewGuid();
            var dc = GetDynamicContext();
            var store = dc.CreateStore(storeId.ToString());
            var o = store.MakeNewObject();
            o.Name = "graham";
            store.SaveChanges();
        }

        [Test]
        public void TestFetchDynamicObject()
        {
            var storeId = Guid.NewGuid().ToString();
            var dc = GetDynamicContext();
            var store = dc.CreateStore(storeId);
            var o = store.MakeNewObject();
            o.Name = "graham";
            store.SaveChanges();

            store = dc.OpenStore(storeId);
            var p = store.GetDataObject(o.Identity);

            Assert.AreEqual(o.Identity, p.Identity);
            Assert.AreEqual(o.Name.FirstOrDefault(), p.Name.FirstOrDefault());
        }

        [Test]
        public void TestUseIndexerOnDynamicObject()
        {
            var storeId = Guid.NewGuid().ToString();
            var dc = GetDynamicContext();
            var store = dc.CreateStore(storeId);
            var o = store.MakeNewObject();
            o["Name"] = "graham";
            store.SaveChanges();

            store = dc.OpenStore(storeId);
            var p = store.GetDataObject(o.Identity);

            var names = p["Name"];
            Assert.IsNotNull(names);

            Assert.AreEqual(o.Identity, p.Identity);
            Assert.AreEqual(o.Name.FirstOrDefault(), names.FirstOrDefault());
        }

        [Test]
        public void TestSetGetPropertyWithLiteral()
        {
            var storeId = Guid.NewGuid().ToString();
            var dc = GetDynamicContext();
            var store = dc.CreateStore(storeId);
            var o = store.MakeNewObject();
            o.Name = "graham";
            store.SaveChanges();
            store = dc.OpenStore(storeId);
            var p = store.GetDataObject(o.Identity);
            var name = p.Name;
            Assert.AreEqual("graham", name.FirstOrDefault());
            foreach (var VARIABLE in name)
            {
                Assert.AreEqual("graham", VARIABLE);
            }

            var enuma = p.Name as IEnumerable<object>;
            Assert.IsNotNull(enuma);

            var nameList = p.Name as IEnumerable<object>;
            Assert.AreEqual("graham", nameList.FirstOrDefault());
        }

        [Test]
        public void TestSetGetPropertyLiteralCollection()
        {
            var storeId = Guid.NewGuid().ToString();
            var dc = GetDynamicContext();
            var store = dc.CreateStore(storeId);
            var o = store.MakeNewObject();
            o.Name = new List<string> {"graham", "gra"};
            store.SaveChanges();
            store = dc.OpenStore(storeId);
            var p = store.GetDataObject(o.Identity);
            var name = p.Name as IEnumerable<object>;
            Assert.IsTrue(name.Contains("gra"));
            Assert.IsTrue(name.Contains("graham"));
        }

        [Test]
        public void TestSetUsingGetValue()
        {
            var storeId = Guid.NewGuid().ToString();
            var dc = GetDynamicContext();
            var store = dc.CreateStore(storeId);
            var o = store.MakeNewObject();
            o.Name = new List<string> { "graham", "gra" };
            store.SaveChanges();
            store = dc.OpenStore(storeId);
            var p = store.GetDataObject(o.Identity);
            var name = p.Name;
            
            p.Name = name;

            var names = p.Name as IEnumerable<object>;
            Assert.IsTrue(names.Contains("gra"));
            Assert.IsTrue(names.Contains("graham"));
        }

        [Test]
        public void TestNamespaceMappings()
        {
            var storeId = Guid.NewGuid().ToString();
            var dc = GetDynamicContext();
            var store = dc.CreateStore(storeId);
            var o = store.MakeNewObject();
            o.rdfs__label = "Graham";

            store.SaveChanges();
            store = dc.OpenStore(storeId);
            var p = store.GetDataObject(o.Identity);
            var names = p.rdfs__label as IEnumerable<object>;
            Assert.IsTrue(names.Contains("Graham"));
        }

        [Test]
        public void TestSetGetWithDataObject()
        {
            var storeId = Guid.NewGuid().ToString();
            var dc = GetDynamicContext();
            var store = dc.CreateStore(storeId);
            var o1 = store.MakeNewObject();

            var o2 = store.MakeNewObject();
            o2.Name = "billybob";
            o1.relatedPerson = o2;

            store.SaveChanges();

            store = dc.OpenStore(storeId);
            var o3 = store.GetDataObject(o1.Identity);

            var o4 = o3.relatedPerson.FirstOrDefault();

            Assert.AreEqual(o2.Identity, o4.Identity);
            Assert.IsNotNull(o4.Name);
            Assert.AreEqual("billybob", o4.Name.FirstOrDefault());
        }

        [Test]
        public void TestListOfRelatedDataObjects()
        {
            var storeId = Guid.NewGuid().ToString();
            var dc = GetDynamicContext();
            var store = dc.CreateStore(storeId);
            var o1 = store.MakeNewObject();
            var o2 = store.MakeNewObject();
            o2.Name = "bob";
            var o3 = store.MakeNewObject();
            o3.Name = "fred";

            o1.friends = new [] {o2, o3};
            store.SaveChanges();

            store = dc.OpenStore(storeId);
            var o4 = store.GetDataObject(o1.Identity);

            var friends = o4.friends as IEnumerable<object>;
            Assert.IsNotNull(friends);

            Assert.AreEqual(2, friends.Count());
        }

        [Test]
        public void TestListOfRelatedDataObjectsDataValues()
        {
            var storeId = Guid.NewGuid().ToString();
            var dc = GetDynamicContext();
            var store = dc.CreateStore(storeId);
            var o1 = store.MakeNewObject();
            var o2 = store.MakeNewObject();
            o2.Name = "bob";
            var o3 = store.MakeNewObject();
            o3.Name = "bob";

            o1.friends = new[] { o2, o3 };
            store.SaveChanges();

            store = dc.OpenStore(storeId);
            var o4 = store.GetDataObject(o1.Identity);

            var friends = o4.friends as IEnumerable<object>;
            Assert.IsNotNull(friends);

            foreach (var f in o4.friends)
            {
                Assert.AreEqual("bob", f.Name.FirstOrDefault());
            }

        }

        [Test]
        public void TestSetRelatedListWithListValues()
        {
            var storeId = Guid.NewGuid().ToString();
            var dc = GetDynamicContext();
            var store = dc.CreateStore(storeId);
            var o1 = store.MakeNewObject();
            var o2 = store.MakeNewObject();
            o2.Name = "bob";
            var o3 = store.MakeNewObject();
            o3.Name = "bob";

            o1.friends = new[] { o2, o3 };

            var o4 = store.MakeNewObject();
            o4.friends = o1.friends;

            store.SaveChanges();

            store = dc.OpenStore(storeId);
            var o5 = store.GetDataObject(o4.Identity);

            foreach (var f in o5.friends)
            {
                Assert.AreEqual("bob", f.Name.FirstOrDefault());
            }
        }

        [Test]
        public void TestSampleCode()
        {
            // gets a new BrightstarDB DataObjectContext
            var dataObjectContext = BrightstarService.GetDataObjectContext();

            // create a dynamic context
            var dynaContext = new BrightstarDynamicContext(dataObjectContext);

            // open a new store
            var storeId = "DynamicSample" + Guid.NewGuid().ToString();
            var dynaStore = dynaContext.CreateStore(storeId);

            // create some dynamic objects. 
            dynamic brightstar = dynaStore.MakeNewObject();
            dynamic product = dynaStore.MakeNewObject();

            // set some properties
            brightstar.name = "BrightstarDB";
            product.rdfs__label = "Product";

            // use namespace mapping (RDF and RDFS are defined by default)
            // Assigning a list creates repeated RDF properties.
            brightstar.rdfs__label = new[] { "BrightstarDB", "NoSQL Database" };

            // objects are connected together in the same way
            brightstar.rdfs__type = product;

            dynaStore.SaveChanges();

            // open store and read some data
            dynaStore = dynaContext.OpenStore(storeId);

            var id = brightstar.Identity;
            brightstar = dynaStore.GetDataObject(id);

            // property values are ALWAYS collections.
            var name = brightstar.name.FirstOrDefault();
            Assert.AreEqual("BrightstarDB", name);

            // they can be enumerated without a cast
            foreach (var l in brightstar.rdfs__label)
            {
                Assert.IsTrue(l.Equals("BrightstarDB") || l.Equals("NoSQL Database"));
            }

            // object relationships are navigated in the same way
            var p = brightstar.rdfs__type.FirstOrDefault();
            Assert.AreEqual("Product", p.rdfs__label.FirstOrDefault());
        }
    }
}
#endif