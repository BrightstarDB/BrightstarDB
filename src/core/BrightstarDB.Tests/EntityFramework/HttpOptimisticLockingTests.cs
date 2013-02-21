using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestClass]
    public class HttpOptimisticLockingTests : OptimisticLockingTestsBase
    {
        private readonly string _storeName = "HttpOptimisticLockingTests_" + DateTime.Now.Ticks;

        [ClassInitialize]
        public static void SetUp(TestContext context)
        {
            StartService();
        }

        [ClassCleanup]
        public static void TearDown()
        {
            CloseService();
        }

        #region Overrides of OptimisticLockingTestsBase

        protected override MyEntityContext NewContext()
        {
            return new MyEntityContext(
                String.Format("type=http;endpoint=http://localhost:8090/brightstar;storeName={0};optimisticLocking=true", _storeName));
        }

        #endregion

        #region Single Object Updates
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
