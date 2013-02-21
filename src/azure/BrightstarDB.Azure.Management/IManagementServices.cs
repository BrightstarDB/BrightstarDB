namespace BrightstarDB.Azure.Management
{
    public interface IManagementServices
    {
        SubscriptionDetails CreateSubscription(string accountId, SubscriptionDetails subscriptionDetails);
        void UpdateSubscription(string subscriptionId, SubscriptionDetails updatedDetails);
        void DeleteSubscription(string subscriptionId);
    }
}
