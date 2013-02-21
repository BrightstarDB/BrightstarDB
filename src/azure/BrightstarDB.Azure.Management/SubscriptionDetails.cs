using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Azure.Management.Accounts;

namespace BrightstarDB.Azure.Management
{
    public class SubscriptionDetails
    {
        public SubscriptionDetails(){}
        public SubscriptionDetails(ISubscription s )
        {
            Id = s.Id;
            Label = s.Label;
            Created = s.Created;
            IsActive = s.IsActive;
            CurrentStoreCount = s.StoreCount;
            StoreLimit = s.StoreCountLimit;
            StoreSizeLimit = s.StoreSizeLimit;
            TotalSizeLimit = s.TotalSizeLimit;
            IsTrial = s.IsTrial;
            Stores = s.Stores.Select(store => new StoreDetail(store)).ToList();
        }

        // TODO: Reset trial limits (or make it externally configured)
        public static SubscriptionDetails TrialDetails = new SubscriptionDetails
                                                             {
                                                                 Label = "Trial Store",
                                                                 IsTrial = true,
                                                                 StoreLimit = 1,
                                                                 StoreSizeLimit = 1000,
                                                                 TotalSizeLimit = 1000
                                                             };

        public string Id { get; set; }
        public string Label { get; set; }
        public DateTime Created { get; set; }
        public bool IsActive { get; set; }
        public bool IsTrial { get; set; }
        public int CurrentStoreCount { get; set; }
        public int StoreLimit { get; set; }
        public int StoreSizeLimit { get; set; }
        public int TotalSizeLimit { get; set; }
        public List<StoreDetail> Stores { get; set; }
    }
}