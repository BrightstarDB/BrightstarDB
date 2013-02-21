using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestClass]
    public class EmbeddedOptimisticLockingTests : OptimisticLockingTestsBase
    {
        private const string TestStoreLocation = "c:\\brightstar";
        private readonly string _storeName = "EmbeddedOptimisticLockingTests_" + DateTime.Now.Ticks;

        protected override MyEntityContext NewContext()
        {
            return new MyEntityContext(
                String.Format("type=embedded;storesDirectory={0};storeName={1};optimisticLocking=true", TestStoreLocation, _storeName));
        }

        #region SingleObjectRefres
        [TestMethod]
        public new void TestSimplePropertyRefreshWithClientWins()
        {
            base.TestSimplePropertyRefreshWithClientWins();
        }

        [TestMethod]
        public new void TestSimplePropertyRefreshWithStoreWins()
        {
            base.TestSimplePropertyRefreshWithStoreWins();

        }

        [TestMethod]
        public new void TestRelatedObjectRefreshWithClientWins()
        {
            base.TestRelatedObjectRefreshWithClientWins();
        }

        [TestMethod]
        public new void TestRelatedObjectRefreshWithStoreWins()
        {
            base.TestRelatedObjectRefreshWithStoreWins();
        }

        [TestMethod]
        public new void TestLiteralCollectionRefreshWithClientWins()
        {
            base.TestLiteralCollectionRefreshWithClientWins();
        }

        [TestMethod]
        public new void TestLiteralCollectionRefreshWithStoreWins()
        {
            base.TestLiteralCollectionRefreshWithStoreWins();
        }

        [TestMethod]
        public new void TestObjectCollectionRefreshWithClientWins()
        {
            base.TestObjectCollectionRefreshWithClientWins();
        }

        [TestMethod]
        public new void TestObjectCollectionRefreshWithStoreWins()
        {
            base.TestObjectCollectionRefreshWithStoreWins();
        }
        #endregion

        #region Multiple Object Updates

        [TestMethod]
        public new void MultiLiteralPropertyRefreshClientWins()
        {
            base.MultiLiteralPropertyRefreshClientWins();
        }

        [TestMethod]
        public new void MultiLiteralPropertyRefreshStoreWins()
        {
            base.MultiLiteralPropertyRefreshStoreWins();
        }

        [TestMethod]
        public new void MultiLiteralPropertyRefreshMixedModes()
        {
            base.MultiLiteralPropertyRefreshMixedModes();
        }

        #endregion
    }
}
