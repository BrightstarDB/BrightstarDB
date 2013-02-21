using System;
using System.Linq;
using System.Xml.Linq;
using BrightstarDB.Azure.Gateway;
using BrightstarDB.Azure.Management;
using BrightstarDB.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.RestServiceTests
{
    [TestClass]
    public class AccountRepositoryTests
    {
        private readonly string _storeName;
        private readonly string _connectionString;
        
        public AccountRepositoryTests()
        {
            _storeName = "AccountRepoTests_" + DateTime.Now.Ticks;
            _connectionString = "type=embedded;storesDirectory=c:\\brightstar\\;storeName=" + _storeName;
            var client = BrightstarService.GetClient(_connectionString);
            if(client.DoesStoreExist(_storeName))
            {
                client.DeleteStore(_storeName);
            }
        }


        [TestMethod]
        public void TestNewAccount()
        {
            var repo = new BrightstarAccountsRepository(_connectionString);
            var accountId = repo.CreateAccount("userTokenA", "userA@example.org");

            AssertTriple("http://demand.brightstardb.com/accounts/usertoken/userTokenA",
                         "http://www.w3.org/1999/02/22-rdf-syntax-ns#type",
                         "http://demand.brightstardb.com/schemas/accounts/UserToken");
            AssertTriple("http://demand.brightstardb.com/accounts/account/" + accountId,
                         "http://www.w3.org/1999/02/22-rdf-syntax-ns#type",
                         "http://demand.brightstardb.com/schemas/accounts/Account");
            AssertTriple("http://demand.brightstardb.com/accounts/account/" + accountId,
                "http://demand.brightstardb.com/schemas/accounts/userToken",
                "http://demand.brightstardb.com/accounts/usertoken/userTokenA");

            var accountDetails = repo.GetAccountDetailsForUser("userTokenA");
            Assert.IsNotNull(accountDetails);
            Assert.AreEqual(accountId, accountDetails.AccountId);
            Assert.AreEqual(0, accountDetails.Subscriptions.Count());
        }

        [TestMethod]
        public void TestDuplicateAccount()
        {
            var repo = new BrightstarAccountsRepository(_connectionString);
            var accountId = repo.CreateAccount("userTokenB", "userB@example.org");
            Assert.IsNotNull(accountId);
            try
            {
                repo.CreateAccount("userTokenB", "userB@example.org");
                Assert.Fail("Expected failure when creating account with duplicate user token");
            }
            catch (AccountsRepositoryException ex)
            {
                // Expected
                Assert.AreEqual(AccountsRepositoryException.UserAccountExists, ex.ErrorCode);
            }
        }

        [TestMethod]
        public void TestNewSubscription()
        {
            var repo = new BrightstarAccountsRepository(_connectionString);
            var accountId = repo.CreateAccount("userTokenC", "userC@example.org");
            var subscription = repo.CreateSubscription(accountId, SubscriptionDetails.TrialDetails);
            Assert.IsNotNull(subscription);
            Assert.IsNotNull(subscription.Id);
            Assert.IsTrue(subscription.IsTrial);
            Assert.AreEqual(SubscriptionDetails.TrialDetails.StoreLimit, subscription.StoreLimit);
            Assert.AreEqual(SubscriptionDetails.TrialDetails.StoreSizeLimit, subscription.StoreSizeLimit);
            Assert.AreEqual(SubscriptionDetails.TrialDetails.TotalSizeLimit, subscription.TotalSizeLimit);

            var accountDetails = repo.GetAccountDetailsForUser("userTokenC");
            Assert.AreEqual(1, accountDetails.Subscriptions.Count());
            var s = accountDetails.Subscriptions.First();
            Assert.AreEqual(subscription.Id, s.Id);
        }

        [TestMethod]
        public void TestOneTrialPerAccount()
        {
            var repo = new BrightstarAccountsRepository(_connectionString);
            var accountId = repo.CreateAccount("userTokenD", "userD@example.org");
            var subscription = repo.CreateSubscription(accountId, SubscriptionDetails.TrialDetails);
            try
            {
                var sub2 = repo.CreateSubscription(accountId, SubscriptionDetails.TrialDetails);
                Assert.Fail("Expected creation of second trial subscription for account to fail.");
            } catch(AccountsRepositoryException are)
            {
                Assert.AreEqual(AccountsRepositoryException.AccountHasTrialSubscription, are.ErrorCode);
            }
        }

        [TestMethod]
        public void TestCreateStore()
        {
            var repo = new BrightstarAccountsRepository(_connectionString);
            var accountId = repo.CreateAccount("userTokenE", "userE@example.org");
            var subscription = repo.CreateSubscription(accountId, SubscriptionDetails.TrialDetails);
            var store = repo.CreateStore("userTokenE", subscription.Id, "Test");
            Assert.IsNotNull(store);
            var updatedAccount = repo.GetAccountDetailsForUser("userTokenE");
            Assert.AreEqual(1, updatedAccount.Subscriptions.Count(), "Unexpected subscriptions count");
            var updatedSubscription = updatedAccount.Subscriptions.First();
            Assert.AreEqual(1, updatedSubscription.CurrentStoreCount, "Unexpected CurrentStoreCount");

            // After creation there should be an access key for the owner on this store
            var storeAccessKeys = repo.GetUserAccessKey("userTokenE", store.Id);
            Assert.IsNotNull(storeAccessKeys);
            Assert.AreEqual(store.Id, storeAccessKeys.StoreId);
            Assert.AreEqual(accountId, storeAccessKeys.AccountId);
            Assert.AreEqual(StoreAccessLevel.Read|StoreAccessLevel.Write|StoreAccessLevel.Admin|StoreAccessLevel.Export,
                storeAccessKeys.Access);

        }

        [TestMethod]
        public void TestStoreCountLimit()
        {
            var repo = new BrightstarAccountsRepository(_connectionString);
            var accountId = repo.CreateAccount("userTokenF", "userF@example.org");
            var subscription = repo.CreateSubscription(accountId, SubscriptionDetails.TrialDetails);
            repo.CreateStore("userTokenF", subscription.Id, "Test");
            try
            {
                repo.CreateStore("userTokenF", subscription.Id, "Test");
                Assert.Fail("Expected exception when creating a second store with StoreCountLimit=1");
            } catch(AccountsRepositoryException are)
            {
                Assert.AreEqual(AccountsRepositoryException.StoreCountLimitReached, are.ErrorCode);
            }
        }

        private XDocument ExecuteQuery(string query)
        {
            var client = BrightstarService.GetClient(_connectionString);
            var ret = XDocument.Load(client.ExecuteQuery(_storeName, query));
            return ret;
        }

        private void AssertTriple(string s, string p, string o)
        {
            var result = ExecuteQuery(String.Format("ASK {{ <{0}> <{1}> <{2}> . }}", s,p,o));
            Assert.IsTrue(result.SparqlBooleanResult(),
                "Assert failed for triple <{0}> <{1}> <{2}>", s,p,o);
        }
    }
}
