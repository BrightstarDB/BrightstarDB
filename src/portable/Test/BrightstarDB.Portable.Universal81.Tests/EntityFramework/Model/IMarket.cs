using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Portable.Tests.EntityFramework.Model
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
