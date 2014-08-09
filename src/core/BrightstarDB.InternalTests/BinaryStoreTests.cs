using System;
using BrightstarDB.Storage;
using BrightstarDB.Storage.BPlusTreeStore;
using BrightstarDB.Storage.Persistence;
using NUnit.Framework;

namespace BrightstarDB.InternalTests
{
    [TestFixture]
    public class BinaryStoreTests : StoreTestsBase
    {
        private readonly IStoreManager _storeManager =
            new BPlusTreeStoreManager(new StoreConfiguration { PersistenceType = PersistenceType.Rewrite },
                                      new FilePersistenceManager());

        #region Overrides of StoreTestsBase

        internal override IStoreManager StoreManager
        {
            get { return _storeManager; }
        }

        internal override PersistenceType TestPersistenceType
        {
            get { return PersistenceType.Rewrite; }
        }
        #endregion

        [Test]
        [ExpectedException(typeof(StoreManagerException), ExpectedMessage="Master file not found")]
        public override void TestOpenStoreFailure()
        {
            base.TestOpenStoreFailure();
        }

        [Test]
        public override void TestCreateStore()
        {
            base.TestCreateStore();
        }

        [Test]
        public override void TestOpenStore()
        {
            base.TestOpenStore();
        }

        [Test]
        public override void TestDeleteStore()
        {
            base.TestDeleteStore();
        }

        [Test]
        public override void TestInsertTriple()
        {
            base.TestInsertTriple();
        }

        [Test]
        public override void TestGetAllTriples()
        {
            base.TestGetAllTriples();
        }

        [Test]
        public override void TestInsertAndRetrieveTriplesInNamedGraphs()
        {
            base.TestInsertAndRetrieveTriplesInNamedGraphs();
        }

        [Test]
        public override void TestInsertAndRetrieveTriplesInNamedGraphs2()
        {
            base.TestInsertAndRetrieveTriplesInNamedGraphs2();
        }

        [Test]
        public override void TestDuplicateTriplesAreNotInserted()
        {
            base.TestDuplicateTriplesAreNotInserted();
        }

        [Test]
        public override void TestDuplicateTriplesAreAllowedInDifferentGraphs()
        {
            base.TestDuplicateTriplesAreAllowedInDifferentGraphs();
        }

        [Test]
        public override void TestInsertMulitpleTriples()
        {
            base.TestInsertMulitpleTriples();
        }

        [Test]
        public override void TestFetchResourceStatements()
        {
            base.TestFetchResourceStatements();
        }

        [Test]
        public override void TestFetchMultipleResourceStatements()
        {
            base.TestFetchMultipleResourceStatements();
        }

        [Test]
        public override void TestDeleteTriples()
        {
            base.TestDeleteTriples();
        }

        [Test]
        public override void TestInsertAndRetrieveTripleWithLiteral()
        {
            base.TestInsertAndRetrieveTripleWithLiteral();
        }

        [Test]
        public override void TestInsertAndRetrieveTripleWithSameLiteralAndDifferentLanguageCode()
        {
            base.TestInsertAndRetrieveTripleWithSameLiteralAndDifferentLanguageCode();
        }

        [Test]
        public override void TestInsertAndRetrieveTripleWithSameLiteralAndDifferentDataType()
        {
            base.TestInsertAndRetrieveTripleWithSameLiteralAndDifferentDataType();
        }

        [Test]
        public override void TestInsertAndRetrieveLiteralObjectTriple()
        {
            base.TestInsertAndRetrieveLiteralObjectTriple();
        }

        [Test]
        public override void TestInsertAndRetrieveXmlLiteral()
        {
            base.TestInsertAndRetrieveXmlLiteral();
        }

        [Test]
        public override void TestMatchTriples()
        {
            base.TestMatchTriples();
        }

        [Test]
        public override void TestMatchTriplesWithNulls()
        {
            base.TestMatchTriplesWithNulls();
        }

        [Test]
        public override void TestSparql1()
        {
            base.TestSparql1();
        }

        [Test]
        public override void TestReadConfiguration()
        {
            base.TestReadConfiguration();
        }

        [Test]
        public override void TestListCommitPoints()
        {
            base.TestListCommitPoints();
        }

        [Test]
        [ExpectedException(typeof(Client.BrightstarClientException))]
        public override void TestRevertToCommitPoint()
        {
            base.TestRevertToCommitPoint();
        }

        [Test]
        public override void TestGetCommitPoint()
        {
            base.TestGetCommitPoint();
        }

        [Test]
        [ExpectedException(typeof(Client.BrightstarClientException))]
        public override void TestQueryAtCommitPoint()
        {
            base.TestQueryAtCommitPoint();
        }

        [Test]
        public override void TestListStoreGraphs()
        {
            base.TestListStoreGraphs();
        }

        [Test]
        public override void TestListStores()
        {
            base.TestListStores();
        }

        [Test]
        public override void TestRecoverFromBadCommitPointWrite()
        {
            base.TestRecoverFromBadCommitPointWrite();
        }

        [Test]
        public override void TestRecoverFromBadCommitPointWrite2()
        {
            base.TestRecoverFromBadCommitPointWrite2();
        }

        [Test]
        public override void TestWriteAllowedAfterRecoverFromBadCommitPointWrite()
        {
            base.TestWriteAllowedAfterRecoverFromBadCommitPointWrite();
        }

        [Test]
        public override void TestBadXmlInSparqlResult()
        {
            base.TestBadXmlInSparqlResult();
        }

        [Test]
        public override void TestConsolidateStore()
        {
            base.TestConsolidateStore();
        }

        [Test]
        public override void TestConsolidateEmptyStore()
        {
            base.TestConsolidateEmptyStore();
        }

        [Test]
        public override void TestBatchedInserts()
        {
            base.TestBatchedInserts();
        }

        [Test]
        public override void TestBatchedInsertsRepeatable()
        {
            base.TestBatchedInsertsRepeatable();
        }

        [Test]
        public override void TestMultiThreadedReadAccess()
        {
            base.TestMultiThreadedReadAccess();
        }

        [Test]
        public override void TestSnapshotStore()
        {
            base.TestSnapshotStore();
        }
    }
}