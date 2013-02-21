using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.PerformanceTests.Model
{
    [Entity]
    public interface IJobRole
    {
        [Identifier("http://www.examplevocab.com/schema/jobRole/")]
        string Id { get; }

        string Description { get; set; }

        [InverseProperty("JobRole")]
        ICollection<IPerson> Persons { get; set; } 
    }
}
