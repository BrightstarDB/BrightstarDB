using System;
using System.Web.Caching;
using BrightstarDB.Azure.Management;

namespace BrightstarDB.Azure.Gateway
{
    public class WebAccountsCache : IAccountsCache
    {
        private readonly Cache _cache;
        public WebAccountsCache(Cache webCache)
        {
            _cache = webCache;
        }

        #region Implementation of IAccountsCache

        public void CacheAccessKey(StoreAccessKey key, string accountOrUserId, string storeId)
        {
            _cache.Add("StoreAccessKey:" + accountOrUserId + ":" + storeId,
                       key,
                       null,
                       DateTime.Now.AddMinutes(15.0),
                       Cache.NoSlidingExpiration,
                       CacheItemPriority.Normal,
                       null);
        }

        public StoreAccessKey LookupAccessKey(string accountOrUserId, string storeId)
        {
            return _cache.Get("StoreAccessKey:" + accountOrUserId + ":" + storeId) as StoreAccessKey;
        }

        public void CacheAccountDetails(AccountDetails accountDetails, string accountOrUserId)
        {
            _cache.Add("AccountDetails:" + accountOrUserId,
                       accountDetails,
                       null,
                       DateTime.Now.AddMinutes(15.0),
                       Cache.NoSlidingExpiration,
                       CacheItemPriority.Normal,
                       null);
        }

        public AccountDetails LookupAccountDetails(string accountOrUserId)
        {
            return _cache.Get("AccountDetails:" + accountOrUserId) as AccountDetails;
        }

        public void DropAccountDetails(string accountOrUserId)
        {
            _cache.Remove("AccountDetails:" + accountOrUserId);
        }

        #endregion
    }

    public class NullAccountsCache : IAccountsCache
    {
        #region Implementation of IAccountsCache

        public void CacheAccessKey(StoreAccessKey key, string accountOrUserId, string storeId)
        {
            return;
        }

        public StoreAccessKey LookupAccessKey(string accountOrUserId, string storeId)
        {
            return null;
        }

        public void CacheAccountDetails(AccountDetails accountDetails, string accountOrUserId)
        {
            return;
        }

        public AccountDetails LookupAccountDetails(string accountOrUserId)
        {
            return null;
        }

        public void DropAccountDetails(string accountOrUserId)
        {
            return;
        }

        #endregion
    }
}