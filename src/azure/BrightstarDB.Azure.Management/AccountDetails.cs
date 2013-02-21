using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Azure.Management.Accounts;

namespace BrightstarDB.Azure.Management
{
    public class AccountDetails
    {
        public AccountDetails(){}
        
        public AccountDetails(IAccount account)
        {
            AccountId = account.Id;
            PrimaryKey = account.PrimaryKey;
            SecondaryKey = account.SecondaryKey;
            Subscriptions = account.Subscriptions.Select(s => new SubscriptionDetails(s)).ToList();
            IsAdmin = account.IsAdmin;
            EmailAddress = account.EmailAddress;
        }

        public string AccountId { get; set; }
        public string PrimaryKey { get; set; }
        public string SecondaryKey { get; set; }
        public IEnumerable<SubscriptionDetails> Subscriptions { get; set; }
        public bool IsAdmin { get; set; }
        public string EmailAddress { get; set; }
    }
}