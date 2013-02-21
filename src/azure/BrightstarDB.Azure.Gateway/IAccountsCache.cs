using BrightstarDB.Azure.Management;

namespace BrightstarDB.Azure.Gateway
{
    public interface IAccountsCache
    {
        void CacheAccessKey(StoreAccessKey key, string accountOrUserId, string storeId);
        StoreAccessKey LookupAccessKey(string accountOrUserId, string storeId);
        void CacheAccountDetails(AccountDetails accountDetails, string accountOrUserId);
        AccountDetails LookupAccountDetails(string accountOrUserId);
        void DropAccountDetails(string accountOrUserId);
    }
}
