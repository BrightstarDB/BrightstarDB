using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework.InverseProperty
{
    [Entity]
    public interface IProduction
    {
        string Id { get; }

        string Title { get; set; }

        [InverseProperty("Production")]
        ICollection<IPerformance> Performances { get; set; }

        [InverseProperty("Production")]
        ICollection<IProductionMember> ProductionTeam { get; set; }

        [InverseProperty("Production")]
        ICollection<IPhoto> Photos { get; set; } 
    }
}