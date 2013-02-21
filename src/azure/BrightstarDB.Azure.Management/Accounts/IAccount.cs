using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Azure.Management.Accounts
{
    [Entity]
    public interface IAccount
    {
        [Identifier("http://demand.brightstardb.com/accounts/account/")]
        string Id { get; }

        [PropertyType("bsa:userToken")]
        ICollection<IUserToken> UserTokens { get; }

        [PropertyType("bsa:subscription")]
        ICollection<ISubscription> Subscriptions { get; }

        [PropertyType("bsa:PrimaryKey")]
        string PrimaryKey { get; }

        [PropertyType("bsa:SecondaryKey")]
        string SecondaryKey { get; }

        [PropertyType("bsa:isAdmin")]
        bool IsAdmin { get; set; }

        [PropertyType("bsa:emailAddress")]
        string EmailAddress { get; set; }
    }
}
