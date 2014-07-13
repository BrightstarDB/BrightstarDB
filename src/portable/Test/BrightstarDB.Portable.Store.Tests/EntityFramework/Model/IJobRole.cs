using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Portable.Tests.EntityFramework.Model
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
