using System;
using NUnit.Framework;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestFixture]
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
        [Test]
        public new void TestSimplePropertyRefreshWithClientWins()
        {
            base.TestSimplePropertyRefreshWithClientWins();
        }

        [Test]
        public new void TestSimplePropertyRefreshWithStoreWins()
        {
            base.TestSimplePropertyRefreshWithStoreWins();

        }

        [Test]
        public new void TestRelatedObjectRefreshWithClientWins()
        {
            base.TestRelatedObjectRefreshWithClientWins();
        }

        [Test]
        public new void TestRelatedObjectRefreshWithStoreWins()
        {
            base.TestRelatedObjectRefreshWithStoreWins();
        }

        [Test]
        public new void TestLiteralCollectionRefreshWithClientWins()
        {
            base.TestLiteralCollectionRefreshWithClientWins();
        }

        [Test]
        public new void TestLiteralCollectionRefreshWithStoreWins()
        {
            base.TestLiteralCollectionRefreshWithStoreWins();
        }

        [Test]
        public new void TestObjectCollectionRefreshWithClientWins()
        {
            base.TestObjectCollectionRefreshWithClientWins();
        }

        [Test]
        public new void TestObjectCollectionRefreshWithStoreWins()
        {
            base.TestObjectCollectionRefreshWithStoreWins();
        }
        #endregion

        #region Multiple Object Updates

        [Test]
        public new void MultiLiteralPropertyRefreshClientWins()
        {
            base.MultiLiteralPropertyRefreshClientWins();
        }

        [Test]
        public new void MultiLiteralPropertyRefreshStoreWins()
        {
            base.MultiLiteralPropertyRefreshStoreWins();
        }

        [Test]
        public new void MultiLiteralPropertyRefreshMixedModes()
        {
            base.MultiLiteralPropertyRefreshMixedModes();
        }

        #endregion

        #region CRUD
        [Test]
        public new void TestCreateAndDeleteInSameContext()
        {
            base.TestCreateAndDeleteInSameContext();
        }
        #endregion
    }
}
