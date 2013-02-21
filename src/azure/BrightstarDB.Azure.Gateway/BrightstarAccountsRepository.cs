using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using BrightstarDB.Azure.Management;
using BrightstarDB.Azure.Management.Accounts;

namespace BrightstarDB.Azure.Gateway
{
    public class BrightstarAccountsRepository : IAccountsRepository
    {
        public const string StoreName = "brightstar-accounts";
        public const string UserTokenIdPrefix = "http://demand.brightstardb.com/accounts/usertoken/";

        private readonly string _connectionString;
        private readonly bool _useCluster = true;
        private readonly IAccountsCache _cache;
        private readonly AccountDetails _superUserAccount;

        internal BrightstarAccountsRepository(string superUserAccountId, string superUserAccountKey, IAccountsCache cache)
        {
            Task.Factory.StartNew(() =>
                                      {
                                          while (true)
                                          {
                                              try
                                              {
                                                  try
                                                  {
                                                      BrightstarCluster.Instance.GetLastModifiedDate(StoreName);
                                                      return;
                                                  }
                                                  catch (StoreNotFoundException)
                                                  {
                                                      Trace.TraceInformation("Attempting to create Accounts Repository");
                                                      BrightstarCluster.Instance.CreateStore(StoreName);
                                                  }
                                              }
                                              catch (EndpointNotFoundException)
                                              {
                                                  Thread.Sleep(3000);
                                              }
                                              catch(Exception)
                                              {
                                                  return;
                                              }
                                          }
                                      });
            _superUserAccount = new AccountDetails{AccountId = superUserAccountId, PrimaryKey = superUserAccountKey, SecondaryKey = superUserAccountKey};
            _cache = cache;
        }

        internal BrightstarAccountsRepository(string connectionString)
        {
            _connectionString = connectionString;
            _useCluster = false;
        }

        private AccountsContext GetAccountsContext()
        {
            if (_useCluster)
            {
                return new AccountsContext(new BrightstarClusterDataObjectStore(StoreName));
            } 
            return new AccountsContext(_connectionString);
        }

        #region Implementation of IAccountsRepository

        /// <summary>
        /// Registers a new account attached to the specified user token
        /// </summary>
        /// <param name="userToken">The user token</param>
        /// <param name="emailAddress">The contact email address for this account</param>
        /// <returns>The ID of the new account</returns>
        public string CreateAccount(string userToken, string emailAddress)
        {
            var context = GetAccountsContext();

            var ut = context.UserTokens.FirstOrDefault(t => t.Id.Equals(userToken));
            if (ut != null)
            {
                var existingAccount = context.Accounts.FirstOrDefault(a => a.UserTokens.Any(t => t.Id.Equals(userToken)));
                if (existingAccount != null) throw new AccountsRepositoryException(AccountsRepositoryException.UserAccountExists, "User already has an account");
            }
            else
            {
                ut= new UserToken();
                context.UserTokens.Add(ut, userToken);
            }
            var account = new Account
                              {
                                  PrimaryKey = GenerateAccessKey(),
                                  SecondaryKey = GenerateAccessKey(),
                                  EmailAddress = emailAddress
                              };
            account.UserTokens.Add(ut);
            context.Accounts.Add(account);
            context.SaveChanges();
            return account.Id;
        }

        /// <summary>
        /// Retrieves the information about a user's account
        /// </summary>
        /// <param name="userToken">The user token for the user</param>
        /// <returns>A structure giving the account ID and the subscriptions attached to the account</returns>
        public AccountDetails GetAccountDetailsForUser(string userToken)
        {
            var accountDetails = _cache.LookupAccountDetails(userToken);
            if (accountDetails == null)
            {
                var context = GetAccountsContext();
                var account = context.Accounts.FirstOrDefault(a => a.UserTokens.Any(ut => ut.Id.Equals(userToken)));
                if (account == null) return null;
                accountDetails = new AccountDetails(account);
                _cache.CacheAccountDetails(accountDetails, userToken);
            }
            return accountDetails;
        }

        public AccountDetails GetAccountDetails(string accountId)
        {
            var accountDetails = _cache.LookupAccountDetails(accountId);
            if (accountDetails == null)
            {
                var context = GetAccountsContext();
                var account = context.Accounts.FirstOrDefault(a => a.Id.Equals(accountId));
                if (account == null) return null;
                accountDetails = new AccountDetails(account);
                _cache.CacheAccountDetails(accountDetails, accountId);
            }
            return accountDetails;
        }

        /// <summary>
        /// Returns the detailed information for a subscription
        /// </summary>
        /// <param name="userToken">The user requesting the subscription details</param>
        /// <param name="subscriptionId">The ID subscription of the subscription</param>
        /// <returns>The subscription details or NULL if the subscription is not found or not owned by the user identified by <paramref name="userToken"/></returns>
        public SubscriptionDetails GetSubscriptionDetailsForUser(string userToken, string subscriptionId)
        {
            var accountDetails = GetAccountDetailsForUser(userToken);
            if (accountDetails != null)
            {
                var context = GetAccountsContext();
                var subscription = context.Subscriptions.FirstOrDefault(
                    s => s.Id.Equals(subscriptionId) && s.Account.Id.Equals(accountDetails.AccountId));
                if (subscription != null)
                {
                    return new SubscriptionDetails(subscription);
                }
            }
            return null;
        }

        public SubscriptionDetails GetSubscriptionDetails(string userToken, string subscriptionId)
        {
            var context = GetAccountsContext();
            var subscription = context.Subscriptions.FirstOrDefault(s => s.Id.Equals(subscriptionId));
            return new SubscriptionDetails(subscription);
        }
        /// <summary>
        /// Creates a new subscription for an account
        /// </summary>
        /// <param name="accountId">The ID of the account</param>
        /// <param name="subscriptionDetails">The configuration for the new subscription</param>
        /// <returns>The ID of the new subscription</returns>
        public SubscriptionDetails CreateSubscription(string accountId, SubscriptionDetails subscriptionDetails)
        {
            var context = GetAccountsContext();
            var account = context.Accounts.FirstOrDefault(a => a.Id.Equals(accountId));
            if (account == null) return null;

            if (subscriptionDetails.IsTrial)
            {
                var existingTrial =
                    context.Subscriptions.FirstOrDefault(s => s.Account.Equals(account) && s.IsTrial);
                if (existingTrial != null)
                {
                    throw new AccountsRepositoryException(AccountsRepositoryException.AccountHasTrialSubscription, "Account already has a trial subscription.");
                }
                subscriptionDetails = SubscriptionDetails.TrialDetails;
            }

            var subscription = context.Subscriptions.Create();
            subscription.Account = account;
            subscription.Created = DateTime.UtcNow;
            subscription.IsActive = true;
            subscription.IsTrial = subscriptionDetails.IsTrial;
            subscription.Label = subscriptionDetails.Label;
            subscription.StoreCountLimit = subscriptionDetails.StoreLimit;
            subscription.StoreSizeLimit = subscriptionDetails.StoreSizeLimit;
            subscription.TotalSizeLimit = subscriptionDetails.TotalSizeLimit;
            context.SaveChanges();
            _cache.DropAccountDetails(accountId);
            foreach(var userToken in account.UserTokens.Select(ut=>ut.Id))
            {
                _cache.DropAccountDetails(userToken);
            }
            return new SubscriptionDetails(subscription);
        }

        /// <summary>
        /// Updates the configuration of a subscription
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription to be updated</param>
        /// <param name="subscriptionDetails">The new details of the subscription</param>
        public void UpdateSubscription(string subscriptionId, SubscriptionDetails subscriptionDetails)
        {
            var context = GetAccountsContext();
            var subscription = context.Subscriptions.FirstOrDefault(s => s.Id.Equals(subscriptionId));
            if (subscription != null)
            {
                subscription.IsActive = subscriptionDetails.IsActive;
                subscription.IsTrial = subscriptionDetails.IsTrial;
                subscription.Label = subscriptionDetails.Label;
                subscription.StoreCountLimit = subscriptionDetails.StoreLimit;
                subscription.StoreSizeLimit = subscriptionDetails.StoreSizeLimit;
                subscription.TotalSizeLimit = subscriptionDetails.TotalSizeLimit;
                context.SaveChanges();
            }
        }

        public StoreDetail CreateStore(string userId, string subscriptionId, string label)
        {
            var context = GetAccountsContext();

            var account = context.Accounts.FirstOrDefault(a => a.UserTokens.Any(t => t.Id.Equals(userId)));
            if (account == null)
            {
                throw new AccountsRepositoryException(AccountsRepositoryException.UserAccountNotFound,
                                                      "No registered account found for user");
            }

            var subscription =
                context.Subscriptions.FirstOrDefault(s => s.Account.Equals(account) && s.Id.Equals(subscriptionId));
            if (subscription == null)
            {
                throw new AccountsRepositoryException(AccountsRepositoryException.InvalidSubscriptionId,
                                                      "The user does not own a subscription with the the specified ID");
            }

            var storeContainerId = Guid.NewGuid().ToString();
            if (subscription.StoreCount >= subscription.StoreCountLimit)
            {
                throw new AccountsRepositoryException(AccountsRepositoryException.StoreCountLimitReached,
                                                      "Subscription store count limit reached");
            }
            subscription.StoreCount++;
            var store = new Store
                            {
                                CurrentSize = 0,
                                IsLimited = false,
                                SizeLimit = subscription.StoreSizeLimit,
                                StoreContainer = storeContainerId,
                                Subscription = subscription,
                                Label = label
                            };
            context.Stores.Add(store);

            var storeAccess = new StoreAccess
                                  {
                                      Grantee = account,
                                      GrantLevel =
                                          (int)
                                          (StoreAccessLevel.Read | StoreAccessLevel.Write | StoreAccessLevel.Admin |
                                           StoreAccessLevel.Export),
                                      AccessStore = store
                                  };
            context.StoreAccesss.Add(storeAccess);

            context.SaveChanges();
            return new StoreDetail(store);
        }

        public IEnumerable<StoreDetail> GetStores(string userToken, string subscriptionId)
        {
            var context = GetAccountsContext();
            var account = context.Accounts.FirstOrDefault(a => a.UserTokens.Any(u => u.Id.Equals(userToken)));
            if (account == null) return new StoreDetail[0];
            var subscriptions =
                context.Stores.Where(s => s.Subscription.Account.Id.Equals(account.Id)).ToList();
            return subscriptions.Select(s => new StoreDetail(s));
        }

        /// <summary>
        /// Returns the access key for a user to access a store
        /// </summary>
        /// <param name="userId">The user requesting access</param>
        /// <param name="storeId">The store to be accessed</param>
        /// <returns>The access key for that store</returns>
        public StoreAccessKey GetUserAccessKey(string userId, string storeId)
        {
            var ret = _cache.LookupAccessKey(userId, storeId);
            if (ret == null)
            {
                var context = GetAccountsContext();
                var account = context.Accounts.FirstOrDefault(a => a.UserTokens.Any(t => t.Id.Equals(userId)));
                if (account == null) return null;
                var access =
                    context.StoreAccesss.FirstOrDefault(
                        s => s.AccessStore.Id.Equals(storeId) && s.Grantee.Equals(account));
                if (access == null) return null;
                ret = new StoreAccessKey(access);
                _cache.CacheAccessKey(ret, userId, storeId);
            }
            return ret;
        }

        /// <summary>
        /// Returns the access key for an account to access a store
        /// </summary>
        /// <param name="accountId">The ID of the account requesting access</param>
        /// <param name="storeId">The store to be accessed</param>
        /// <returns>The access key for the store</returns>
        public StoreAccessKey GetAccountAccessKey(string accountId, string storeId)
        {
            var ret = _cache.LookupAccessKey(accountId, storeId);
            if (ret == null)
            {
                var context = GetAccountsContext();
                var access =
                    context.StoreAccesss.FirstOrDefault(
                        s => s.AccessStore.Id.Equals(storeId) && s.Grantee.Id.Equals(accountId));
                if (access == null)
                {
                    return null;
                }
                ret = new StoreAccessKey(access);
                _cache.CacheAccessKey(ret, accountId, storeId);
            }
            return ret;
        }

        /// <summary>
        /// Returns the configured super user account details
        /// </summary>
        /// <returns></returns>
        public AccountDetails GetSuperUserAccount()
        {
            return _superUserAccount;
        }

        public bool GrantAccess(string ownerAccountId, string storeId, string userAccountId, StoreAccessLevel accessLevel)
        {
            throw new NotImplementedException();
        }

        public bool RevokeAccess(string ownerAccountId, string storeId, string userAccountId)
        {
            throw new NotImplementedException();
        }

        public bool RegenerateKeys(string ownerAccountId, string storeId, string userAccountId, bool regeneratePrimaryKey, bool regenerateSecondaryKey)
        {
            throw new NotImplementedException();
        }

        public AccountDetails UpdateAccount(string accountId, bool isAdmin)
        {
            var context = GetAccountsContext();
            var account = context.Accounts.FirstOrDefault(x => x.Id.Equals(accountId));
            if (account != null)
            {
                account.IsAdmin = isAdmin;
                context.SaveChanges();
                var accountDetails = new AccountDetails(account);
                _cache.CacheAccountDetails(accountDetails, accountId);
                return accountDetails;
            }
            throw new AccountsRepositoryException(AccountsRepositoryException.UserAccountNotFound, "No account found with ID " + accountId);
        }

        public IEnumerable<AccountSummary> GetAccountSummaries()
        {
            var context = GetAccountsContext();
// ReSharper disable LoopCanBeConvertedToQuery
            // Cannot convert this to a Select() query as it is not currently supported in EF
            foreach(var a in context.Accounts)
            {
                yield return new AccountSummary(a);
            }
// ReSharper restore LoopCanBeConvertedToQuery
        }

        public void DeactivateSubscription(string accountId, string subscriptionId)
        {
            var context = GetAccountsContext();
            var subscription =
                context.Subscriptions.FirstOrDefault(s => s.Id.Equals(subscriptionId) && s.Account.Id.Equals(accountId));
            if (subscription != null && subscription.IsActive)
            {
                subscription.IsActive = false;
                context.SaveChanges();
            }
        }

        public void ActivateSubscription(string accountId, string subscriptionId)
        {
            var context = GetAccountsContext();
            var subscription =
                context.Subscriptions.FirstOrDefault(s => s.Id.Equals(subscriptionId) && s.Account.Id.Equals(accountId));
            if (subscription != null && !subscription.IsActive)
            {
                subscription.IsActive = true;
                context.SaveChanges();
            }
        }

        public void UpdateStoreSizes(Dictionary<string, int> storeSizes)
        {
            var context = GetAccountsContext();
            foreach(var storeSize in storeSizes)
            {
                var store = context.Stores.FirstOrDefault(s => s.Id.Equals(storeSize.Key));
                if (store != null)
                {
                    store.CurrentSize = storeSize.Value;
                    if (storeSize.Value > store.SizeLimit)
                    {
                        store.IsLimited = true;
                    }
                }
            }
            context.SaveChanges();
        }

        public StoreDetail GetStoreDetail(string storeId)
        {
            var context = GetAccountsContext();
            var store = context.Stores.FirstOrDefault(s => s.Id.Equals(storeId));
            return store != null ? new StoreDetail(store) : null;
        }

        public void DeleteStore(string id)
        {
            var context = GetAccountsContext();
            var subscription = context.Subscriptions.FirstOrDefault(s => s.Stores.Any(x => x.Id.Equals(id)));
            var account = context.Accounts.FirstOrDefault(a => a.Subscriptions.Contains(subscription));
            var store = context.Stores.FirstOrDefault(s => s.Id.Equals(id));
            if (store != null) context.DeleteObject(store);
            subscription.StoreCount -= 1;
            context.SaveChanges();
            _cache.DropAccountDetails(account.Id);
        }

        #endregion

        private static string GenerateAccessKey()
        {
            var bytes = new byte[64];
            Guid.NewGuid().ToByteArray().CopyTo(bytes, 0);
            Guid.NewGuid().ToByteArray().CopyTo(bytes, 16);
            Guid.NewGuid().ToByteArray().CopyTo(bytes, 32);
            Guid.NewGuid().ToByteArray().CopyTo(bytes, 48);
            return Convert.ToBase64String(bytes);
        }
    }
}