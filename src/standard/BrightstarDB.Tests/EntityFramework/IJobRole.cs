using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity]
    public interface IJobRole
    {
        string Id { get; }
        string Description { get; set; }
        [InverseProperty("JobRole")]
        ICollection<IPerson> Persons { get; set; } 
    }
}
