using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.OData.Tests
{
    [Entity]
    public interface IJobRole
    {
        [Identifier("http://example.org/jobroles/")]
        string Id { get; }
        string Description { get; set; }
        [InverseProperty("JobRole")]
        ICollection<IPerson> Persons { get; set; } 
    }
}
