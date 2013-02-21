using BrightstarDB.Azure.Management;

namespace BrightstarDB.Azure.Gateway.Models
{
    public class AccountAndSubscription
    {
        public AccountDetails Account { get; private set; }
        public SubscriptionDetails Subscription { get; private set; }
        public string ApiEndpoint { get; private set; }
        public AccountAndSubscription(AccountDetails account, SubscriptionDetails subscription, string apiEndpoint)
        {
            Account = account;
            Subscription = subscription;
            ApiEndpoint = apiEndpoint;
        }
    }
}