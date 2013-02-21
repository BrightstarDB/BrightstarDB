using System;

namespace BrightstarDB.EntityFramework.Tests.ContextObjects
{
    [Entity("Company")]
    public interface ICompany
    {
        [Identifier]
        string Id { get;  }
        
        [PropertyType("dc:title")]
        string Name { get; set; }

        [PropertyType("ticker")]
        string TickerSymbol { get; set; }
        
        [InversePropertyType("listing")]
        IMarket ListedOn { get; set; }

        [PropertyType("price")]
        decimal CurrentSharePrice { get; set; }

        [PropertyType("marketCap")]
        double CurrentMarketCap { get; set; }

        [PropertyType("headCount")]
        int HeadCount { get; set; }

        [PropertyType("isListed")]
        bool IsListed { get; set; }

        [PropertyType("isBlueChip")]
        bool IsBlueChip { get; set; }
    }

    public class Company : ICompany{
        #region Implementation of ICompany

        public string Id
        {
            get { throw new NotImplementedException(); }
        }

        public string Name
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string TickerSymbol
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IMarket ListedOn
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public decimal CurrentSharePrice
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public double CurrentMarketCap
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public int HeadCount
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool IsListed
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool IsBlueChip
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        #endregion
    }
}
