using System.ComponentModel;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity]
    public interface ICompany : INotifyPropertyChanged
    {
        [Identifier(null)]
        string Id { get; }

        [PropertyType("dc:title")]
        string Name { get; set; }

        [PropertyType("ticker")]
        string TickerSymbol { get; set; }

        [InverseProperty("ListedCompanies")]
        IMarket ListedOn { get; set; }

        [PropertyType("price")]
        decimal CurrentSharePrice { get; set; }

        [PropertyType("marketCap")]
        double CurrentMarketCap { get; set; }

        [PropertyType("headCount")]
        int HeadCount { get; set; }
    }
}
