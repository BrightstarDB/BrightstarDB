namespace BrightstarDB.EntityFramework.Tests.ContextObjects
{
    [Entity("Market")]
    public interface IMarket
    {
        [Identifier]
        string Id { get; }

        [PropertyType("dc:title")]
        string Name { get; set; }

        [PropertyType("listing")]
        IEntitySet<ICompany> ListedCompanies { get; set; }
    }

    public class Market : IMarket{
        #region Implementation of IMarket

        public string Id
        {
            get { throw new System.NotImplementedException(); }
        }

        public string Name
        {
            get { throw new System.NotImplementedException(); }
            set { throw new System.NotImplementedException(); }
        }

        public IEntitySet<ICompany> ListedCompanies
        {
            get { throw new System.NotImplementedException(); }
            set { throw new System.NotImplementedException(); }
        }

        #endregion
    }
}