using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity("Market")]
    public interface IMarket
    {
        [Identifier]
        string Id { get; }

        [PropertyType("dc:title")]
        string Name { get; set; }

        [PropertyType("listing")]
        ICollection<ICompany> ListedCompanies { get; set; }
    }
}
