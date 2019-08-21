using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Samples.EntityFramework.GettingStartedCore
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
