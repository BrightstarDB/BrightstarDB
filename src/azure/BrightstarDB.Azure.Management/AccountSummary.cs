using BrightstarDB.Azure.Management.Accounts;

namespace BrightstarDB.Azure.Management
{
    public class AccountSummary
    {
        public AccountSummary(IAccount account)
        {
            AccountId = account.Id;
            IsAdmin = account.IsAdmin;
            EmailAddress = account.EmailAddress;
        }

        public string AccountId { get; set; }
        public string EmailAddress { get; set; }
        public bool IsAdmin { get; set; }
    }
}