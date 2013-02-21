using System;
using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Azure.Management.Accounts
{
        [Entity]
    public interface ISubscription
    {
            [Identifier("http://demand.brightstardb.com/accounts/subscription/")]
        string Id { get; }

        [InverseProperty("Subscriptions")]
        IAccount Account { get; set; }

        string Label { get; set; }
        DateTime Created { get; set; }
        bool IsTrial { get; set; }
        int StoreCount { get; set; }
        int StoreSizeLimit { get; set; }
        int StoreCountLimit { get; set; }
        int TotalSizeLimit { get; set; }
        bool IsActive { get; set; }

        [PropertyType("bsa:subscriberStore")]
        ICollection<IStore> Stores { get; set; } 
    }
}