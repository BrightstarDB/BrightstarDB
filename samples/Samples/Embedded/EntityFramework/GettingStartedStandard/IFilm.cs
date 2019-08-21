using System.Collections.Generic;
using BrightstarDB.EntityFramework;
using BrightstarDB.CodeGeneration;

namespace BrightstarDB.Samples.EntityFramework.GettingStartedStandard
{
    [Entity]
    public interface IFilm
    {
        string Id { get; }
        string Name { get; set; }
        [InverseProperty("Films")]
        ICollection<IActor> Actors { get; }
    }
}
