using System.Collections.Generic;

namespace BrightstarDB.Azure.Management
{
    public interface IAccountsRepository
    {
        /// <summary>
        /// Registers a new account attached to the specified user token
        /// </summary>
        /// <param name="userToken">The user token</param>
        /// <param name="emailAddress">The contact email address to record for the account</param>
        /// <returns>The ID of the new account</returns>
        string CreateAccount(string userToken, string emailAddress);

        /// <summary>
        /// Retrieves the information about a user's account
        /// </summary>
        /// <param name="userToken">The user token for the user</param>
        /// <returns>A structure giving the account ID and the subscriptions attached to the account</returns>
        AccountDetails GetAccountDetailsForUser(string userToken);

        AccountDetails GetAccountDetails(string accountId);

        /// <summary>
        /// Returns the detailed information for a subscription
        /// </summary>
        /// <param name="userToken">The user requesting the subscription details</param>
        /// <param name="subscriptionId">The ID of the subscription</param>
        /// <returns>The subscription details or NULL if the subscription is not found or not owned by the user identified by <paramref name="userToken"/></returns>
        SubscriptionDetails GetSubscriptionDetailsForUser(string userToken, string subscriptionId);

        /// <summary>
        /// Creates a new subscription for an account
        /// </summary>
        /// <param name="accountId">The ID of the account</param>
        /// <param name="subscriptionDetails">The configuration for the new subscription</param>
        /// <returns>The ID of the new subscription</returns>
        SubscriptionDetails CreateSubscription(string accountId, SubscriptionDetails subscriptionDetails);

        /// <summary>
        /// Updates the configuration of a subscription
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription to be updated</param>
        /// <param name="subscriptionDetails">The new details of the subscription</param>
        void UpdateSubscription(string subscriptionId, SubscriptionDetails subscriptionDetails);

        /// <summary>
        /// Create a new store under a subscription
        /// </summary>
        /// <param name="authorizedUser">The ID of the user attempting to create the store</param>
        /// <param name="subscriptionId">The ID of the subscription that the store will be part of</param>
        /// <param name="label">The label string to assign to the store</param>
        /// <returns></returns>
        StoreDetail CreateStore(string authorizedUser, string subscriptionId, string label);

        IEnumerable<StoreDetail> GetStores(string userToken, string subscriptionId);

        /// <summary>
        /// Returns the access key for a user to access a store
        /// </summary>
        /// <param name="userId">The user requesting access</param>
        /// <param name="storeId">The store to be accessed</param>
        /// <returns>The access key for that store</returns>
        StoreAccessKey GetUserAccessKey(string userId, string storeId);

        /// <summary>
        /// Returns the access key for an account to access a store
        /// </summary>
        /// <param name="accountId">The ID of the account requesting access</param>
        /// <param name="storeId">The store to be accessed</param>
        /// <returns>The access key for the store</returns>
        StoreAccessKey GetAccountAccessKey(string accountId, string storeId);

        /// <summary>
        /// Returns the configured super user account details
        /// </summary>
        /// <returns></returns>
        AccountDetails GetSuperUserAccount();

        bool GrantAccess(string ownerAccountId, string storeId, string userAccountId, StoreAccessLevel accessLevel);

        bool RevokeAccess(string ownerAccountId, string storeId, string userAccountId);

        bool RegenerateKeys(string ownerAccountId, string storeId, string userAccountId, bool regeneratePrimaryKey,
                            bool regenerateSecondaryKey);

        AccountDetails UpdateAccount(string accountId, bool isAdmin);
        IEnumerable<AccountSummary> GetAccountSummaries();

        /// <summary>
        /// Marks the specified subscription as not active. 
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="subscriptionId"></param>
        void DeactivateSubscription(string accountId, string subscriptionId);

        /// <summary>
        /// Marks the specified subscription as active
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="subscriptionId"></param>
        void ActivateSubscription(string accountId, string subscriptionId);

        /// <summary>
        /// Performs a batch update of the store size information for a collection of stores
        /// </summary>
        /// <param name="storeSizes">A dictionary mapping store id to store size</param>
        void UpdateStoreSizes(Dictionary<string, int> storeSizes);

        /// <summary>
        /// Retrieves detailed information for a specific store
        /// </summary>
        /// <param name="storeId"></param>
        /// <returns></returns>
        StoreDetail GetStoreDetail(string storeId);

        void DeleteStore(string id);
    }
}
