using BrightstarDB.EntityFramework;

namespace BrightstarDB.Azure.Management.Accounts
{
    [Entity]
    public interface IStore
    {
        [Identifier("http://demand.brightstardb.com/stores/")]
        string Id { get; }

        bool IsLimited { get; set; }
        string StoreContainer { get; set; }

        [InverseProperty("Stores")]
        ISubscription Subscription { get; set; }

        int CurrentSize { get; set; }
        int SizeLimit { get; set; }
        string Label { get; set; }
    }
}