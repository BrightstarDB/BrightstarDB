using System;
using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Samples.EntityFramework.GettingStartedCore
{
    [Entity]
    public interface IActor
    {
        string Id { get; }
        string Name { get; set; }
        DateTime DateOfBirth { get; set; }  
        ICollection<IFilm> Films { get; set; }
    }
}
