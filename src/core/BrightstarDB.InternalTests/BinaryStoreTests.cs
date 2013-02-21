using System;
using BrightstarDB.Storage;
using BrightstarDB.Storage.BPlusTreeStore;
using BrightstarDB.Storage.Persistence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests
{
    [TestClass]
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

        #endregion

        [TestMethod]
        [ExpectedException(typeof(StoreManagerException), "Master file not found")]
        public override void TestOpenStoreFailure()
        {
            base.TestOpenStoreFailure();
        }

        [TestMethod]
        public override void TestCreateStore()
        {
            base.TestCreateStore();
        }

        [TestMethod]
        public override void TestOpenStore()
        {
            base.TestOpenStore();
        }

        [TestMethod]
        public override void TestDeleteStore()
        {
            base.TestDeleteStore();
        }

        [TestMethod]
        public override void TestInsertTriple()
        {
            base.TestInsertTriple();
        }

        [TestMethod]
        public override void TestGetAllTriples()
        {
            base.TestGetAllTriples();
        }

        [TestMethod]
        public override void TestInsertAndRetrieveTriplesInNamedGraphs()
        {
            base.TestInsertAndRetrieveTriplesInNamedGraphs();
        }

        [TestMethod]
        public override void TestInsertAndRetrieveTriplesInNamedGraphs2()
        {
            base.TestInsertAndRetrieveTriplesInNamedGraphs2();
        }

        [TestMethod]
        public override void TestDuplicateTriplesAreNotInserted()
        {
            base.TestDuplicateTriplesAreNotInserted();
        }

        [TestMethod]
        public override void TestDuplicateTriplesAreAllowedInDifferentGraphs()
        {
            base.TestDuplicateTriplesAreAllowedInDifferentGraphs();
        }

        [TestMethod]
        public override void TestInsertMulitpleTriples()
        {
            base.TestInsertMulitpleTriples();
        }

        [TestMethod]
        public override void TestFetchResourceStatements()
        {
            base.TestFetchResourceStatements();
        }

        [TestMethod]
        public override void TestFetchMultipleResourceStatements()
        {
            base.TestFetchMultipleResourceStatements();
        }

        [TestMethod]
        public override void TestDeleteTriples()
        {
            base.TestDeleteTriples();
        }

        [TestMethod]
        public override void TestInsertAndRetrieveTripleWithLiteral()
        {
            base.TestInsertAndRetrieveTripleWithLiteral();
        }

        [TestMethod]
        public override void TestInsertAndRetrieveTripleWithSameLiteralAndDifferentLanguageCode()
        {
            base.TestInsertAndRetrieveTripleWithSameLiteralAndDifferentLanguageCode();
        }

        [TestMethod]
        public override void TestInsertAndRetrieveTripleWithSameLiteralAndDifferentDataType()
        {
            base.TestInsertAndRetrieveTripleWithSameLiteralAndDifferentDataType();
        }

        [TestMethod]
        public override void TestInsertAndRetrieveLiteralObjectTriple()
        {
            base.TestInsertAndRetrieveLiteralObjectTriple();
        }

        [TestMethod]
        public override void TestInsertAndRetrieveXmlLiteral()
        {
            base.TestInsertAndRetrieveXmlLiteral();
        }

        [TestMethod]
        public override void TestMatchTriples()
        {
            base.TestMatchTriples();
        }

        [TestMethod]
        public override void TestMatchTriplesWithNulls()
        {
            base.TestMatchTriplesWithNulls();
        }

        [TestMethod]
        public override void TestSparql1()
        {
            base.TestSparql1();
        }

        [TestMethod]
        public override void TestReadConfiguration()
        {
            base.TestReadConfiguration();
        }

        [TestMethod]
        public override void TestListCommitPoints()
        {
            base.TestListCommitPoints();
        }

        [TestMethod]
        [ExpectedException(typeof(Client.BrightstarClientException))]
        public override void TestRevertToCommitPoint()
        {
            base.TestRevertToCommitPoint();
        }

        [TestMethod]
        public override void TestGetCommitPoint()
        {
            base.TestGetCommitPoint();
        }

        [TestMethod]
        [ExpectedException(typeof(Client.BrightstarClientException))]
        public override void TestQueryAtCommitPoint()
        {
            base.TestQueryAtCommitPoint();
        }

        [TestMethod]
        public override void TestListStoreGraphs()
        {
            base.TestListStoreGraphs();
        }

        [TestMethod]
        public override void TestListStores()
        {
            base.TestListStores();
        }

        [TestMethod]
        public override void TestRecoverFromBadCommitPointWrite()
        {
            base.TestRecoverFromBadCommitPointWrite();
        }

        [TestMethod]
        public override void TestRecoverFromBadCommitPointWrite2()
        {
            base.TestRecoverFromBadCommitPointWrite2();
        }

        [TestMethod]
        public override void TestWriteAllowedAfterRecoverFromBadCommitPointWrite()
        {
            base.TestWriteAllowedAfterRecoverFromBadCommitPointWrite();
        }

        [TestMethod]
        public override void TestBadXmlInSparqlResult()
        {
            base.TestBadXmlInSparqlResult();
        }

        [TestMethod]
        public override void TestConsolidateStore()
        {
            base.TestConsolidateStore();
        }

        [TestMethod]
        public override void TestConsolidateEmptyStore()
        {
            base.TestConsolidateEmptyStore();
        }

        [TestMethod]
        public override void TestBatchedInserts()
        {
            base.TestBatchedInserts();
        }

        [TestMethod]
        public override void TestMultiThreadedReadAccess()
        {
            base.TestMultiThreadedReadAccess();
        }
    }
}