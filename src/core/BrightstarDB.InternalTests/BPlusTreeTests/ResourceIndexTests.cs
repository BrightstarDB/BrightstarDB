using System;
using BrightstarDB.Storage;
using BrightstarDB.Storage.BPlusTreeStore;
using BrightstarDB.Storage.BPlusTreeStore.ResourceIndex;
using BrightstarDB.Storage.Persistence;
using NUnit.Framework;

namespace BrightstarDB.InternalTests.BPlusTreeTests
{
    [TestFixture]
    public class ResourceIndexTests
    {
        [Test]
        public void TestAssertShortLiteral()
        {
            var pageStore = TestUtils.CreateEmptyPageStore("TestAssertShortLiteral.data");
            var resourceIndex = new ResourceIndex(pageStore, null);

            var resourceId = resourceIndex.AssertResourceInIndex(0, "Short string value", true,
                                                                   "http://example.org/datatypes/string",
                                                                   "en-us");
            // Should now be able to find the resource id
            Assert.AreEqual(resourceId,
                            resourceIndex.GetResourceId("Short string value", true,
                                                        "http://example.org/datatypes/string", "en-us", true));
            // data type URI and language code should have been inserted as resources
            Assert.AreNotEqual(StoreConstants.NullUlong,
                               resourceIndex.GetResourceId("http://example.org/datatypes/string", false, null,
                                                           null, true));
            Assert.AreNotEqual(StoreConstants.NullUlong,
                               resourceIndex.GetResourceId("en-us", true, null, null, true));
            var resource = resourceIndex.GetResource(resourceId, true);
            Assert.IsNotNull(resource);
            Assert.IsTrue(resource.IsLiteral);
            Assert.AreEqual("Short string value", resource.Value);
            var dtResource = resourceIndex.GetResource(resource.DataTypeId, true);
            Assert.IsNotNull(dtResource);
            Assert.IsFalse(dtResource.IsLiteral);
            Assert.AreEqual("http://example.org/datatypes/string", dtResource.Value);
            var lcResource = resourceIndex.GetResource(resource.LanguageCodeId, true);
            Assert.IsNotNull(lcResource);
            Assert.IsTrue(lcResource.IsLiteral);
            Assert.AreEqual("en-us", lcResource.Value);

            // Persist the index
            var resourceIndexRoot = resourceIndex.RootId;
            resourceIndex.Save(0, null);
            pageStore.Commit(0ul, null);
            pageStore.Close();


            // Test we can still find the resource after reopening the store
            using (pageStore = TestUtils.OpenPageStore("TestAssertShortLiteral.data", false))
            {
                resourceIndex = new ResourceIndex(pageStore, null, resourceIndexRoot);
                // Should still be able to find the resource id
                Assert.AreEqual(resourceId,
                                resourceIndex.GetResourceId("Short string value", true,
                                                            "http://example.org/datatypes/string", "en-us", true));
                Assert.AreNotEqual(StoreConstants.NullUlong,
                                   resourceIndex.GetResourceId("http://example.org/datatypes/string", false, null,
                                                               null,
                                                               true));
                Assert.AreNotEqual(StoreConstants.NullUlong,
                                   resourceIndex.GetResourceId("en-us", true, null, null, true));
            }
        }

        [Test]
        public void TestAssertLongLiteral()
        {
            var longStringValue = "Long string value " + new string('!', 100);
            var pageStore = TestUtils.CreateEmptyPageStore("TestAssertLongLiteral.data");
            var resourceStore = TestUtils.CreateEmptyPageStore("TestAssertLongLiteral.resources");
            var resourceTable = new ResourceTable(resourceStore);
            var resourceIndex = new ResourceIndex(pageStore, resourceTable);

            var resourceId = resourceIndex.AssertResourceInIndex(0, longStringValue, true, "http://example.org/datatypes/string",
                                                "en-us");
            // Should now be able to find the resource id
            Assert.AreEqual(resourceId, resourceIndex.GetResourceId(longStringValue, true, "http://example.org/datatypes/string", "en-us", true));
            // data type URI and language code should have been inserted as resources
            Assert.AreNotEqual(StoreConstants.NullUlong, resourceIndex.GetResourceId("http://example.org/datatypes/string", false, null, null, true));
            Assert.AreNotEqual(StoreConstants.NullUlong, resourceIndex.GetResourceId("en-us", true, null, null, true));
            var resource = resourceIndex.GetResource(resourceId, true);
            Assert.IsNotNull(resource);
            Assert.IsTrue(resource.IsLiteral);
            Assert.AreEqual(longStringValue, resource.Value);
            var dtResource = resourceIndex.GetResource(resource.DataTypeId, true);
            Assert.IsNotNull(dtResource);
            Assert.IsFalse(dtResource.IsLiteral);
            Assert.AreEqual("http://example.org/datatypes/string", dtResource.Value);
            var lcResource = resourceIndex.GetResource(resource.LanguageCodeId, true);
            Assert.IsNotNull(lcResource);
            Assert.IsTrue(lcResource.IsLiteral);
            Assert.AreEqual("en-us", lcResource.Value);

            // Persist the index
            var resourceIndexRoot = resourceIndex.RootId;
            resourceIndex.Save(0, null);
            pageStore.Commit(0ul, null);
            pageStore.Close();
            resourceTable.Commit(0ul, null);
            resourceTable.Dispose();

            // Test we can still find the resource after reopening the store
            using (pageStore = TestUtils.OpenPageStore("TestAssertLongLiteral.data", false))
            {
                using (var rt = new ResourceTable(TestUtils.OpenPageStore("TestAssertLongLiteral.resources", false)))
                {
                    resourceIndex = new ResourceIndex(pageStore, rt, resourceIndexRoot);
                    // Should still be able to find the resource id
                    Assert.AreEqual(resourceId,
                                    resourceIndex.GetResourceId(longStringValue, true,
                                                                "http://example.org/datatypes/string", "en-us", true));
                    Assert.AreNotEqual(StoreConstants.NullUlong,
                                       resourceIndex.GetResourceId("http://example.org/datatypes/string", false, null,
                                                                   null,
                                                                   true));
                    Assert.AreNotEqual(StoreConstants.NullUlong,
                                       resourceIndex.GetResourceId("en-us", true, null, null, true));
                }
            }
        }

        [Test]
        public void TestAssertShortUri()
        {
            IPageStore pageStore;
            ulong resourceId, resourceIndexRoot;
            var shortenedUri = "p0:" + Guid.Empty;
            using (pageStore = TestUtils.CreateEmptyPageStore("TestAssertShortUri.data"))
            {
                var resourceIndex = new ResourceIndex(pageStore, null);
                resourceId = resourceIndex.AssertResourceInIndex(0, shortenedUri);
                Assert.AreEqual(resourceId, resourceIndex.GetResourceId(shortenedUri, false, null, null, true));
                var resource = resourceIndex.GetResource(resourceId, true);
                Assert.IsNotNull(resource);
                Assert.AreEqual(shortenedUri, resource.Value);
                Assert.IsFalse(resource.IsLiteral);
                resourceIndexRoot = resourceIndex.RootId;
                resourceIndex.Save(0, null);
                pageStore.Commit(0ul, null);
            }
            
            using(pageStore = TestUtils.OpenPageStore("TestAssertShortUri.data", false))
            {
                var resourceIndex = new ResourceIndex(pageStore, null, resourceIndexRoot);
                Assert.AreEqual(resourceId, resourceIndex.GetResourceId(shortenedUri, false, null, null, true));
                var resource = resourceIndex.GetResource(resourceId, true);
                Assert.IsNotNull(resource);
                Assert.AreEqual(shortenedUri, resource.Value);
                Assert.IsFalse(resource.IsLiteral);                
            }
        }

        [Test]
        public void TestAssertLongUri()
        {
            IPageStore pageStore;
            ulong resourceId, resourceIndexRoot;
            var shortenedUri = "p0:" + Guid.Empty + Guid.NewGuid();
            using (pageStore = TestUtils.CreateEmptyPageStore("TestAssertLongUri.data"))
            {
                using (var resourceTable = new ResourceTable(TestUtils.CreateEmptyPageStore("TestAssertLongUri.resources")))
                {
                    var resourceIndex = new ResourceIndex(pageStore, resourceTable);
                    resourceId = resourceIndex.AssertResourceInIndex(0, shortenedUri);
                    Assert.AreEqual(resourceId, resourceIndex.GetResourceId(shortenedUri, false, null, null, true));
                    var resource = resourceIndex.GetResource(resourceId, true);
                    Assert.IsNotNull(resource);
                    Assert.AreEqual(shortenedUri, resource.Value);
                    Assert.IsFalse(resource.IsLiteral);
                    resourceIndexRoot = resourceIndex.RootId;
                    resourceIndex.Save(0, null);
                    pageStore.Commit(0ul, null);
                    resourceTable.Commit(0ul, null);
                }
            }

            using (pageStore = TestUtils.OpenPageStore("TestAssertLongUri.data", false))
            {
                using (var resourceTable = new ResourceTable(TestUtils.OpenPageStore("TestAssertLongUri.resources", true)))
                {
                    var resourceIndex = new ResourceIndex(pageStore, resourceTable, resourceIndexRoot);
                    Assert.AreEqual(resourceId, resourceIndex.GetResourceId(shortenedUri, false, null, null, true));
                    var resource = resourceIndex.GetResource(resourceId, true);
                    Assert.IsNotNull(resource);
                    Assert.AreEqual(shortenedUri, resource.Value);
                    Assert.IsFalse(resource.IsLiteral);
                }
            }
        }

    }
}
